# TMNT: Malcolm's Revenge

A new-level mod project for the Steam edition of Teenage Mutant Ninja Turtles: Shredder's Revenge, with a reusable AI-assisted sprite pipeline.

**Status:** development foundations. The tools inspect local files and validate sprites. There is no playable custom level, verified mod loader, image-generation adapter, or game-ready sprite exporter yet.

## Quick start

Python 3.11 or newer is required. From the repository/worktree root:

```powershell
python -m venv .venv
.venv/Scripts/python -m pip install -e .
.venv/Scripts/python -m unittest discover -s tests -v
.venv/Scripts/python -m tmnt_mod inspect-game 'D:/SteamLibrary/steamapps/common/TMNT'
.venv/Scripts/python -m tmnt_mod inventory-assets 'C:/path/to/visual_assets.zip'
.venv/Scripts/python -m tmnt_mod validate-sprite 'C:/path/to/boss.png' --width 128 --height 128 --max-colors 64
```

On Linux/macOS use `.venv/bin/python`. Dimensions and palette limits above are examples, not verified TMNT constraints.

Commands print JSON to standard output. Exit status is 0 for successful reports, 1 for failed validation/unsafe archive paths/invalid installation evidence, and 2 for input or operational errors. Argument syntax errors use argparse's standard error output. Redirect output into an ignored `artifacts/` directory to save reports.

The game inspector hashes required assemblies and inspects compressed stage-data strings with bounded decompression. It does not execute game code or deserialize a complete level. The ZIP inventory does not extract or decode the entire archive. Sprite validation inspects pixels without modifying them and accepts individual frames up to 1,048,576 pixels (1024x1024 area); larger images are rejected before pixel processing. Split large sheets into frames before validation. Passing checks does not establish visual quality or game compatibility. Palette counts include distinct visible RGBA values; fully transparent pixels are excluded.

## Development approach

Use an integration worktree and separate branches for independent agent tasks. Each implementer owns explicit files, writes behavioral tests with synthetic assets, and submits a commit for independent review before integration. Keep source game assets and credentials out of Git. Do not auto-install historic mod-loader binaries into Steam: establish compatibility first.

The proposed generation pipeline is reference design -> controlled poses -> short frame sequences -> validation and manual review -> game-specific export. Providers remain replaceable. Benchmark OpenAI, PixelLab, and optionally Retro Diffusion on the same boss and idle/walk/attack/hurt sequences; compare native-size style, identity, attack readability, correction effort, and cost per accepted animation.

See [design and research](docs/design.md) and [implementation plan](docs/superpowers/plans/2026-09-08-foundations.md).
