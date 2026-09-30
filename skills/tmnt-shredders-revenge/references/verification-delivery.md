# Verification, troubleshooting and delivery

Use [verification-record.md](../assets/verification-record.md) to separate structural tests, native-hook execution, visual observation and user feedback. Record revision and evidence for the final integrated build; historic test counts are not a current pass criterion.

## Acceptance sequence

1. Inspect the user's installation and compare supported hashes. Record framework revision and local prerequisites.
2. Run the installed Python suite and relevant synthetic native suites through `build.ps1`; run launcher `--self-test` explicitly. Standalone native-shaped fixtures remain outside the game launcher.
3. Inspect/preview each art pack, compare exact native map-plus-passthrough membership with the exported contract, and review all structural warnings. Preserve source hashes.
4. Launch the isolated visible build with explicit primary mode and optional character manifest. Record `artifacts/playtest-session.json`, its exact command/mode, log and capture. Do not confuse another worktree's older executable with the current build.
5. Observe selection label and portrait, then the actual controlled character. Check idle, walk/run, facing, jump/land, attack contact/recovery, hurt, knockdown/getup, grab/throw, specials and victory. Keep native effects distinct from the body. Review every UI collection separately.
6. Play the intended route in order: camera/collision, unique landmarks/seams, encounter activation and defeat thresholds, boss lifecycle, victory and retry. Logs of boss-fight entry do not establish victory.
7. Stop the tracked session and verify normal Steam launch remains available. Package only original source/art/configuration, prompts and redacted evidence; the other machine stages from its own game installation.

When permitted computer-control tools are available, use their observed-window workflow for live interaction. Brief automated key presses may be missed by game polling. If input is unreliable, ask the user to navigate while continuing read-only logs/diagnostics. If the user stops computer control, stop that interaction and report exactly what remains unverified; do not infer the unseen result from draw logs.

## Diagnostic ladder

| Evidence or symptom | Interpretation / next check |
| --- | --- |
| Unsupported build hash | Investigate/port native contract; do not bypass allowlist |
| Music, no visible window | Check tracked executable and visible launch; avoid `Hidden` for the interactive game |
| `CHARACTER_ART_PREFLIGHT_COMPLETE` | CPU manifest accepted; not GPU or visual proof |
| `MALCOLM_CHARACTER_READY` / hooks ready | Hooks installed; not evidence that a target matched |
| Malcolm name with Leo image | Inspect actual normalized texture-folder selectors; `Path` is not the asset filename |
| `CHARACTER_ART_OBSERVED` only | Compare observed folders to exact donor/UI targets; investigate bounded path diagnostics |
| `CHARACTER_COVERAGE` | Actual native names covered; not bespoke animation quality |
| `CHARACTER_ART_RENDERED` | Custom draw executed; still inspect position, alpha, size and shader result |
| `CHARACTER_ART_FALLBACK` | Native appearance retained after error; inspect the logged reflection/upload/draw exception |
| `CHARACTER_ART_RESTORE_FAILED` | Shader restoration failed; fix state handling before further acceptance |
| Body duplicates during attacks | Native effects were mapped to a body instead of explicit passthrough |
| Feet/body jump | Compare pivots, source proportions, sheet scale and native sampling, not only preview durations |
| `BACKGROUND_RENDERED` | Three panels drawn; not a reconstructed route or playable geometry proof |
| `ENCOUNTER_REJECTED` | Exact preflight failed and original encounter remains; inspect selectors and expected values |
| Waves advance surprisingly fast | Check native completion thresholds/activation; delay is to next wave, not proof of kills |

Some native content loads produce handled first-chance exceptions during fallback lookup. Investigate final failure/fallback and observed behavior rather than assuming every first-chance message is fatal. Do not ignore a renderer-specific failure because startup continued.

## Publish reusable work

Keep a focused feature branch and independently reviewed commits. Include a concise problem/result summary, validated revision, actual commands and evidence, known limitations and rollback. A fresh user should use current `main` and record its revision; historical feature branches may lack the later runtime/character work.

In the project documentation link the skill entrypoint, setup, example manifest/provenance and current verification record. Historical design proposals should be marked historical when implementation supersedes them. Keep exact home addresses, personal reference photos, native exports, logs containing private paths and binaries out of commits. Generated character/level art may be distributed only within the user's intended scope; the skill package itself contains no personal character images.

For a skill release, validate frontmatter/relative links and run a fresh-context scenario using only the installed bundle and repository location. Check fresh-machine commands, changed-build handling, animation timing semantics, and honest route capability. Structural validation alone cannot prove that another agent can follow it. Install the skill as an independent folder under the target Codex skills directory; do not rely on links into a developer's worktree. Record source revision to make later updates deliberate.
