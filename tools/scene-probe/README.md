# Native scene base-record probe

This separate, opt-in .NET Framework diagnostic loads the user's installed game
assemblies and invokes their serialization code. It does not launch the game or
write installation files. It is outside the nonexecuting Python inspector.
Use only with a trusted local installation; assembly loading executes code.

From the worktree root in PowerShell:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:artifacts\SceneProbe.exe tools\scene-probe\SceneProbe.cs
& .\artifacts\SceneProbe.exe --self-test
& .\artifacts\SceneProbe.exe 'D:/SteamLibrary/steamapps/common/TMNT' 'D:/SteamLibrary/steamapps/common/TMNT/Content/2d/Level/Scene2d/Stage/Stage_01/Level_01_complete.zpbn' > artifacts/scene-probe.txt
```

The probe bounds raw-deflate output at 16 MiB. It locates complete length-prefixed
`ParisSerializer.FootSoldierReadWriter` tokens, then invokes the installed
`GameObject2dReadWriter.Read(BinaryReader, ref object)` to obtain base fields.
It does not use handwritten binary offsets for those fields. It reads each
candidate into a base GameObject2d, so enemy-specific properties are deliberately
not reported. Token matching is a candidate search, not a complete scene grammar;
unexpected candidates fail the command rather than silently yielding partial
success. The self-test checks the search's length prefix and EOF boundaries.

The binary content manager normally requires a live game context. This isolated
process initializes only its reader registry using reflection. This is a
diagnostic technique for the tested installation, not a supported engine API.
Full native scene deserialization additionally invokes texture/content loading
through property setters and cannot currently run headlessly with this probe.
No serializer writes, scene round-trip, or in-game compatibility are claimed.

Observed on the local installation, 2026-09-08: 42 FootSoldier base records parsed.
`Paris.Game.Actor.FootSoldier` derives from `FootBasic`, then `EnemySpawn`.
The early encounter candidate `FootSoldierRegular_202` has ID
`6f229aef-a56f-4457-b5a0-60d158b48fb1`, Position and InitialPosition `(483,228,0)`,
and StartActive `true`. A reversible runtime test can select this exact object
and shift its initial X to 563. Visual confirmation is still required.

Scene identity and playfield identity differ: the scene file is
`2d\Level\Scene2d\Stage\Stage_01\Level_01_complete`; its trailing playfield
string is `2d\Level\Playfield\Stage\Stage_01\Level_01_art`.
Do not compare Scene2d.PlayfieldPath to a Scene2d file path. A runtime log should
confirm the playfield value before claiming an exact scene guard works.

The installed schema also exposes `CameraBlockTrigger` and `WaveInfo` with
Group, Disabled, StartActive, AvailableDifficulty, EnemyThresholdToNextWave,
and DelayToNextWave. Those fields were inspected as metadata; this tool does not
decode complete wave objects. Keep diagnostic outputs and game data in ignored
`artifacts/` only.
