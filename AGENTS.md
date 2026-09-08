# Development instructions

Read README.md and docs/design.md before changing behavior. The first milestone is local inspection and validation; do not describe it as a playable mod.

## Coordination

- Work on a feature branch in an isolated worktree. Default worktree parent: .worktrees/ (ignored).
- Give each implementer explicit file ownership and interface contracts. Integrate commits through the coordinating agent; do not edit another agent's worktree.
- Use synthetic fixtures and test behavior before implementation. Obtain an independent code review before merging completed work.
- Run `python -m unittest discover -s tests -v` from an environment with `python -m pip install -e .` completed.

## Project boundaries

- Inspection code must not execute game assemblies or write into Steam folders.
- Keep game files, ripped assets, generated reports, and credentials out of Git. Use ignored `local/` and `artifacts/` directories.
- Never extract an archive wholesale to discover its contents. Validate paths and bound decompression.
- Image providers are interchangeable. No API key belongs in source or test fixtures. Use synthetic images for CI.
- A valid sprite report means structural checks passed, not that art quality or game compatibility is established.
- Preserve pixel alignment and explicit animation metadata; do not infer hitboxes or frame timings from appearance.
