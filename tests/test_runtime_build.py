"""Safety preflight uses fake games and must never reach dependency download."""
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


@unittest.skipUnless(os.name == 'nt', 'Windows PowerShell staging tool')
class RuntimeBuildTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'game'
        self.source.mkdir()
        for name in ('TMNT.exe', 'ParisEngine.dll', 'ParisSerializers.dll'):
            (self.source / name).write_bytes(b'unsupported synthetic assembly')
        self.output = self.root / 'output'
        self.cache = self.root / 'cache'

    def reject(self, output, reason):
        before = {str(p.relative_to(self.root)): p.read_bytes()
                  for p in self.root.rglob('*') if p.is_file()}
        result = subprocess.run([
            'powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass',
            '-File', str(Path(__file__).resolve().parents[1] / 'tools/runtime/build.ps1'),
            '-SourceGameDirectory', str(self.source),
            '-PlaytestDirectory', str(output),
            '-DependencyDirectory', str(self.cache),
        ], capture_output=True, text=True, timeout=15)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(reason, result.stdout + result.stderr)
        after = {str(p.relative_to(self.root)): p.read_bytes()
                 for p in self.root.rglob('*') if p.is_file()}
        self.assertEqual(before, after)
        self.assertFalse(self.cache.exists())

    def test_same_directory_is_rejected_without_modification(self):
        self.reject(self.source, 'overlap')

    def test_nested_directory_is_rejected_without_modification(self):
        self.reject(self.source / 'playtest', 'overlap')

    def test_output_ancestor_is_rejected_without_modification(self):
        self.reject(self.root, 'overlap')

    def test_case_and_dot_alias_is_rejected_without_modification(self):
        self.reject(str(self.source).upper() + '/child/..', 'overlap')

    def test_junction_output_is_rejected_without_modification(self):
        subprocess.run(['powershell.exe', '-NoProfile', '-Command',
                        'New-Item -ItemType Junction -Path $env:TEST_OUTPUT -Target $env:TEST_SOURCE'],
                       env={**os.environ, 'TEST_OUTPUT': str(self.output),
                            'TEST_SOURCE': str(self.source)},
                       check=True, capture_output=True, text=True)
        self.addCleanup(lambda: self.output.rmdir())
        self.reject(self.output, 'Reparse points')

    def test_unsupported_build_does_not_create_output(self):
        self.reject(self.output, 'Unsupported game build')
        self.assertFalse(self.output.exists())

    def test_default_directories_reach_game_validation(self):
        # Run from a disposable script location so repository local state cannot
        # change which preflight runs, and a regression cannot write repo output.
        import shutil
        script = self.root / 'tools/runtime/build.ps1'
        script.parent.mkdir(parents=True)
        shutil.copyfile(Path(__file__).resolve().parents[1] / 'tools/runtime/build.ps1', script)
        result = subprocess.run([
            'powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass',
            '-File', str(script), '-SourceGameDirectory', str(self.source),
        ], capture_output=True, text=True, timeout=15)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Unsupported game build', result.stdout + result.stderr)
        self.assertFalse((self.root / 'local').exists())

    def test_unowned_nonempty_output_is_untouched(self):
        self.output.mkdir()
        (self.output / 'keep.txt').write_text('do not overwrite')
        self.reject(self.output, 'not owned')

    def test_invalid_marker_does_not_authorize_output(self):
        self.output.mkdir()
        (self.output / '.malcolm-playtest.json').write_text('{"owner":"someone else"}')
        self.reject(self.output, 'not owned')
