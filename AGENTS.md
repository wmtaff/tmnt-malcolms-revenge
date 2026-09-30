# Agent instructions: TMNT Shredder's Revenge

This is the shared repository guide for Codex, Claude Code, and other coding agents. Work directly from this checkout; no globally installed skill, remembered conversation, or original developer's machine is required.

## Start here

1. Read [README.md](README.md) for current capability and [the bundled skill](skills/tmnt-shredders-revenge/SKILL.md) for the workflow relevant to the user's task. Treat that file as ordinary local guidance if your harness does not support skills. Load only the relevant linked reference.
2. Inspect `git status --short` and `git log -1 --oneline`. Preserve unrelated changes. Locate the actual game installation and references only if the task needs them; do not reuse example paths or ask for the game to perform documentation/Python work.
3. For a new-machine task, follow [setup](skills/tmnt-shredders-revenge/references/setup.md). For characters/animation, use [characters](skills/tmnt-shredders-revenge/references/characters-animation.md); for route scenery/levels, use [levels](skills/tmnt-shredders-revenge/references/levels-routes.md).
4. Establish the requested outcome and existing evidence, then implement and verify the authorized work. Ask for missing inputs when they matter; repository instructions do not themselves authorize paid generation, map uploads, or unrelated external actions.

## Current capability

The framework has read-only inspectors, an isolated Windows runtime, guarded three-wave edits, three scenic panels over the native Baxter approach, and a custom Leo-slot appearance/name adapter. Malcolm was observed in game, with jumpy animation reported. Smooth animation, full boss-victory acceptance, independent roster slots, arbitrary route loading and a PixelLab client are not established. Continuous Maps address-to-address reconstruction is extension work, not an available command.

Historical proposals in `docs/design.md` and `docs/custom-player-plan.md` are background, not current completion reports. Consult `docs/character-verification.md` and `docs/residential-verification.md` for evidence.

## Setup and checks

Python 3.11+ works for inspectors/tests without the game. From repository root:

```powershell
python -m venv .venv
& ./.venv/Scripts/python.exe -m pip install -e .
& ./.venv/Scripts/python.exe -m unittest discover -s tests -v
& ./.venv/Scripts/python.exe -m tmnt_mod --help
```

On Linux/macOS use `.venv/bin/python`. Check exit codes and fix relevant failures. Do not assume a historical test count is the current count. Runtime build/playtest requires Windows x64, the supported game hashes and the Framework compiler; use the exact commands in the setup reference. Missing game/provider access does not block independent source, docs or synthetic-test work. Report the specific remaining verification limitation.

## Source map

- `src/tmnt_mod/`, `tests/`: inspection, art validation/preview and synthetic tests.
- `tools/runtime/`: build, CLI, native hooks and tracked Start/Status/Stop playtest controls.
- `tools/player-assets/`: bounded native contract export and character art loading.
- `art/`, `encounters/`: original example artwork, prompts and reviewed configuration.
- `skills/tmnt-shredders-revenge/`: portable workflow knowledge and planning templates.
- `docs/`: detailed contracts, procedures and verification evidence.

## Coordination

- Work on a feature branch in an isolated worktree. Default worktree parent: .worktrees/ (ignored).
- When parallel agents are available and useful, give each implementer explicit file ownership and interface contracts. Integrate commits through the coordinator; do not edit another agent's worktree. A single-agent harness can perform the same sequence itself.
- Use synthetic fixtures and test behavior before implementation. Obtain an independent code review before merging completed work.
- Run `python -m unittest discover -s tests -v` from an environment with `python -m pip install -e .` completed.

## Project boundaries

- Inspection code must not execute game assemblies or write into Steam folders.
- The separately authorized runtime experiment may execute game assemblies in an isolated local playtest copy. Do not weaken the original inspection commands' read-only guarantees.
- Keep game files, ripped assets, generated reports, and credentials out of Git. Use ignored `local/` and `artifacts/` directories.
- Never extract an archive wholesale to discover its contents. Validate paths and bound decompression.
- Image providers are interchangeable. No API key belongs in source or test fixtures. Use synthetic images for CI.
- A valid sprite report means structural checks passed, not that art quality or game compatibility is established.
- Preserve pixel alignment and explicit animation metadata; do not infer hitboxes or frame timings from appearance.

## Native and art traps

- Never bypass the supported-build hash gate by updating hashes alone. Inspect and port changed native contracts first.
- `AnimatedObject2dData.Path` stores the texture folder, not the full asset name. Match exact normalized donor/UI folders.
- Native character timing/events remain authoritative. Manifest `duration_ms` controls the preview, not gameplay. Diagnose pivots, body scale and native frame sampling when motion jumps.
- Keep native effects in explicit passthrough mappings; mapping them to a body creates duplicate characters.
- Full-color artwork uses the custom shader path and premultiplied RGBA. A painted checkerboard is not transparency.
- Keep synthetic native-shaped fixture classes in separate test executables. Update both local build and CI source lists when adding runtime modules.
- Start the interactive playtest visibly. Stop only the tracked owned process before rebuilding; normal Steam files and saves stay untouched.

## Finish a change

Run relevant tests, `git diff --check`, and review the integrated diff. Separate compilation, manifest validation, hook execution, direct visual observations and user feedback. Record exact commands, revision, outcomes and unverified behavior. Update reusable workflow documentation when interfaces or lessons change. Do not claim a rendered character, smooth animation, geographic route or completed level based only on passing tests or draw logs. Keep commits focused and publish only within the user's requested scope.
