import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile

from PIL import Image


class CliTests(unittest.TestCase):
    def run_cli(self, *args):
        env = dict(os.environ, PYTHONPATH=str(Path(__file__).resolve().parents[1] / 'src'))
        result = subprocess.run([sys.executable, '-m', 'tmnt_mod', *map(str, args)],
                                capture_output=True, text=True, env=env)
        self.assertTrue(result.stdout.strip(), result.stderr)
        return result, json.loads(result.stdout)

    def test_inventory_is_json(self):
        with tempfile.TemporaryDirectory() as tmp:
            archive = Path(tmp) / 'assets.zip'
            with zipfile.ZipFile(archive, 'w') as z:
                z.writestr('visual_assets/bosses/test.png', b'not decoded')
            result, report = self.run_cli('inventory-assets', archive)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIsInstance(report, dict)

    def test_invalid_sprite_returns_one(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'sprite.png'
            Image.new('RGBA', (8, 8), (255, 0, 0, 255)).save(path)
            result, report = self.run_cli('validate-sprite', path, '--width', 16)
            self.assertEqual(result.returncode, 1)
            self.assertFalse(report['valid'])

    def test_valid_sprite_returns_zero(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'sprite.png'
            Image.new('RGBA', (8, 8), (255, 0, 0, 255)).save(path)
            result, report = self.run_cli('validate-sprite', path, '--width', 8)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertTrue(report['valid'])

    def test_corrupt_archive_is_controlled_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'bad.zip'
            path.write_bytes(b'not an archive')
            result, report = self.run_cli('inventory-assets', path)
            self.assertEqual(result.returncode, 2)
            self.assertIn('error', report)

    def test_missing_game_is_invalid(self):
        with tempfile.TemporaryDirectory() as tmp:
            result, report = self.run_cli('inspect-game', tmp)
            self.assertEqual(result.returncode, 1)
            self.assertFalse(report['valid'])

    def test_oversized_sprite_is_controlled_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'large.png'
            Image.new('RGBA', (1025, 1024), (255, 0, 0, 255)).save(path)
            result, report = self.run_cli('validate-sprite', path)
            self.assertEqual(result.returncode, 2)
            self.assertIn('error', report)

    def test_missing_input_is_controlled_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            result, report = self.run_cli('inventory-assets', Path(tmp) / 'absent.zip')
            self.assertEqual(result.returncode, 2)
            self.assertIn('error', report)
            self.assertNotIn('Traceback', result.stderr)

    def test_nonpositive_constraint_is_controlled_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'sprite.png'
            Image.new('RGBA', (8, 8), (255, 0, 0, 255)).save(path)
            result, report = self.run_cli('validate-sprite', path, '--width', 0)
            self.assertEqual(result.returncode, 2)
            self.assertIn('error', report)


if __name__ == '__main__':
    unittest.main()
