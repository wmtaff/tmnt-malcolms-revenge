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
    def test_character_inspection_preview_and_controlled_errors(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            image = Image.new('RGBA', (2, 2))
            image.putpixel((0, 0), (255, 0, 0, 255))
            image.save(root / 'poses.png')
            manifest = root / 'manifest.json'
            manifest.write_text(json.dumps({
                'schema_version': 1, 'character_id': 'test', 'display_name': 'Test Character',
                'sheets': [{'id': 'poses', 'path': 'poses.png', 'columns': 1, 'rows': 1}],
                'frames': [{'id': 'idle', 'sheet': 'poses', 'rect': [0, 0, 2, 2], 'pivot': [1, 2]}],
                'animations': [{'id': 'idle', 'loop': True, 'frames': [{'frame': 'idle', 'duration_ms': 100}]}],
            }))
            result, report = self.run_cli('inspect-character', manifest)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertTrue(report['structurally_valid'])
            output = root / 'preview.html'
            result, report = self.run_cli('preview-character', manifest, output)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertTrue(output.is_file())
            original = output.read_bytes()
            result, report = self.run_cli('preview-character', manifest, output)
            self.assertEqual(result.returncode, 2)
            self.assertIn('error', report)
            self.assertEqual(output.read_bytes(), original)
            manifest.write_text('{}')
            result, report = self.run_cli('inspect-character', manifest)
            self.assertEqual(result.returncode, 2)
            self.assertIn('error', report)

    def test_background_report_and_preview(self):
        with tempfile.TemporaryDirectory() as tmp:
            image = Path(tmp) / 'tile.png'
            output = Path(tmp) / 'repeat.html'
            Image.new('RGB', (16, 8), 'navy').save(image)
            result, report = self.run_cli('inspect-background', image)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertTrue(report['structurally_valid'])
            result, report = self.run_cli('preview-background', image, output)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertTrue(output.exists())
            result, report = self.run_cli('preview-background', image, output)
            self.assertEqual(result.returncode, 2)

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
