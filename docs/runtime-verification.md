# Runtime verification record

Local verification on 2026-09-08 uses Steam build 15664053, game assembly version 1.0.0.349, x64 .NET Framework 4.5.2. The launcher and staging script pin SHA256 hashes for TMNT.exe, ParisEngine.dll, and ParisSerializers.dll. Unsupported versions are rejected before executing game code.

## Observed

- The native scene probe parsed 42 FootSoldier base records using the installed serializer. This is a bounded record diagnostic, not a complete level parser or writer.
- The launcher installed Harmony hooks and reached the rendered intro and title screen. The user also confirmed seeing the intro video.
- Direct graphics backbuffer capture produced a 3440 by 1440 image. Windows window capture showed the desktop behind the game and was unsuitable for verification on this system.
- Runtime synthetic tests passed for save suppression/restoration, inherited hook resolution, exact encounter selection, baseline behavior, noncumulative position changes, and bitmap encoding/bounds. These surrogate tests do not prove the target was reached in gameplay.
- The scene probe synthetic boundary test and all 41 Python tests passed locally.
- The documented staging command succeeded against the installed game with default output/cache paths. The three original Steam assembly hashes still match their pinned values and the isolated copy.

## Encounter under test

The candidate is `FootSoldierRegular_202`, ID `6f229aef-a56f-4457-b5a0-60d158b48fb1`, in playfield `2d/level/playfield/stage/stage_01/level_01_art`. The native base record starts at `(483,228,0)`. The experiment changes only its initial X to 563 during reset. Scene identity, object name, ID, and expected original coordinates must match.

Live baseline verification passed at 19:12:31 UTC: Episode 1 logged `TARGET_BASELINE` at `(483,228,0)` and `TARGET_AFTER_RESET` confirmed both Position and InitialPosition retained those values across three resets. A gameplay capture showed Leonardo in the opening Channel 6 lobby.

Modified runtime verification passed at 19:22:00 UTC in `modified-runtime-4.log`: `ENCOUNTER_CHANGED` recorded `(483,228,0)` to `(563,228,0)`, and `TARGET_AFTER_RESET` confirmed Position and InitialPosition both equal `(563,228,0)`. Two subsequent resets encountered the already-modified InitialPosition and skipped another mutation. A 960 by 540 capture from this same process shows Episode 1 rendering with Leonardo in the opening lobby. The coordinate change is directly established by the runtime log; the lobby screenshot does not itself identify the target enemy or measure its displacement.

Earlier attempts exposed two launch issues. Changing display mode in Options triggered an FNA backbuffer creation error (`0x80070057`); the underlying invalid buffer parameters were not captured. Starting the process with `-WindowStyle Hidden` then hid its window despite music and rendering continuing. The successful run used normal window visibility and the user's saved windowed settings. Avoid changing display mode during this proof run. The launcher currently supplies `-Windowed` and `-scale=2`; these are forced graphics arguments rather than a guarantee that every saved graphics preference is honored.

## Repeating the check

Build using `docs/runtime-build.md`, run the baseline, and enter Episode 1. Close the playtest and run again without `--baseline`. Keep logs and screenshots under ignored `artifacts/`. The launcher suppresses the concrete save and delete methods in both modes; progress in this experiment is intentionally not saved. The original Steam installation is not patched.

For optional direct frame capture, set an absolute output filename before launch:

```powershell
$env:MALCOLM_CAPTURE_FRAME = "$PWD/artifacts/latest-frame.bmp"
```

Use a new filename for each launch in an existing directory. The launcher rejects existing diagnostic files and reparse paths, then keeps its newly created file handles open. It updates its own BMP every 120 Present calls, with dimensions bounded to 3840 by 2160 and file size to 32 MiB. Capture is diagnostic and can reduce performance. Remove the environment variable to disable capture. Logs, copied game binaries, and captured game images are local evidence and must not be committed.

To roll back the experiment, close `Malcolm.Runtime.exe` and launch the normal game through Steam. No installed assembly replacement is required.
