import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from PIL import Image

from tmnt_mod.sprites import validate_sprite


class SpriteTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / 'sprite.png'

    def test_visible_bounds_palette_and_partial_alpha(self):
        sprite = Image.new('RGBA', (4, 4), (10, 20, 30, 0))
        sprite.putpixel((0, 0), (100, 200, 50, 0))
        sprite.putpixel((1, 1), (255, 0, 0, 255))
        sprite.putpixel((2, 2), (255, 0, 0, 128))
        sprite.save(self.path)
        before = self.path.read_bytes()
        result = validate_sprite(self.path, width=4, height=4, max_colors=2)
        self.assertEqual(result['visible_bounds'], [1, 1, 3, 3])
        self.assertEqual(result['visible_colors'], 2)
        self.assertTrue(result['has_alpha'])
        self.assertTrue(result['has_transparency'])
        self.assertTrue(result['has_partial_alpha'])
        self.assertTrue(result['valid'])
        self.assertEqual(self.path.read_bytes(), before)

    def test_constraints_report_all_violations(self):
        sprite = Image.new('RGB', (3, 4), 'red')
        sprite.putpixel((1, 1), (0, 0, 255))
        sprite.save(self.path)
        result = validate_sprite(self.path, width=8, height=8, max_colors=1)
        self.assertFalse(result['valid'])
        self.assertEqual(len(result['violations']), 3)
        self.assertFalse(result['has_alpha'])
        self.assertFalse(result['has_transparency'])
        self.assertTrue(any('edge' in warning.lower() for warning in result['warnings']))

    def test_empty_sprite_is_invalid(self):
        Image.new('RGBA', (2, 2), (0, 0, 0, 0)).save(self.path)
        result = validate_sprite(self.path)
        self.assertFalse(result['valid'])
        self.assertIsNone(result['visible_bounds'])
        self.assertEqual(result['visible_colors'], 0)
        self.assertTrue(any('visible' in item for item in result['violations']))

    def test_palette_transparency_is_alpha(self):
        sprite = Image.new('P', (3, 3), 0)
        sprite.putpalette([0, 0, 0, 255, 0, 0] + [0] * 762)
        sprite.putpixel((1, 1), 1)
        sprite.save(self.path, transparency=0)
        result = validate_sprite(self.path)
        self.assertTrue(result['has_alpha'])
        self.assertTrue(result['has_transparency'])
        self.assertEqual(result['visible_colors'], 1)
        self.assertEqual(result['visible_bounds'], [1, 1, 2, 2])
        self.assertTrue(result['valid'])

    def test_constraints_require_positive_integers(self):
        Image.new('RGBA', (1, 1), 'red').save(self.path)
        for key in ['width', 'height', 'max_colors']:
            for value in [0, -1, 1.5, True]:
                with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                    validate_sprite(self.path, **{key: value})

    def test_oversized_sprite_is_rejected_before_conversion(self):
        Image.new('RGBA', (1025, 1024), 'red').save(self.path)
        with patch.object(Image.Image, 'convert', side_effect=AssertionError('Do not decode')):
            with self.assertRaisesRegex(ValueError, 'individual frames'):
                validate_sprite(self.path)

    def test_pixel_limit_accepts_boundary(self):
        Image.new('RGBA', (4, 4), 'red').save(self.path)
        with patch('tmnt_mod.sprites.MAX_SPRITE_PIXELS', 16):
            self.assertTrue(validate_sprite(self.path)['valid'])
