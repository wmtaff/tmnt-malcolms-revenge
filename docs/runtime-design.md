# Runtime encounter proof

User approved on 2026-09-08: prove mod-loader compatibility, inspect one level, and make a reversible visible enemy encounter change verified in game.

## Approach

Use a local isolated playtest copy of the installed Steam build. Inspect .NET metadata and game serializers to choose exact runtime hooks. Original tools and patch source can be versioned; game assemblies, extracted/decompiled data, saved games, and logs stay ignored. Existing read-only Python inspection commands retain their guarantees.

Prefer an in-memory Harmony patch through a small source-built launcher over overwriting shipped assemblies or guessing binary offsets. Start with logging a real startup hook. Then log and change one explicitly identified enemy spawn in Episode 1. Fail closed when expected types/signatures or target identifiers do not match. Preserve unrelated encounters and retain an unmodified launch path.

## Acceptance

- Compile against the actual installed runtime and record assembly fingerprints.
- Identify scene/playfield and encounter objects using actual metadata or deserialization evidence.
- Observe a patch executing in a running game; successful compilation alone is insufficient.
- Observe the chosen encounter change in game and correlate it with runtime logs.
- Keep patch operations reversible; never commit game content or secrets.
- Run synthetic tests for any reusable launch/configuration/patch selection logic and the existing suite; independent review before publishing.

## Boundaries

This phase permits controlled execution of game assemblies and a local playtest copy. The initial milestone's ban on execution is retained for the original Python inspectors and does not block this explicitly authorized runtime experiment. No generation provider calls are needed.
