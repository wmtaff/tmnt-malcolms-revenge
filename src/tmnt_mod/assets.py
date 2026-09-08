"""Read ZIP inventories without opening or extracting archive members."""

from collections import Counter
from pathlib import Path, PurePosixPath, PureWindowsPath
from zipfile import ZipFile


def inventory_archive(path):
    """Count non-metadata files, including unsafe names, using ZIP headers only.

    Categories use the first directory after an optional common wrapper.
    Unsafe names remain in totals and are reported separately. Byte sizes are
    unverified declarations from the archive, not decompressed measurements.
    """
    path = Path(path)
    files = []
    unsafe = []
    ignored = 0
    with ZipFile(path) as archive:
        for member in archive.infolist():
            name = member.orig_filename
            parts = name.replace('\\', '/').split('/')
            if (name.startswith(('/', '\\')) or PureWindowsPath(name).drive
                    or '\x00' in name or '..' in parts or any(':' in part for part in parts)):
                unsafe.append(name)
            if any(part == '__MACOSX' or part == '.DS_Store' or part.startswith('._')
                   for part in parts):
                if not member.is_dir():
                    ignored += 1
                continue
            if member.is_dir() or name.endswith('\\'):
                continue
            files.append((member, parts))

    # Only treat a common directory as a wrapper when every file sits at least
    # two levels deep; a sole category with direct files keeps its name.
    wrapper = (files[0][1][0] if files and all(len(parts) >= 3 for _, parts in files)
               and len({parts[0] for _, parts in files}) == 1 else None)
    categories = Counter()
    extensions = Counter()
    for member, parts in files:
        relative = parts[1:] if wrapper else parts
        categories[relative[0] if len(relative) > 1 else '(root)'] += 1
        extensions[PurePosixPath(parts[-1]).suffix.lower() or '(none)'] += 1
    return {
        'path': str(path),
        'total_files': len(files),
        'ignored_metadata': ignored,
        'categories': dict(sorted(categories.items())),
        'extensions': dict(sorted(extensions.items())),
        'total_uncompressed_bytes': sum(member.file_size for member, _ in files),
        'total_compressed_bytes': sum(member.compress_size for member, _ in files),
        'unsafe_paths': unsafe,
        'warnings': ['Archive contains unsafe member paths; do not extract them.'] if unsafe else [],
        'valid': not unsafe,
    }
