# Native camera-wave probe

This opt-in Framework diagnostic executes the installed game serialization code
without starting the game or a graphics device. It is separate from the Python
nonexecuting inspection commands. It writes only stdout/stderr; redirect output
to ignored `artifacts/`.

```powershell
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:artifacts\WaveProbe.exe tools\wave-probe\WaveProbe.cs
& .\artifacts\WaveProbe.exe --self-test
& .\artifacts\WaveProbe.exe 'D:/SteamLibrary/steamapps/common/TMNT' 'D:/SteamLibrary/steamapps/common/TMNT/Content/2d/Level/Scene2d/Stage/Stage_01/Level_01_complete.zpbn' > artifacts/waves.txt
```

The probe bounds raw-deflate output to 16 MiB and searches complete
length-prefixed CameraBlockTrigger reader tokens. For each candidate it invokes
the native CameraBlockTriggerReadWriter, including inherited field readers. An
inert ContextManager with EditorMode false allows its property setters to run.
The binary content manager registry is initialized without a running context.
Unexpected candidates fail the process; this is not a general scene parser.

Reports include native block identifiers, coordinates, wave Group.ID strings,
delays, thresholds, activation state, and available difficulty. Group IDs include
the leading `#`. The probe does not resolve group membership or instantiate enemies.
Use the existing scene base-record probe for enemy identifiers and positions;
runtime validation must resolve membership through Group.GetGroup().Members.

For the first three existing waves, a runtime adapter can validate the exact
CameraBlockTrigger and its Waves list in an InternalStartWave prefix, before the
native method resets and activates group members. Validate every configured wave
and member before applying any edit. Keep native difficulty, threshold, cleanup,
and activation behavior. A successful report alone is not an in-game verification.
