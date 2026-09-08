# Episode 1 encounter prototype

The sample `encounters/episode1-lobby.json` edits the first three existing waves of CamBlock01. It uses the installed game's Foot Soldiers, camera block, difficulty rules, and wave progression. It does not create a new level slot or replace the rest of Episode 1.

| Wave | Enemy | Original position | Prototype position | Delay to next wave |
|---|---|---|---|---|
| Lobby entry | Regular_202 | 483,228,0 | 523,228,0 | 0.25 → 0.75 seconds |
| Lower approach | Regular_204 | 552,224,0 | 572,244,0 | 0.25 → 1.25 seconds |
| Upper pressure | Regular_106 | 616,224,0 | 596,208,0 | 4 → 2 seconds |

These are initial playtest choices, not validated balance. The delay is the native delay **to the next wave**, not a spawn timer. Native completion thresholds remain 0, 0, and 1; the third wave therefore retains the game's existing overlap behavior with the following wave. Each of these three native groups contains one selected enemy.

Before the first wave starts, the runtime checks scene, camera block identity, all three wave groups and expected delays, group membership, enemy identity, and original positions. Only after every check succeeds does it apply the edits. A mismatch logs `ENCOUNTER_REJECTED` and leaves the original encounter in place. Later `NATIVE_ENEMY_SPAWN` entries report the position after the game resets each enemy. Configuration is read once per launch; restart after editing it.

The JSON schema requires exactly three ordered wave indexes (0,1,2) in the same block. Input is bounded to 64 KiB, 32 enemy edits per wave, finite coordinates in ±100000, and delays from 0 to 60 seconds. Unknown fields, duplicate keys and selectors, and type coercions are rejected. Those bounds prevent malformed input; they do not establish that arbitrary coordinates are playable.

See [playtest controls](playtest-controls.md) for visible launch, status, and rollback. The baseline mode disables encounter edits. Saved display preferences are honored. Progress remains unsaved in both modes.

## Verification status

Live playtest passed on 2026-09-08. The isolated prototype process loaded Episode 1 and applied all three wave edits at 19:58:02 UTC. Native spawn logs confirmed active enemies at `(523,228,0)`, `(572,244,0)`, and `(596,208,0)` for wave indexes 0, 1, and 2. Native wave index 3 started at 19:58:06 UTC, preserving progression into the remainder of the encounter. No encounter rejection, rollback abort, or fatal error appeared in the checked log. The user reported defeating the first three enemies and seeing another arrive from the left. A gameplay capture corroborated the running encounter.

The fourth wave is intentionally unchanged. Logged start intervals were approximately 0.74, 1.28, and 2.03 seconds, consistent with the configured delays. This verifies native progression and configured spawns, not a guarantee that each wave waits for every previous enemy's death under all game states. The native completion logic and thresholds were preserved.

All 47 Python tests pass, including synthetic launch/status/stop coverage. An independently compiled test program containing only the configuration and edit/rollback logic (no Harmony, launcher, or game code) passes the configuration tests, adapter tests, and sample-file validation. Review also verified the native hook signatures against the installed game.

The combined runtime self-test and all four Windows/Linux Python matrix jobs passed in GitHub Actions at code commit `50a9206`. Microsoft Defender initially blocked the local combined executable as `Trojan:MSIL/Injuke.AMMA!MTB`; the user subsequently added an exclusion. After rebuilding, the combined self-test passed locally, the full staging build succeeded, and the live test above completed. The assistant did not change Defender settings, and the underlying reason for the detection remains unestablished.
