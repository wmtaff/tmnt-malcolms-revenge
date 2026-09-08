# Start, inspect, and stop an isolated playtest

Build the separate owned copy first using [runtime build instructions](runtime-build.md). Close any existing game instance, then run these commands from the repository root:

```powershell
# Use encounters/episode1-lobby.json and optionally capture rendered frames.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Start -Capture

# Inspect the recorded process.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Status

# Stop only that recorded playtest.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Stop

# Start a baseline comparison without encounter changes.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Start -Baseline

# Or select another encounter configuration.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Start -Encounter 'C:/path/encounter.json'
```

`-Baseline` and `-Encounter` are mutually exclusive. Start without either selects the repository's `encounters/episode1-lobby.json`. The launcher validates encounter contents. The controls require that configuration file to exist before starting, and require the staged `Malcolm.Runtime.exe` and ownership marker `{"schemaVersion":1,"owner":"malcolm-mod-runtime"}`. They do not force window size or change saved graphics preferences. Launches use a normal visible window.

Defaults are `local/playtest` and `artifacts`. Override them with `-PlaytestDirectory` and `-ArtifactsDirectory`, using the same paths for Start, Status, and Stop. Each run has a unique log filename and, with `-Capture`, a unique BMP filename. Capture output is supplied through `MALCOLM_CAPTURE_FRAME`; the controller restores its prior environment value after spawning the child. Paths with spaces are serialized as individual Windows command-line arguments.

`artifacts/playtest-session.json` records the PID, UTC start-time ticks, executable path, mode, and diagnostic paths. The active session is written with exclusive creation. Completed records are preserved under unique `session-*.json` names on the next Start. A lock prevents overlapping control operations from replacing session identity. Status without a recorded session does not create files.

Stop checks both the recorded start time and exact executable path against the live process, retaining its OS handle during the check and termination. A stale or mismatched identity is refused. It never stops processes by a generic name such as `TMNT`, and it does not stop a manually launched game that has no matching session record. Use the game's normal exit control for untracked sessions.

For rollback, stop the playtest, then launch the game normally through Steam. The encounter patch exists only in the playtest process; no files in the source installation are changed by these controls. The script does not automatically launch Steam. Keep the session JSON and diagnostics when comparing baseline and encounter runs.

The Windows tests compile a tiny synthetic executable to exercise argument passing, capture settings, fresh sessions, status, and identity-checked stopping. They do not launch game assemblies.
