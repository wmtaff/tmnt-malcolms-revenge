# Build a separate runtime playtest copy

On Windows, from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/build.ps1 -SourceGameDirectory 'D:/SteamLibrary/steamapps/common/TMNT'
```

The script uses the installed .NET Framework 4 compiler, targeting x64. It needs no SDK. It validates the three supported assembly hashes before creating output or downloading dependencies, then copies root game files and Content into `local/playtest`. It downloads the official Harmony 2.2.1 NuGet package to `local/dependencies`, verifies its pinned SHA256, extracts only the bounded `lib/net45/0Harmony.dll` entry, and compiles `tools/runtime/RuntimeLauncher.cs` and `tools/runtime/RuntimeLauncherTests.cs` to `Malcolm.Runtime.exe` with a Harmony reference. Both local directories are ignored by Git; do not distribute them or the game assets.

Optional `-PlaytestDirectory` and `-DependencyDirectory` parameters override those defaults. Source, destination and cache must be separate local directories. Reparse points are rejected in their ancestry and trees. A nonempty playtest directory is accepted only with `.malcolm-playtest.json` containing `{"schemaVersion":1,"owner":"malcolm-mod-runtime"}`. New or empty output receives this marker automatically. Only mark an existing directory when it is a disposable playtest copy. Rebuilds replace staged files and leave unrelated files in place; they do not clean old content. Existing file entries are unlinked before replacement so a hard link cannot cause a write into the source installation. Do not modify the directories concurrently with a build.

The owned playtest copy receives `steam_appid.txt` with app ID `1361510`, allowing Steam initialization when running the custom executable. This file is generated after copying the game files, leaving the source installation untouched.

The script never launches the game. After a successful build, close existing game instances and run the launcher explicitly:

```powershell
& ./local/playtest/Malcolm.Runtime.exe --self-test
& ./local/playtest/Malcolm.Runtime.exe "$PWD/local/playtest" "$PWD/local/playtest/runtime.log"
# Baseline without the patch:
& ./local/playtest/Malcolm.Runtime.exe "$PWD/local/playtest" "$PWD/local/playtest/baseline.log" --baseline
```

A successful build establishes compilation and staging only. Runtime self-test and actual visual verification remain separate checks.

Windows CI separately downloads and verifies the pinned Harmony package, compiles the launcher with the .NET Framework compiler, and runs `--self-test` without any game assets. This exercises the surrogate runtime patches; it does not replace a gameplay check.

