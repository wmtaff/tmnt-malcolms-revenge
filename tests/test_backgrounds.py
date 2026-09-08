import base64
import html
import tempfile
import unittest
from pathlib import Path

from PIL import Image

from tmnt_mod.backgrounds import inspect_background, write_repeat_preview


class BackgroundTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.image = self.root / 'street.png'

    def test_constant_image_has_zero_edge_differences_and_is_unchanged(self):
        Image.new('RGB', (8, 4), (25, 50, 75)).save(self.image)
        before = self.image.read_bytes()
        report = inspect_background(self.image)
        self.assertEqual((report['width'], report['height']), (8, 4))
        self.assertEqual(report['seam']['rgb_mean_absolute_difference'], 0)
        self.assertEqual(report['edge_strips']['rgb_mean_absolute_difference'], 0)
        self.assertEqual(report['palette']['visible_colors'], 1)
        self.assertEqual(report['alpha']['opaque_pixels'], 32)
        self.assertEqual(self.image.read_bytes(), before)

    def test_discontinuous_edges_are_diagnostic_not_structural_failure(self):
        image = Image.new('RGB', (8, 4), 'black')
        image.paste('white', (4, 0, 8, 4))
        image.save(self.image)
        report = inspect_background(self.image)
        self.assertEqual(report['seam']['rgb_mean_absolute_difference'], 255)
        self.assertEqual(report['seam']['rgb_max_absolute_difference'], 255)
        self.assertEqual(report['edge_strips']['rgb_mean_absolute_difference'], 255)
        self.assertTrue(report['structurally_valid'])
        self.assertTrue(report['warnings'])
        self.assertNotIn('seamless', report)

    def test_alpha_and_visible_palette_are_reported(self):
        image = Image.new('RGBA', (2, 2), (255, 0, 0, 0))
        image.putpixel((1, 0), (1, 2, 3, 128))
        image.putpixel((1, 1), (1, 2, 3, 255))
        image.save(self.image)
        report = inspect_background(self.image)
        self.assertEqual(report['alpha']['transparent_pixels'], 2)
        self.assertEqual(report['alpha']['partial_pixels'], 1)
        self.assertEqual(report['palette']['visible_colors'], 2)
        self.assertEqual(report['seam']['alpha_max_absolute_difference'], 255)

    def test_preview_embeds_original_bytes_and_rejects_overwrites(self):
        Image.new('RGB', (4, 2), 'green').save(self.image)
        before = self.image.read_bytes()
        output = self.root / 'repeat.html'
        result = write_repeat_preview(self.image, output)
        html = output.read_text(encoding='utf-8')
        self.assertIn('data:image/png;base64,' + base64.b64encode(before).decode('ascii'), html)
        self.assertIn('repeat-x', html)
        self.assertIn('image-rendering:pixelated', html)
        self.assertNotIn(str(self.root), html)
        self.assertEqual(result['output'], str(output))
        with self.assertRaises(FileExistsError):
            write_repeat_preview(self.image, output)
        self.assertEqual(output.read_text(encoding='utf-8'), html)
        with self.assertRaises(ValueError):
            write_repeat_preview(self.image, self.image)
        self.assertEqual(self.image.read_bytes(), before)

    def test_non_png_is_rejected_even_with_png_extension(self):
        Image.new('RGB', (2, 2)).save(self.image, format='BMP')
        with self.assertRaisesRegex(ValueError, 'PNG'):
            inspect_background(self.image)

    def test_dimension_limit_before_preview_creation(self):
        Image.new('RGB', (4097, 1)).save(self.image)
        output = self.root / 'repeat.html'
        with self.assertRaisesRegex(ValueError, 'dimensions'):
            write_repeat_preview(self.image, output)
        self.assertFalse(output.exists())

    def test_pixel_limit(self):
        Image.new('1', (4096, 2048)).save(self.image)
        with self.assertRaisesRegex(ValueError, 'pixel'):
            inspect_background(self.image)

    def test_input_byte_limit(self):
        with self.image.open('wb') as stream:
            stream.truncate(32 * 1024 * 1024 + 1)
        with self.assertRaisesRegex(ValueError, 'byte'):
            inspect_background(self.image)

    def test_animation_is_rejected(self):
        Image.new('RGB', (2, 2), 'red').save(
            self.image, save_all=True, append_images=[Image.new('RGB', (2, 2), 'blue')],
            duration=100, loop=0)
        with self.assertRaisesRegex(ValueError, 'single-frame'):
            inspect_background(self.image)

    def test_preview_escapes_filename_and_does_not_embed_local_paths(self):
        source = self.root / "street '& review.png"
        Image.new('RGB', (2, 2)).save(source)
        output = self.root / 'repeat.html'
        write_repeat_preview(source, output)
        document = output.read_text(encoding='utf-8')
        self.assertIn(html.escape(source.name), document)
        self.assertNotIn(source.name, document)
        self.assertNotIn(str(self.root), document)

    def test_palette_over_counting_limit_is_unknown_not_truncated(self):
        image = Image.new('RGB', (257, 256))
        image.putdata([(i & 255, (i >> 8) & 255, i >> 16) for i in range(257 * 256)])
        image.save(self.image)
        report = inspect_background(self.image)
        self.assertIsNone(report['palette']['visible_colors'])
        self.assertFalse(report['palette']['exact'])

    def test_corrupt_input_does_not_create_preview(self):
        self.image.write_bytes(b'not an image')
        output = self.root / 'repeat.html'
        with self.assertRaises(OSError):
            write_repeat_preview(self.image, output)
        self.assertFalse(output.exists())
