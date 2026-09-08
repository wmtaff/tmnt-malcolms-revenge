"""Explicit character-frame manifests and previews; source PNGs stay unchanged."""

import base64
import hashlib
import html
import io
import json
import math
from pathlib import Path, PureWindowsPath
import re

from PIL import Image, ImageChops


def _require(condition, message):
    if not condition:
        raise ValueError(message)


def _integer(value):
    return type(value) is int


def _number(value):
    return type(value) in (int, float) and math.isfinite(value)


def _identifier(value):
    return isinstance(value, str) and re.fullmatch(r'[A-Za-z][A-Za-z0-9_-]{0,63}', value)


def grid_rectangles(width, height, columns, rows):
    """Return row-major integer rectangles with floor boundaries and no lost pixels."""
    _require(all(_integer(n) and n > 0 for n in (width, height, columns, rows)), 'Grid values must be positive integers.')
    _require(columns <= width <= 4096 and rows <= height <= 4096 and columns * rows <= 4096,
             'Grid dimensions exceed bounds or create empty cells.')
    return [[x * width // columns, y * height // rows,
             (x + 1) * width // columns - x * width // columns,
             (y + 1) * height // rows - y * height // rows]
            for y in range(rows) for x in range(columns)]


def _unique(items, limit, label):
    _require(isinstance(items, list) and 0 < len(items) <= limit, f'{label} must be a bounded nonempty list.')
    result = {}
    for item in items:
        _require(isinstance(item, dict) and _identifier(item.get('id')), f'Invalid {label} ID.')
        _require(item['id'] not in result, f'Duplicate {label} ID: {item["id"]}')
        result[item['id']] = item
    return result


def _pairs(pairs):
    result = {}
    for key, value in pairs:
        _require(key not in result, f'Duplicate JSON key: {key}')
        result[key] = value
    return result


def _read(path, limit):
    with path.open('rb') as stream:
        data = stream.read(limit + 1)
    _require(len(data) <= limit, f'Input exceeds {limit} bytes: {path.name}')
    return data


def _analyze(path):
    path = Path(path)
    document = json.loads(_read(path, 1024 * 1024).decode('utf-8-sig'), object_pairs_hook=_pairs)
    _require(isinstance(document, dict) and type(document.get('schema_version')) is int and document['schema_version'] == 1,
             'Expected character schema_version 1.')
    _require(_identifier(document.get('character_id')), 'Invalid character_id.')
    scale = document.get('render_scale', 1)
    _require(_number(scale) and .01 <= scale <= 4, 'render_scale must be finite and in 0.01..4.')
    sheets = _unique(document.get('sheets'), 8, 'sheet')
    frames = _unique(document.get('frames'), 4096, 'frame')
    animations = _unique(document.get('animations'), 256, 'animation')
    for frame in frames.values():
        _require(isinstance(frame.get('sheet'), str) and frame['sheet'] in sheets, 'Frame references a missing sheet.')
        rect, pivot = frame.get('rect'), frame.get('pivot')
        _require(isinstance(rect, list) and len(rect) == 4 and all(_integer(n) for n in rect)
                 and rect[0] >= 0 and rect[1] >= 0 and rect[2] > 0 and rect[3] > 0, 'Invalid frame rectangle.')
        _require(isinstance(pivot, list) and len(pivot) == 2 and all(_number(n) for n in pivot)
                 and all(-4096 <= n <= 4096 for n in pivot), 'Frame pivots must be finite and in -4096..4096.')
    _require(sum(f['rect'][2] * f['rect'][3] for f in frames.values()) <= 32_000_000,
             'Mapped frame pixels exceed the 32M analysis bound.')
    sequence_count = 0
    for animation in animations.values():
        _require(type(animation.get('loop')) is bool, 'Animation loop must be explicit boolean.')
        sequence = animation.get('frames')
        _require(isinstance(sequence, list) and 0 < len(sequence) <= 4096, 'Animation requires a bounded frame sequence.')
        sequence_count += len(sequence)
        for step in sequence:
            _require(isinstance(step, dict) and isinstance(step.get('frame'), str) and step['frame'] in frames, 'Animation references a missing frame.')
            duration = step.get('duration_ms')
            _require(_number(duration) and 0 < duration <= 60_000, 'Preview duration_ms must be finite and in (0, 60000].')
    _require(sequence_count <= 16_384, 'Animation sequences exceed the entry bound.')
    native_map = document.get('native_animation_map', {})
    _require(isinstance(native_map, dict) and len(native_map) <= 1024, 'Invalid native_animation_map.')
    _require(all(isinstance(k, str) and 0 < len(k) <= 256 and isinstance(v, str) and v in animations for k, v in native_map.items()),
             'Native animation mapping references a missing animation.')
    passthrough = document.get('native_passthrough', [])
    _require(isinstance(passthrough, list) and len(passthrough) <= 150
             and all(isinstance(name, str) and 0 < len(name) <= 256 for name in passthrough),
             'native_passthrough must be a bounded list of nonempty native names.')
    _require(len(set(passthrough)) == len(passthrough) and not set(passthrough).intersection(native_map),
             'native_passthrough must be unique and disjoint from native_animation_map.')
    report = {'schema_version': 1, 'character_id': document['character_id'], 'structurally_valid': True,
              'render_scale': scale, 'sheets': [], 'frames': [], 'animation_count': len(animations),
              'native_mapping_count': len(native_map), 'native_passthrough_count': len(passthrough), 'warnings': [],
              'interpretation': 'Structural review only. Preview timing does not replace native combat timing; native coverage is not established.'}
    if len(native_map) + len(passthrough) != 150 or 'portrait' not in animations:
        report['warnings'].append('Current runtime additionally requires exactly 150 mapped/passthrough native names and a portrait animation; native-name coverage must be checked against the exported collection.')
    assets, total_bytes, total_pixels = {}, 0, 0
    base = path.resolve().parent
    for sheet in sheets.values():
        sheet_scale = sheet.get('render_scale', scale)
        _require(_number(sheet_scale) and .01 <= sheet_scale <= 4, 'Sheet render_scale must be finite and in 0.01..4.')
        name = sheet.get('path')
        _require(isinstance(name, str) and name and not Path(name).is_absolute() and not PureWindowsPath(name).drive,
                 'Sheet paths must be relative and inside the manifest directory.')
        source = (base / name).resolve()
        _require(source.is_relative_to(base), 'Sheet paths must remain inside the manifest directory.')
        data = _read(source, 16 * 1024 * 1024)
        total_bytes += len(data)
        _require(total_bytes <= 32 * 1024 * 1024, 'Combined sheet input exceeds 32 MiB.')
        with Image.open(io.BytesIO(data)) as image:
            _require(image.format == 'PNG' and getattr(image, 'n_frames', 1) == 1, 'Sheets must be single-frame PNGs.')
            width, height = image.size
            _require(0 < width <= 4096 and 0 < height <= 4096 and width * height <= 4_000_000,
                     'Sheet exceeds 4096-axis or 4M-pixel bounds.')
            cells = grid_rectangles(width, height, sheet.get('columns'), sheet.get('rows'))
            image.verify()
        total_pixels += width * height
        _require(total_pixels <= 16_000_000, 'Combined sheets exceed 16M pixels.')
        with Image.open(io.BytesIO(data)) as image:
            rgba = image.convert('RGBA')
        try:
            alpha = rgba.getchannel('A')
            key = sheet.get('chroma_key')
            _require(key is not None or 'chroma_tolerance' not in sheet, 'chroma_tolerance requires chroma_key.')
            keyed_pixels = 0
            if key is not None:
                tolerance = sheet.get('chroma_tolerance', 32)
                _require(isinstance(key, list) and len(key) == 3 and all(_integer(n) and 0 <= n <= 255 for n in key), 'Invalid chroma_key.')
                _require(_integer(tolerance) and 0 <= tolerance <= 64, 'chroma_tolerance must be in 0..64.')
                difference = ImageChops.difference(rgba.convert('RGB'), Image.new('RGB', rgba.size, tuple(key)))
                channels = [band.point(lambda n: 255 if n <= tolerance else 0) for band in difference.split()]
                mask = ImageChops.multiply(ImageChops.multiply(channels[0], channels[1]), channels[2])
                keyed_pixels = mask.histogram()[255]
                alpha = ImageChops.multiply(alpha, ImageChops.invert(mask))
            counts = alpha.histogram()
            _require(sum(counts[:255]) > 0, 'Sheet requires transparent pixels or an explicit matching chroma key.')
            mapped = set()
            for frame in frames.values():
                if frame['sheet'] != sheet['id']:
                    continue
                x, y, w, h = frame['rect']
                _require(x + w <= width and y + h <= height, 'Frame rectangle extends outside its sheet.')
                bounds = alpha.crop((x, y, x + w, y + h)).getbbox()
                if bounds is None:
                    report['warnings'].append(f'Frame {frame["id"]} has no visible pixels.')
                report['frames'].append({'id': frame['id'], 'visible_bounds': list(bounds) if bounds else None,
                                         'rect': frame['rect'], 'pivot': frame['pivot'], 'render_scale': sheet_scale,
                                         'visible_size_world': [(bounds[2] - bounds[0]) * sheet_scale,
                                                                (bounds[3] - bounds[1]) * sheet_scale] if bounds else None})
                mapped.add(tuple(frame['rect']))
            unused = sum(tuple(cell) not in mapped for cell in cells)
            report['sheets'].append({'id': sheet['id'], 'width': width, 'height': height,
                                     'sha256': hashlib.sha256(data).hexdigest(), 'unmapped_grid_cells': unused, 'render_scale': sheet_scale,
                                     'transparent_pixels': counts[0], 'partial_alpha_pixels': sum(counts[1:255]),
                                     'keyed_pixels': keyed_pixels})
            if unused:
                report['warnings'].append(f'Sheet {sheet["id"]}: {unused} declared grid cells are not mapped as complete cells.')
            assets[sheet['id']] = 'data:image/png;base64,' + base64.b64encode(data).decode('ascii')
        finally:
            rgba.close()
    return document, report, assets


def inspect_character_manifest(path):
    """Validate explicit rectangles, pivots, preview timing, and alpha without writing."""
    return _analyze(path)[1]


def write_character_preview(path, output):
    """Write a new self-contained browser preview; never modify original sheets."""
    path, output = Path(path), Path(output)
    _require(path.resolve() != output.resolve(), 'Preview output must differ from the manifest.')
    if output.exists() or output.is_symlink():
        raise FileExistsError(output)
    _require(output.suffix.lower() == '.html', 'Preview output must end in .html.')
    document, report, assets = _analyze(path)
    # Only known presentation fields enter the document; no local paths are embedded.
    payload = {'frames': [{key: frame[key] for key in ('id', 'sheet', 'rect', 'pivot')} for frame in document['frames']],
               'animations': [{'id': animation['id'], 'loop': animation['loop'],
                               'frames': [{'frame': step['frame'], 'duration_ms': step['duration_ms']} for step in animation['frames']]}
                              for animation in document['animations']],
               'sheets': [{'id': sheet['id'], 'data': assets[sheet['id']], 'key': sheet.get('chroma_key'),
                           'scale': sheet.get('render_scale', document.get('render_scale', 1)),
                           'tolerance': sheet.get('chroma_tolerance', 32)} for sheet in document['sheets']]}
    encoded = json.dumps(payload, allow_nan=False).replace('<', '\\u003c').replace('>', '\\u003e').replace('&', '\\u0026')
    title = html.escape(document['character_id'])
    page = '''<!doctype html><html lang="en"><meta charset="utf-8">
<meta name="viewport" content="width=device-width"><title>Character preview: TITLE</title>
<style>body{background:#17191e;color:#eee;font:16px system-ui;margin:24px}canvas{image-rendering:pixelated;max-width:100%;background:repeating-conic-gradient(#555 0% 25%,#777 0% 50%) 0/20px 20px}select,button{font:inherit;padding:6px}p{max-width:80ch}pre{white-space:pre-wrap}</style>
<h1>TITLE animation review</h1><p>Preview timing is for art review only. Native combat timing, events, hitboxes, and animation coverage require separate verification. Original PNG files remain unchanged; declared chroma keys are applied only in memory.</p>
<label>Animation <select id="animations"></select></label> <button id="toggle">Pause</button>
<p id="status"></p><canvas id="view" width="640" height="640"></canvas>
<script type="application/json" id="data">PAYLOAD</script><script>
const data=JSON.parse(document.getElementById('data').textContent),sheets={},frames=Object.fromEntries(data.frames.map(f=>[f.id,f]));
const select=document.getElementById('animations'),view=document.getElementById('view'),ctx=view.getContext('2d'),status=document.getElementById('status'),scales=Object.fromEntries(data.sheets.map(s=>[s.id,s.scale]));
const bodyIds=new Set(data.animations.filter(a=>a.id!=='portrait').flatMap(a=>a.frames.map(f=>f.frame))),bodyFrames=data.frames.filter(f=>bodyIds.has(f.id)),cameraFrames=bodyFrames.length?bodyFrames:data.frames;
const left=Math.min(...cameraFrames.map(f=>-f.pivot[0]*scales[f.sheet])),right=Math.max(...cameraFrames.map(f=>(f.rect[2]-f.pivot[0])*scales[f.sheet]));
const top=Math.min(...cameraFrames.map(f=>-f.pivot[1]*scales[f.sheet])),bottom=Math.max(...cameraFrames.map(f=>(f.rect[3]-f.pivot[1])*scales[f.sheet]));
const zoom=Math.min(4,560/(right-left),560/(bottom-top)),originX=320-(left+right)*zoom/2,originY=320-(top+bottom)*zoom/2;
ctx.imageSmoothingEnabled=false;let index=0,paused=false,timer;
for(const a of data.animations){const o=document.createElement('option');o.textContent=a.id;select.append(o);}
async function load(){for(const s of data.sheets){const image=new Image();image.src=s.data;await image.decode();const c=document.createElement('canvas');c.width=image.width;c.height=image.height;const x=c.getContext('2d');x.drawImage(image,0,0);if(s.key){const p=x.getImageData(0,0,c.width,c.height);for(let i=0;i<p.data.length;i+=4){if(Math.max(...s.key.map((v,k)=>Math.abs(p.data[i+k]-v)))<=s.tolerance)p.data[i+3]=0;}x.putImageData(p,0,0);}sheets[s.id]=c;}show();}
function show(){clearTimeout(timer);const a=data.animations[select.selectedIndex],step=a.frames[index],f=frames[step.frame],r=f.rect,s=scales[f.sheet]*zoom;ctx.clearRect(0,0,640,640);if(a.id==='portrait'){const fit=Math.min(560/r[2],560/r[3]);ctx.drawImage(sheets[f.sheet],...r,320-r[2]*fit/2,320-r[3]*fit/2,r[2]*fit,r[3]*fit);}else ctx.drawImage(sheets[f.sheet],...r,originX-f.pivot[0]*s,originY-f.pivot[1]*s,r[2]*s,r[3]*s);status.textContent=a.id+' / '+f.id+' / '+step.duration_ms+' ms (preview only) / '+(a.id==='portrait'?'UI portrait fitted separately':'source-to-world scale '+scales[f.sheet]);if(!paused)timer=setTimeout(()=>{if(index+1<a.frames.length)index++;else if(a.loop)index=0;else return;show();},step.duration_ms);}
select.onchange=()=>{index=0;show();};document.getElementById('toggle').onclick=()=>{paused=!paused;document.getElementById('toggle').textContent=paused?'Play':'Pause';show();};load().catch(e=>status.textContent='Preview failed: '+e.message);
</script></html>'''.replace('TITLE', title)
    # The marker occurs once as script data; never substitute inside a user title.
    page = page.replace('<script type="application/json" id="data">PAYLOAD</script>',
                        '<script type="application/json" id="data">' + encoded + '</script>')
    with output.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(page)
    return {'output': str(output), 'report': report}
