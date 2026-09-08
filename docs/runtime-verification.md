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

Live baseline and modified encounter verification remain pending. Required evidence is `TARGET_BASELINE` with the original coordinates, then `ENCOUNTER_CHANGED` and the reset result in a separate modified run, correlated with visible Episode 1 gameplay. Reaching the intro or passing a synthetic test does not satisfy this requirement.

## Repeating the check

Build using `docs/runtime-build.md`, run the baseline, and enter Episode 1. Close the playtest and run again without `--baseline`. Keep logs and screenshots under ignored `artifacts/`. The launcher suppresses the concrete save and delete methods in both modes; progress in this experiment is intentionally not saved. The original Steam installation is not patched.

For optional direct frame capture, set an absolute output filename before launch:

```powershell
$env:MALCOLM_CAPTURE_FRAME = "$PWD/artifacts/latest-frame.bmp"
```

Use a new filename for each launch in an existing directory. The launcher rejects existing diagnostic files and reparse paths, then keeps its newly created file handles open. It updates its own BMP every 120 Present calls, with dimensions bounded to 3840 by 2160 and file size to 32 MiB. Capture is diagnostic and can reduce performance. Remove the environment variable to disable capture. Logs, copied game binaries, and captured game images are local evidence and must not be committed.

To roll back the experiment, close `Malcolm.Runtime.exe` and launch the normal game through Steam. No installed assembly replacement is required.
