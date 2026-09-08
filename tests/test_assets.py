import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

from tmnt_mod.assets import inventory_archive


class ArchiveTests(unittest.TestCase):
    def test_common_wrapper_is_removed_from_categories(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'assets.zip'
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('visual_assets/bosses/a.png', b'')
                archive.writestr('visual_assets/enemies/b.png', b'')
                archive.writestr('__MACOSX/visual_assets/._x', b'')
            result = inventory_archive(path)
            self.assertEqual(result['categories'], {'bosses': 1, 'enemies': 1})
            self.assertTrue(result['valid'])

    def test_inventory_uses_metadata_and_filters_macos(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'assets.zip'
            with zipfile.ZipFile(path, 'w') as archive:
                for name in ['actors/', 'actors/hero.PNG', 'actors/no_extension',
                             'music/theme.ogg', 'README.txt', '__MACOSX/actors/._hero.PNG',
                             'actors/._hero.PNG', 'actors/.DS_Store']:
                    archive.writestr(name, b'' if name.endswith('/') else b'data')
            before = path.read_bytes()
            with patch.object(zipfile.ZipFile, 'open', side_effect=AssertionError('No reads')):
                result = inventory_archive(path)
            self.assertEqual(result['total_files'], 4)
            self.assertEqual(result['ignored_metadata'], 3)
            self.assertEqual(result['categories'], {'actors': 2, 'music': 1, '(root)': 1})
            self.assertEqual(result['extensions'], {'.png': 1, '(none)': 1, '.ogg': 1, '.txt': 1})
            self.assertEqual(result['total_uncompressed_bytes'], 16)
            self.assertEqual(result['total_compressed_bytes'], 16)
            self.assertEqual(result['unsafe_paths'], [])
            self.assertEqual(path.read_bytes(), before)
            self.assertEqual(list(Path(directory).iterdir()), [path])

    def test_reports_unsafe_names_including_metadata_and_windows_paths(self):
        names = ['../outside.png', '/absolute.png', 'C:/drive.png', 'C:relative.png',
                 'folder\\..\\escape.png', '\\\\server\\share.png', '__MACOSX/../bad.png']
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'unsafe.zip'
            with zipfile.ZipFile(path, 'w') as archive:
                for name in names + ['good/sub/sprite.png']:
                    member = zipfile.ZipInfo()
                    member.filename = name
                    archive.writestr(member, b'')
            result = inventory_archive(path)
            self.assertEqual(set(result['unsafe_paths']), set(names))
            self.assertTrue(result['warnings'])
            self.assertFalse(result['valid'])

    def test_invalid_archive_raises(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'bad.zip'
            path.write_bytes(b'not a zip')
            with self.assertRaises(zipfile.BadZipFile):
                inventory_archive(path)
