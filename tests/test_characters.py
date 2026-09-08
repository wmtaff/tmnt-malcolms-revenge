import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image

from tmnt_mod.characters import grid_rectangles, inspect_character_manifest, write_character_preview


class CharacterPipelineTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        image = Image.new('RGBA', (7, 5))
        image.putpixel((1, 1), (255, 0, 0, 255))
        image.putpixel((4, 3), (0, 0, 255, 128))
        image.save(self.root / 'poses.png')
        self.path = self.root / 'manifest.json'
        self.manifest = {
            'schema_version': 1, 'character_id': 'malcolm', 'render_scale': .2,
            'sheets': [{'id': 'poses', 'path': 'poses.png', 'columns': 2, 'rows': 2}],
            'frames': [
                {'id': 'idle_a', 'sheet': 'poses', 'rect': [0, 0, 3, 2], 'pivot': [1, 2]},
                {'id': 'idle_b', 'sheet': 'poses', 'rect': [3, 2, 4, 3], 'pivot': [2, 3]},
            ],
            'animations': [{'id': 'idle', 'loop': True, 'frames': [
                {'frame': 'idle_a', 'duration_ms': 100}, {'frame': 'idle_b', 'duration_ms': 200}]}],
            'native_animation_map': {'NativeIdle': 'idle'},
        }

    def save(self):
        self.path.write_text(json.dumps(self.manifest), encoding='utf-8')

    def test_fractional_grid_covers_all_pixels_without_resampling(self):
        self.assertEqual(grid_rectangles(7, 5, 2, 2),
                         [[0, 0, 3, 2], [3, 0, 4, 2], [0, 2, 3, 3], [3, 2, 4, 3]])
        cells = grid_rectangles(1774, 887, 4, 2)
        self.assertEqual(sum(rect[2] * rect[3] for rect in cells), 1774 * 887)
        self.assertEqual(cells[-1], [1330, 443, 444, 444])

    def test_inspection_reports_alpha_bounds_and_preserves_sources(self):
        self.save()
        before = (self.root / 'poses.png').read_bytes()
        report = inspect_character_manifest(self.path)
        self.assertTrue(report['structurally_valid'])
        self.assertEqual(report['frames'][0]['visible_bounds'], [1, 1, 2, 2])
        self.assertEqual(report['frames'][1]['visible_bounds'], [1, 1, 2, 2])
        self.assertEqual(report['sheets'][0]['partial_alpha_pixels'], 1)
        self.assertEqual(report['sheets'][0]['unmapped_grid_cells'], 2)
        self.assertEqual(report['native_mapping_count'], 1)
        self.assertEqual((self.root / 'poses.png').read_bytes(), before)

    def test_preview_is_self_contained_and_never_overwrites(self):
        self.save()
        output = self.root / 'preview.html'
        result = write_character_preview(self.path, output)
        page = output.read_text(encoding='utf-8')
        self.assertEqual(result['output'], str(output))
        self.assertIn('data:image/png;base64,', page)
        self.assertIn('image-rendering:pixelated', page)
        self.assertNotIn(str(self.root), page)
        self.assertIn('Preview timing', page)
        with self.assertRaises(FileExistsError):
            write_character_preview(self.path, output)
        with self.assertRaises(ValueError):
            write_character_preview(self.path, self.path)

    def test_invalid_frame_geometry_and_duplicate_ids(self):
        for field, value in [('rect', [6, 0, 2, 2]), ('pivot', [float('nan'), 1]),
                             ('id', 'idle_b')]:
            with self.subTest(field=field):
                previous = self.manifest['frames'][0][field]
                self.manifest['frames'][0][field] = value
                self.save()
                with self.assertRaises(ValueError):
                    inspect_character_manifest(self.path)
                self.manifest['frames'][0][field] = previous

    def test_invalid_timing_and_missing_references(self):
        for duration in [0, -1, True, float('inf')]:
            self.manifest['animations'][0]['frames'][0]['duration_ms'] = duration
            self.save()
            with self.assertRaises(ValueError):
                inspect_character_manifest(self.path)
        self.manifest['animations'][0]['frames'][0] = {'frame': 'missing', 'duration_ms': 100}
        self.save()
        with self.assertRaises(ValueError):
            inspect_character_manifest(self.path)

    def test_opaque_sheet_is_rejected_without_background_removal(self):
        Image.new('RGB', (7, 5), 'white').save(self.root / 'poses.png')
        self.save()
        with self.assertRaisesRegex(ValueError, 'transparent'):
            inspect_character_manifest(self.path)

    def test_external_sheet_path_is_rejected(self):
        self.manifest['sheets'][0]['path'] = '../outside.png'
        self.save()
        with self.assertRaisesRegex(ValueError, 'inside'):
            inspect_character_manifest(self.path)

    def test_invalid_grid_and_render_scale(self):
        with self.assertRaises(ValueError):
            grid_rectangles(2, 2, 3, 1)
        self.manifest['render_scale'] = 0
        self.save()
        with self.assertRaises(ValueError):
            inspect_character_manifest(self.path)

    def test_declared_chroma_key_is_analytical_and_source_unchanged(self):
        image = Image.new('RGB', (7, 5), (255, 0, 255))
        image.putpixel((1, 1), (0, 200, 20))
        image.putpixel((2, 1), (230, 20, 240))  # Within the explicit tolerance.
        image.save(self.root / 'poses.png')
        self.manifest['sheets'][0].update(chroma_key=[255, 0, 255], chroma_tolerance=32)
        self.save()
        before = (self.root / 'poses.png').read_bytes()
        report = inspect_character_manifest(self.path)
        self.assertEqual(report['sheets'][0]['keyed_pixels'], 34)
        self.assertEqual(report['frames'][0]['visible_bounds'], [1, 1, 2, 2])
        self.assertEqual((self.root / 'poses.png').read_bytes(), before)

    def test_preview_omits_unknown_metadata_and_escapes_script_text(self):
        self.manifest['frames'][0]['local_private_path'] = str(self.root)
        self.manifest['animations'][0]['unused'] = '</script><script>alert(1)</script>'
        self.save()
        output = self.root / 'preview.html'
        write_character_preview(self.path, output)
        page = output.read_text(encoding='utf-8')
        self.assertNotIn(str(self.root), page)
        self.assertNotIn('alert(1)', page)

    def test_outsized_sheet_is_rejected_before_alpha_analysis(self):
        Image.new('1', (2001, 2000)).save(self.root / 'poses.png')
        self.save()
        with self.assertRaisesRegex(ValueError, 'bounds'):
            inspect_character_manifest(self.path)
