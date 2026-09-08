import json
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest


@unittest.skipUnless(os.name == 'nt', 'Windows playtest controls')
class PlaytestControlTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.fixture = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.fixture.cleanup)
        source = Path(cls.fixture.name) / 'Surrogate.cs'
        source.write_text('''using System; using System.IO; using System.Threading;
class Surrogate { static void Main(string[] args) {
File.WriteAllLines(args[1], args);
File.WriteAllText(args[1] + ".capture", Environment.GetEnvironmentVariable("MALCOLM_CAPTURE_FRAME") ?? "");
Thread.Sleep(10000);
} }''')
        cls.surrogate = Path(cls.fixture.name) / 'surrogate.exe'
        subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe',
                        '/nologo', '/target:winexe', '/platform:x64',
                        '/out:' + str(cls.surrogate), str(source)],
                       check=True, capture_output=True, text=True)

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.stage = self.root / 'stage with spaces'
        self.stage.mkdir()
        self.artifacts = self.root / 'artifacts'
        self.script = Path(__file__).resolve().parents[1] / 'tools/runtime/playtest.ps1'

    def invoke(self, action, *extra):
        return subprocess.run(['powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass',
                               '-File', str(self.script), '-Action', action,
                               '-PlaytestDirectory', str(self.stage),
                               '-ArtifactsDirectory', str(self.artifacts), *extra],
                              capture_output=True, text=True, timeout=15)

    def own_stage(self):
        (self.stage / '.malcolm-playtest.json').write_text(json.dumps(
            {'owner': 'malcolm-mod-runtime', 'schemaVersion': 1}))
        (self.stage / 'Malcolm.Runtime.exe').write_bytes(b'never execute synthetic fixture')

    def test_start_rejects_unowned_stage_before_creating_artifacts(self):
        result = self.invoke('Start')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('not owned', result.stderr)
        self.assertFalse(self.artifacts.exists())

    def test_baseline_and_encounter_are_exclusive(self):
        self.own_stage()
        result = self.invoke('Start', '-Baseline', '-Encounter', str(self.root / 'config.json'))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('exclusive', result.stderr)
        self.assertFalse(self.artifacts.exists())

    def test_status_without_session_does_not_write(self):
        result = self.invoke('Status')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('No tracked playtest', result.stdout)
        self.assertFalse(self.artifacts.exists())

    def test_stop_cannot_kill_a_process_with_reused_or_forged_identity(self):
        self.own_stage()
        self.artifacts.mkdir()
        session = self.artifacts / 'playtest-session.json'
        session.write_text(json.dumps({'schemaVersion': 1, 'owner': 'malcolm-playtest-controls',
                                       'pid': os.getpid(), 'startTimeUtcTicks': '1',
                                       'executable': str(self.stage / 'Malcolm.Runtime.exe')}))
        before = session.read_bytes()
        result = self.invoke('Stop')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('identity mismatch', result.stderr)
        self.assertEqual(session.read_bytes(), before)

    def test_stop_rejects_ordinary_game_executable_record(self):
        self.own_stage()
        self.artifacts.mkdir()
        (self.artifacts / 'playtest-session.json').write_text(json.dumps(
            {'schemaVersion': 1, 'owner': 'malcolm-playtest-controls', 'pid': os.getpid(),
             'startTimeUtcTicks': '1', 'executable': str(self.stage / 'TMNT.exe')}))
        result = self.invoke('Stop')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('identity mismatch', result.stderr)

    def test_synthetic_start_status_stop_and_fresh_logs(self):
        self.own_stage()
        (self.stage / 'Malcolm.Runtime.exe').write_bytes(self.surrogate.read_bytes())
        self.addCleanup(lambda: self.invoke('Stop'))
        previous_log = None
        for iteration in range(2):
            config = self.root / 'encounter with spaces.json'
            config.write_text('{}')
            options = ['-Baseline'] if iteration == 0 else ['-Encounter', str(config)]
            result = self.invoke('Start', *options, '-Capture')
            self.assertEqual(result.returncode, 0, result.stderr)
            session = json.loads((self.artifacts / 'playtest-session.json').read_text())
            log = Path(session['log'])
            for _ in range(100):
                if Path(str(log) + '.capture').exists():
                    break
                time.sleep(.02)
            expected = ['--baseline'] if iteration == 0 else ['--encounter', str(config)]
            self.assertEqual(log.read_text().splitlines(), [str(self.stage), str(log), *expected])
            self.assertEqual(Path(str(log) + '.capture').read_text(), session['capture'])
            self.assertNotEqual(str(log), previous_log)
            self.assertIn('running', self.invoke('Status').stdout)
            if iteration == 0:
                session_file = self.artifacts / 'playtest-session.json'
                recorded = session_file.read_bytes()
                session['startTimeUtcTicks'] = '1'
                session_file.write_text(json.dumps(session))
                rejected = self.invoke('Stop')
                self.assertNotEqual(rejected.returncode, 0)
                self.assertIn('identity mismatch', rejected.stderr)
                session_file.write_bytes(recorded)
            result = self.invoke('Stop')
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn('has exited', self.invoke('Status').stdout)
            previous_log = str(log)
