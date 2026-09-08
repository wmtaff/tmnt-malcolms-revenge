# Foundation verification — 2026-09-08

Verified locally on Windows with Python 3.13 and Pillow 12.3.0. CI configuration covers Windows/Linux and Python 3.11/3.13; a configured workflow is not evidence of a completed hosted run.

## Local inputs

- Steam app 1361510, manifest build 15664053.
- Installation inventory: 3,463 files including 1,747 `.zpbn` and 1,502 `.zxnb` files.
- Required game/engine/serializer assemblies present and hashed.
- Stage data decompressed successfully; printable strings remain unparsed evidence.
- ZIP inventory: 227,054 non-metadata file entries: 226,705 PNGs, 208 palettes, 141 text files. No unsafe member paths reported. These are entries, not deduplicated assets.
- One selected local PNG: 32x32 RGBA, transparent, no partial alpha, two visible colors, structural validation passed. This effect frame is not representative evidence of character-generation quality.

Reports and the single inspected sample are in ignored `artifacts/`. No game files were modified or executed. No image-generation provider was called.

## Reproduction

Install with `python -m pip install -e .`, then run `python -m unittest discover -s tests -v`. See README.md for real-input commands. Tests generate synthetic archives/images/deflate streams and do not require owning the game.

## Remaining product gates

Runtime loader compatibility, scene deserialization/import, in-game encounter changes, provider comparison, atlas export, and a playable level remain unverified and unimplemented. Structural validation cannot judge animation identity, readability, or timing.

Final local suite: 31 tests passed. Independent review found one excessive-memory issue in sprite palette counting; a 1,048,576-pixel input limit now rejects oversized images before conversion, covered by API and CLI regression tests.
