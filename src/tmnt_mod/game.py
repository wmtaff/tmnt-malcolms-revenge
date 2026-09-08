"""Read-only installation evidence; stage strings are not a scene-schema parse."""

import hashlib
import os
from pathlib import Path
import re
import zlib

REQUIRED_ASSEMBLIES = ('TMNT.exe', 'ParisEngine.dll', 'ParisSerializers.dll')
STAGE_PATH = 'Content/StageData/StagesData.zpbn'
MAX_STAGE_BYTES = 8 * 1024 * 1024
MAX_COMPRESSED_BYTES = 8 * 1024 * 1024
MAX_STRINGS = 256
MAX_STRING_LENGTH = 256


def _inspect_stage(root, warnings):
    stage = root / STAGE_PATH
    report = {
        'path': STAGE_PATH, 'present': False, 'format': 'raw-deflate',
        'parsed': False, 'status': 'missing', 'compressed_size_bytes': None,
        'decompressed_size_bytes': None, 'strings': [], 'strings_truncated': False,
        'max_decompressed_bytes': MAX_STAGE_BYTES,
    }
    try:
        if not stage.is_file():
            warnings.append(f'Stage data is missing: {STAGE_PATH}')
            return report
        report['present'] = True
        report['compressed_size_bytes'] = stage.stat().st_size
        if report['compressed_size_bytes'] > MAX_COMPRESSED_BYTES:
            report['status'] = 'limit_exceeded'
            warnings.append(f'Stage compressed data exceeds {MAX_COMPRESSED_BYTES} bytes.')
            return report
        with stage.open('rb') as stream:
            compressed = stream.read(MAX_COMPRESSED_BYTES + 1)
        if len(compressed) > MAX_COMPRESSED_BYTES:
            report['status'] = 'limit_exceeded'
            warnings.append(f'Stage compressed data exceeds {MAX_COMPRESSED_BYTES} bytes.')
            return report
        decoder = zlib.decompressobj(wbits=-15)
        data = decoder.decompress(compressed, MAX_STAGE_BYTES + 1)
        if len(data) > MAX_STAGE_BYTES:
            report['status'] = 'limit_exceeded'
            warnings.append(f'Stage decompression exceeds {MAX_STAGE_BYTES} bytes.')
            return report
        if not decoder.eof:
            raise ValueError('incomplete raw-deflate stream')
        if decoder.unused_data:
            raise ValueError('trailing data after raw-deflate stream')
        report['decompressed_size_bytes'] = len(data)
        # Printable ASCII runs are evidence only; do not deserialize game types.
        for match in re.finditer(rb'[\x20-\x7e]{4,}', data):
            if len(report['strings']) >= MAX_STRINGS:
                report['strings_truncated'] = True
                break
            if match.end() - match.start() > MAX_STRING_LENGTH:
                report['strings_truncated'] = True
            report['strings'].append(data[match.start():min(match.end(), match.start() + MAX_STRING_LENGTH)].decode('ascii'))
        report['status'] = 'inspected'
    except (OSError, ValueError, zlib.error) as error:
        report['status'] = 'error'
        warnings.append(f'Cannot inspect {STAGE_PATH}: {error}')
    return report


def inspect_game(path):
    """Return JSON-compatible hashes, inventory and bounded stage string evidence.

    Valid means the required assemblies are readable and available, inventory
    completed, and any present stage data passed bounded raw-deflate inspection.
    It does not establish game/mod compatibility. Missing optional stage data
    produces a warning without invalidating the assembly inspection.
    """
    root = Path(path).absolute()
    warnings = []
    report = {
        'path': str(root), 'valid': True, 'required_assemblies': {},
        'inventory': {'total_files': 0, 'by_extension': {}},
        'stage_data': None, 'warnings': warnings,
    }
    if not root.is_dir():
        report['valid'] = False
        warnings.append(f'Installation directory is missing or not a directory: {root}')
    for name in REQUIRED_ASSEMBLIES:
        assembly = root / name
        entry = {'present': False, 'size_bytes': None, 'sha256': None}
        report['required_assemblies'][name] = entry
        try:
            if not assembly.is_file():
                raise FileNotFoundError(f'Required assembly is missing: {name}')
            entry['present'] = True
            entry['size_bytes'] = assembly.stat().st_size
            digest = hashlib.sha256()
            with assembly.open('rb') as stream:
                for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                    digest.update(chunk)
            entry['sha256'] = digest.hexdigest()
        except OSError as error:
            report['valid'] = False
            warnings.append(f'Cannot inspect {name}: {error}')

    counts = {}

    def inventory_error(error):
        report['valid'] = False
        warnings.append(f'Cannot inventory installation: {error}')

    if root.is_dir():
        for directory, subdirs, files in os.walk(root, onerror=inventory_error, followlinks=False):
            subdirs.sort()
            for name in sorted(files):
                extension = Path(name).suffix.lower() or '[no extension]'
                counts[extension] = counts.get(extension, 0) + 1
    report['inventory'] = {'total_files': sum(counts.values()), 'by_extension': dict(sorted(counts.items()))}
    report['stage_data'] = _inspect_stage(root, warnings)
    if report['stage_data']['status'] in ('error', 'limit_exceeded'):
        report['valid'] = False
    return report
