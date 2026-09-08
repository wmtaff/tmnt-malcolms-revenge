"""Bounded PNG diagnostics and a local repeat preview; never repair source art."""

import base64
import hashlib
import html
import io
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


MAX_INPUT_BYTES = 32 * 1024 * 1024
MAX_DIMENSION = 4096
MAX_PIXELS = 8_000_000
PALETTE_LIMIT = 65_536


def _load(path):
    with Path(path).open('rb') as stream:
        data = stream.read(MAX_INPUT_BYTES + 1)
    if len(data) > MAX_INPUT_BYTES:
        raise ValueError(f'PNG exceeds the {MAX_INPUT_BYTES:,}-byte input limit.')
    with Image.open(io.BytesIO(data)) as source:
        if source.format != 'PNG':
            raise ValueError('Background input must be PNG.')
        width, height = source.size
        if not (1 <= width <= MAX_DIMENSION and 1 <= height <= MAX_DIMENSION):
            raise ValueError(f'Background dimensions must each be 1..{MAX_DIMENSION}.')
        if width * height > MAX_PIXELS:
            raise ValueError(f'Background exceeds the {MAX_PIXELS:,}-pixel limit.')
        if getattr(source, 'n_frames', 1) != 1:
            raise ValueError('Background must be a single-frame PNG.')
        mode = source.mode
        has_alpha = 'A' in source.getbands() or 'transparency' in source.info
        source.verify()
    with Image.open(io.BytesIO(data)) as source:
        rgba = source.convert('RGBA')
    return data, rgba, mode, has_alpha


def _difference(first, second):
    difference = ImageChops.difference(first, second)
    stats = ImageStat.Stat(difference)
    return round(sum(stats.mean) / len(stats.mean), 6), max(hi for lo, hi in stats.extrema)


def _report(path, data, rgba, mode, has_alpha):
    width, height = rgba.size
    histogram = rgba.getchannel('A').histogram()
    # Pillow bounds the palette table; never allocate a Python set of 8M colors.
    colors = rgba.getcolors(maxcolors=PALETTE_LIMIT)
    visible_colors = None if colors is None else sum(1 for _, color in colors if color[3])
    rgb = rgba.convert('RGB')
    left = rgb.crop((0, 0, 1, height))
    right = rgb.crop((width - 1, 0, width, height))
    seam_mean, seam_max = _difference(left, right)
    alpha_mean, alpha_max = _difference(
        rgba.getchannel('A').crop((0, 0, 1, height)),
        rgba.getchannel('A').crop((width - 1, 0, width, height)))
    strip_width = min(16, max(1, width // 2))
    left_strip = rgb.crop((0, 0, strip_width, height))
    right_strip = rgb.crop((width - strip_width, 0, width, height))
    strip_mean, strip_max = _difference(left_strip, right_strip)
    left_mean = ImageStat.Stat(left_strip).mean
    right_mean = ImageStat.Stat(right_strip).mean
    strip_color_mean = round(sum(abs(a - b) for a, b in zip(left_mean, right_mean)) / 3, 6)
    warnings = []
    if seam_mean > 12:
        warnings.append('Edge RGB mean difference exceeds the diagnostic threshold of 12/255.')
    if strip_mean > 24:
        warnings.append('Edge-strip RGB mean difference exceeds the diagnostic threshold of 24/255.')
    if sum(histogram[:255]):
        warnings.append('Transparency is present; raw RGB metrics include hidden colors. Review alpha and the preview.')
    if colors is None:
        warnings.append('RGBA palette exceeds the counting limit; exact visible color count is unavailable.')
    return {
        'schema_version': 1, 'path': str(path), 'input_bytes': len(data),
        'sha256': hashlib.sha256(data).hexdigest(),
        'width': width, 'height': height, 'mode': mode, 'structurally_valid': True,
        'alpha': {'has_alpha': has_alpha, 'transparent_pixels': histogram[0],
                  'partial_pixels': sum(histogram[1:255]), 'opaque_pixels': histogram[255]},
        'palette': {'visible_colors': visible_colors, 'exact': colors is not None,
                    'rgba_counting_limit': PALETTE_LIMIT},
        'seam': {'rgb_mean_absolute_difference': seam_mean, 'rgb_max_absolute_difference': seam_max,
                 'alpha_mean_absolute_difference': alpha_mean, 'alpha_max_absolute_difference': alpha_max},
        'edge_strips': {'width': strip_width, 'rgb_mean_absolute_difference': strip_mean,
                        'rgb_max_absolute_difference': strip_max,
                        'mean_color_absolute_difference': strip_color_mean},
        'diagnostic_thresholds': {'seam_rgb_mean': 12, 'strip_rgb_mean': 24},
        'warnings': warnings,
        'interpretation': 'Diagnostics only: low differences do not prove seamless repetition, style, or game compatibility.',
    }


def inspect_background(path):
    """Return JSON-safe structural and edge diagnostics without modifying the PNG.

    Invalid input raises ValueError or a Pillow/OSError. RGB differences use raw
    channel values on a 0..255 scale, including colors beneath transparent pixels.
    The edge strips compare corresponding pixels in the first/last up-to-16 columns.
    """
    data, rgba, mode, has_alpha = _load(path)
    try:
        return _report(path, data, rgba, mode, has_alpha)
    finally:
        rgba.close()


def write_repeat_preview(path, output):
    """Create a self-contained HTML review artifact, refusing all overwrites.

    The output parent must exist. Original PNG bytes are embedded unchanged; no
    resizing, edge repair, generation, or game installation occurs.
    """
    path, output = Path(path), Path(output)
    if path.resolve() == output.resolve():
        raise ValueError('Preview output must differ from the source image.')
    if output.exists() or output.is_symlink():
        raise FileExistsError(f'Preview output already exists: {output}')
    if output.suffix.lower() != '.html':
        raise ValueError('Preview output must have an .html extension.')
    data, rgba, mode, has_alpha = _load(path)
    try:
        report = _report(path, data, rgba, mode, has_alpha)
    finally:
        rgba.close()
    width, height = report['width'], report['height']
    data_url = 'data:image/png;base64,' + base64.b64encode(data).decode('ascii')
    safe_name = html.escape(path.name)
    public_report = {key: value for key, value in report.items() if key != 'path'}
    diagnostics = html.escape(json.dumps(public_report, indent=2))
    document = f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'">
<title>Repeat review: {safe_name}</title>
<style>
body{{margin:24px;background:#17191e;color:#e8e9ec;font:16px system-ui,sans-serif}}
h1{{font-size:24px}} p{{max-width:80ch;line-height:1.5}}
.viewport{{overflow:auto;max-height:70vh;border:1px solid #616773;background:#353944;margin-bottom:24px}}
.repeat{{background-image:url('{data_url}');background-repeat:repeat-x;image-rendering:pixelated;
width:{width * 4}px;height:{height}px;background-size:{width}px {height}px}}
.zoom{{width:{width * 8}px;height:{height * 2}px;background-size:{width * 2}px {height * 2}px}}
pre{{white-space:pre-wrap;overflow-wrap:anywhere;background:#242830;padding:16px;font-size:13px}}
</style></head><body>
<h1>Horizontal repeat review: {safe_name}</h1>
<p>Four unchanged copies at native size, followed by a 2× pixelated preview. Scroll horizontally and vertically as needed.
Tile boundaries occur every {width} pixels at native size. Review streets, rooflines, lighting, repeated landmarks, and alpha edges.</p>
<h2>Native size — {width} × {height}</h2><div class="viewport"><div class="repeat" role="img" aria-label="Four horizontal copies at native size"></div></div>
<h2>2× pixelated view</h2><div class="viewport"><div class="repeat zoom" role="img" aria-label="Four horizontal copies at double size"></div></div>
<p>Metrics are diagnostic. Low edge differences do not prove a seamless result, suitable art style, or game compatibility. No seam repair was performed.</p>
<details><summary>PNG diagnostics</summary><pre>{diagnostics}</pre></details>
</body></html>'''
    # Exclusive creation also rejects a destination created since preflight.
    with output.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(document)
    return {'output': str(output), 'report': report}
