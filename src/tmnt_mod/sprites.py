"""Inspect sprite pixels without modifying the source image."""

from pathlib import Path

from PIL import Image


def validate_sprite(path, width=None, height=None, max_colors=None):
    """Report the first image frame; bounds use exclusive right/bottom edges.

    Visible colors are distinct RGBA values with nonzero alpha. Fully transparent
    pixels do not contribute, but partial alpha shades count as separate colors.
    """
    for name, value in [('width', width), ('height', height), ('max_colors', max_colors)]:
        if value is not None and (type(value) is not int or value <= 0):
            raise ValueError(f'{name} must be a positive integer')
    path = Path(path)
    violations = []
    warnings = []
    with Image.open(path) as source:
        actual_width, actual_height = source.size
        mode = source.mode
        has_alpha = 'A' in source.getbands() or 'transparency' in source.info
        if getattr(source, 'n_frames', 1) > 1:
            warnings.append('Image has multiple frames; only the first frame was validated.')
        rgba = source.convert('RGBA')
        alpha = rgba.getchannel('A')
        bounds = alpha.getbbox()
        alpha_counts = alpha.histogram()
        colors = rgba.getcolors(maxcolors=actual_width * actual_height)
        visible_colors = sum(1 for _, pixel in colors if pixel[3] != 0)
        if width is not None and actual_width != width:
            violations.append(f'Width is {actual_width}; expected {width}.')
        if height is not None and actual_height != height:
            violations.append(f'Height is {actual_height}; expected {height}.')
        if max_colors is not None and visible_colors > max_colors:
            violations.append(f'Visible palette has {visible_colors} colors; limit is {max_colors}.')
        if bounds is None:
            violations.append('Sprite has no visible pixels.')
        elif bounds[0] == 0 or bounds[1] == 0 or bounds[2] == actual_width or bounds[3] == actual_height:
            warnings.append('Visible pixels touch an image edge; check for clipping.')
    return {
        'path': str(path), 'width': actual_width, 'height': actual_height,
        'mode': mode, 'has_alpha': has_alpha,
        'has_transparency': bool(sum(alpha_counts[:255])),
        'has_partial_alpha': bool(sum(alpha_counts[1:255])),
        'visible_bounds': list(bounds) if bounds is not None else None,
        'visible_colors': visible_colors,
        'violations': violations, 'warnings': warnings, 'valid': not violations,
    }
