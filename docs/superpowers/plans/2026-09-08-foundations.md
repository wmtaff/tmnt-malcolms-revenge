# Mod and asset foundations implementation plan

> **For agentic workers:** Use superpowers:subagent-driven-development. Work in assigned worktrees, test behavior before implementation, and request independent review.

**Goal:** Deliver runnable read-only inspection and sprite validation tools.

**Architecture:** Independent game, archive, and sprite modules return dictionaries consumed by a JSON CLI. Generated reports stay local. No game or model dependencies are executed.

**Tech Stack:** Python 3.11+, Pillow, unittest, GitHub Actions.

**Spec:** docs/design.md

## Global constraints

- Never write to the Steam installation.
- Never commit source-game assets, credentials, or generated reports.
- Read ZIP metadata without extraction; bound decompression.
- Test with synthetic files. No paid generation.

## Task 1: Game inspection

Files: src/tmnt_mod/game.py, tests/test_game.py.
Interface: inspect_game(path) -> dict.
- [ ] Write and run failing tests for missing assemblies, real SHA-256 output, valid raw-deflate input, corrupt input, and decompression size limits.
- [ ] Implement installation inspection with stable JSON output and explicit warnings for unreadable/unsupported content.
- [ ] Run module tests and commit on the assigned branch.

## Task 2: Asset inspection

Files: src/tmnt_mod/assets.py, src/tmnt_mod/sprites.py, tests/test_assets.py, tests/test_sprites.py.
Interfaces: inventory_archive(path) -> dict; validate_sprite(path, width=None, height=None, max_colors=None) -> dict.
- [ ] Write and run failing tests using temporary ZIPs and Pillow-created images: metadata filtering, traversal reporting, dimensions, alpha, palette limits, empty sprites, and edge contact.
- [ ] Implement read-only reporting with actionable violations and no extraction or pixel mutation.
- [ ] Run module tests and commit on the assigned branch.

## Task 3: CLI and integration

Files: pyproject.toml, src/tmnt_mod/__main__.py, tests/test_cli.py, README.md, .github/workflows/test.yml.
- [ ] Write failing CLI subprocess tests for JSON output, exit codes, and invalid inputs.
- [ ] Implement argparse commands, controlled errors, packaging, and Windows/Linux CI.
- [ ] Integrate reviewed agent commits and run all tests.
- [ ] Run inspection against the local Steam game and ZIP; save reports to ignored artifacts/.
- [ ] Obtain independent review, fix substantive findings, commit verified results.
