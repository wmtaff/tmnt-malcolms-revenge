"""JSON command-line interface for local mod-development evidence."""

import argparse
import json
import zipfile

from PIL import Image


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    for name in ('inspect-game', 'inventory-assets'):
        commands.add_parser(name).add_argument('path')
    sprite = commands.add_parser('validate-sprite')
    sprite.add_argument('path')
    sprite.add_argument('--width', type=int)
    sprite.add_argument('--height', type=int)
    sprite.add_argument('--max-colors', type=int)
    commands.add_parser('inspect-background').add_argument('path')
    background_preview = commands.add_parser('preview-background')
    background_preview.add_argument('path')
    background_preview.add_argument('output')
    commands.add_parser('inspect-character').add_argument('path')
    character_preview = commands.add_parser('preview-character')
    character_preview.add_argument('path')
    character_preview.add_argument('output')
    args = parser.parse_args(argv)
    try:
        if args.command == 'inspect-game':
            from .game import inspect_game
            report = inspect_game(args.path)
        elif args.command == 'inventory-assets':
            from .assets import inventory_archive
            report = inventory_archive(args.path)
        elif args.command == 'inspect-background':
            from .backgrounds import inspect_background
            report = inspect_background(args.path)
        elif args.command == 'preview-background':
            from .backgrounds import write_repeat_preview
            report = write_repeat_preview(args.path, args.output)
        elif args.command == 'inspect-character':
            from .characters import inspect_character_manifest
            report = inspect_character_manifest(args.path)
        elif args.command == 'preview-character':
            from .characters import write_character_preview
            report = write_character_preview(args.path, args.output)
        else:
            from .sprites import validate_sprite
            report = validate_sprite(args.path, width=args.width,
                                     height=args.height, max_colors=args.max_colors)
    except (OSError, ValueError, zipfile.BadZipFile, Image.DecompressionBombError) as exc:
        print(json.dumps({'error': str(exc), 'command': args.command}))
        return 2
    print(json.dumps(report, indent=2, ensure_ascii=True))
    return 0 if report.get('valid', True) and not report.get('unsafe_paths') else 1


if __name__ == '__main__':
    raise SystemExit(main())
