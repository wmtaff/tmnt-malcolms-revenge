import tempfile
import unittest
import zlib
from pathlib import Path

from tmnt_mod.game import inspect_game


class GameInspectionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for name in ('TMNT.exe', 'ParisEngine.dll', 'ParisSerializers.dll'):
            (self.root / name).write_bytes(b'abc')
        self.stage = self.root / 'Content/StageData/StagesData.zpbn'
        self.stage.parent.mkdir(parents=True)

    def write_stage(self, data):
        compressor = zlib.compressobj(wbits=-15)
        self.stage.write_bytes(compressor.compress(data) + compressor.flush())

    def test_missing_assembly_invalidates_and_warns(self):
        (self.root / 'ParisEngine.dll').unlink()
        result = inspect_game(self.root)
        self.assertFalse(result['valid'])
        self.assertFalse(result['required_assemblies']['ParisEngine.dll']['present'])
        self.assertTrue(any('ParisEngine.dll' in w for w in result['warnings']))

    def test_hash_inventory_and_read_only_inspection(self):
        self.write_stage(b'\x00StageOne\x00EnemySpawn\x01')
        before = {p.relative_to(self.root): p.read_bytes() for p in self.root.rglob('*') if p.is_file()}
        result = inspect_game(self.root)
        self.assertTrue(result['valid'])
        self.assertEqual(result['required_assemblies']['TMNT.exe']['sha256'],
                         'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad')
        self.assertEqual(result['inventory']['by_extension'], {'.dll': 2, '.exe': 1, '.zpbn': 1})
        after = {p.relative_to(self.root): p.read_bytes() for p in self.root.rglob('*') if p.is_file()}
        self.assertEqual(before, after)

    def test_raw_deflate_strings_are_evidence_without_schema_parse(self):
        self.write_stage(b'\x00StageOne\x00EnemySpawn\x01')
        result = inspect_game(self.root)['stage_data']
        self.assertEqual(result['status'], 'inspected')
        self.assertEqual(result['strings'], ['StageOne', 'EnemySpawn'])
        self.assertFalse(result['parsed'])
        self.assertEqual(result['decompressed_size_bytes'], 21)

    def test_corrupt_and_truncated_stage_are_reported(self):
        for content in (b'not raw deflate', b''):
            with self.subTest(content=content):
                self.stage.write_bytes(content)
                result = inspect_game(self.root)
                self.assertFalse(result['valid'])
                self.assertEqual(result['stage_data']['status'], 'error')
                self.assertTrue(result['warnings'])

    def test_decompression_limit_is_enforced(self):
        self.write_stage(b'A' * (8 * 1024 * 1024 + 1))
        result = inspect_game(self.root)
        self.assertFalse(result['valid'])
        self.assertEqual(result['stage_data']['status'], 'limit_exceeded')
        self.assertEqual(result['stage_data']['strings'], [])

    def test_exact_decompression_limit_is_accepted(self):
        self.write_stage(b'\x00' * (8 * 1024 * 1024))
        result = inspect_game(self.root)
        self.assertTrue(result['valid'])
        self.assertEqual(result['stage_data']['decompressed_size_bytes'], 8 * 1024 * 1024)

    def test_missing_installation_returns_controlled_invalid_report(self):
        result = inspect_game(self.root / 'missing')
        self.assertFalse(result['valid'])
        self.assertTrue(result['warnings'])

    def test_missing_stage_is_explicit_warning(self):
        result = inspect_game(self.root)
        self.assertTrue(result['valid'])
        self.assertEqual(result['stage_data']['status'], 'missing')
        self.assertTrue(any('StagesData.zpbn' in w for w in result['warnings']))

    def test_trailing_data_warns_and_invalidates(self):
        self.write_stage(b'StageOne')
        self.stage.write_bytes(self.stage.read_bytes() + b'trailing')
        result = inspect_game(self.root)
        self.assertFalse(result['valid'])
        self.assertTrue(any('trailing' in w.lower() for w in result['warnings']))

    def test_incomplete_nonempty_deflate_is_rejected(self):
        self.write_stage(b'StageOne' * 100)
        self.stage.write_bytes(self.stage.read_bytes()[:-1])
        result = inspect_game(self.root)
        self.assertFalse(result['valid'])
        self.assertEqual(result['stage_data']['status'], 'error')

    def test_compressed_input_limit_is_enforced_before_decoding(self):
        self.stage.write_bytes(b'\x00' * (8 * 1024 * 1024 + 1))
        result = inspect_game(self.root)
        self.assertFalse(result['valid'])
        self.assertEqual(result['stage_data']['status'], 'limit_exceeded')

    def test_string_evidence_is_bounded(self):
        self.write_stage(b'A' * 1000 + b'\x00' + b'StageOne\x00' * 1000)
        stage = inspect_game(self.root)['stage_data']
        self.assertTrue(stage['strings_truncated'])
        self.assertLessEqual(len(stage['strings']), 256)
        self.assertLessEqual(max(map(len, stage['strings'])), 256)
