# Malcolm's Revenge: first development milestone

Build a local, provider-independent Python toolkit for the Steam mod and future sprite production. The initial deliverable is inspection and validation, not a playable level or a proven image generator.

## Scope and interfaces

- `tmnt_mod.game.inspect_game(path)` returns JSON-compatible installation evidence: required assemblies, SHA-256 hashes, file inventory by extension, and bounded raw-deflate stage-data inspection. It never writes into the installation. Decompressed strings are evidence, not a parsed scene schema.
- `tmnt_mod.assets.inventory_archive(path)` counts archive assets by category and extension without extraction; ignores macOS metadata and reports unsafe member paths.
- `tmnt_mod.sprites.validate_sprite(path, width=None, height=None, max_colors=None)` reports image dimensions, alpha properties, visible bounds, palette count, and constraint violations without changing pixels.
- CLI commands `inspect-game`, `inventory-assets`, and `validate-sprite` print JSON; sprite constraint failures return nonzero status.
- Tests use synthetic fixtures, never committed game assets. Python 3.11+; Pillow is the sole runtime dependency. Windows and Linux CI.

## Next gates

1. Establish runtime mod-loader compatibility and deserialize one scene using the installed assemblies.
2. Change one encounter and verify in game, then build a short level in an existing episode slot.
3. Benchmark OpenAI and optional PixelLab/Retro Diffusion using one approved boss design and four animations.
4. Implement atlas/animation export only after frame rectangles, pivots, palette representation, and timing are verified.

Do not assume generated sheets match the engine contract. No paid API calls, bulk extraction, game patching, or game-asset redistribution in this milestone. Keep local reports under ignored artifacts/.

## Research sources

- https://github.com/Platonymous/TMNTModAPI
- https://github.com/aedenthorn/TMNTModLoader
- https://github.com/htdt/godogen
- https://www.pixellab.ai/docs/tools/animate-with-skeleton
- https://www.pixellab.ai/pixellab-api
- https://github.com/Retro-Diffusion/api-examples
- https://developers.openai.com/api/docs/guides/image-generation

These are research leads; loader compatibility and provider quality have not been established.
