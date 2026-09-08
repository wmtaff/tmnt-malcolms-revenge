# Three-wave Episode 1 prototype

The user approved a configurable encounter using existing assets, three short waves, adjusted positions and pacing, and repeatable launch/rollback controls on 2026-09-08. This extends the verified runtime spawn proof; it does not add a new episode slot or generated boss art.

Prefer the game's existing wave controller so camera locking, enemy death accounting, difficulty, and stage progression remain native. Identify the first three wave records and their member enemies using installed serializers or runtime inspection before constructing the sample. A configuration selects exact objects and expected original values, then supplies bounded replacement positions and delays. Reject malformed configuration before loading the game. Preflight all selected records before applying a wave so mismatches cannot produce a partially changed encounter.

Use a small JSON file with a versioned schema, exact scene identity, three named wave edits, and enemy selectors. Keep configuration parsing independent from game assemblies and test with synthetic fixtures. Use the source-built .NET Framework launcher and pinned assembly checks. Baseline remains available, and save writes remain suppressed in playtests.

Launch controls start a visible window, create fresh diagnostics, and track only the process they started. Stop/rollback validates PID, executable path, and start time before terminating that process. Normal Steam sessions are never targeted. Honor saved graphics preferences rather than forcing a resolution.

Acceptance: configuration and rollback tests pass; native wave/actor selectors are supported by local evidence; the sample loads in Episode 1 and logs each applied change; gameplay and wave transitions are playtested. Distinguish implementation completion from human gameplay verification. Keep all copied game data and captures ignored.
