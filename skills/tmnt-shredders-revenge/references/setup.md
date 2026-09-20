# Fresh installation and reproducible setup

## Locate source and game

The skill is instructions; executable tooling lives in the framework repository:
https://github.com/wmtaff/tmnt-malcolms-revenge

Use an existing checkout when available. The portable skill was authored against framework commit `8be3c86` (2026-09-08); its branch contains character tooling missing from the original `main`. The skill publication branch is `codex/tmnt-reusable-skill`. Inspect the chosen revision before using different flags or upgrading dependencies.

For a fresh checkout:

```powershell
git clone --branch codex/tmnt-reusable-skill https://github.com/wmtaff/tmnt-malcolms-revenge.git tmnt-mod
Set-Location tmnt-mod
git rev-parse HEAD
```

Locate the user's actual game through Steam > Manage > Browse local files, or inspect that machine's Steam library folders. Do not reuse the author's drive letter. The Steam app ID is `1361510`; the executable is `TMNT.exe`. A copied skill does not include the game, assets ZIP, generated character pack, or Python environment.

Windows x64 is required for this native adapter. Python tooling requires Python 3.11+, with dependencies declared by `pyproject.toml`. Native compilation uses the .NET Framework compiler at `C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe`; verify it exists. The build script downloads hash-checked Harmony 2.2.1 from NuGet, so first build needs that network access. The game must be owned/installed and Steam available for playtesting. Image generation is a separate provider dependency, not required to run inspectors or an existing pack.

## Bootstrap and read-only inspection

Set the single game path below to the located directory. All later commands run from the chosen framework worktree.

```powershell
$gameRoot = 'E:/SteamLibrary/steamapps/common/TMNT' # Replace with the actual path.
python -m venv .venv
& ./.venv/Scripts/python.exe -m pip install -e .
if ($LASTEXITCODE -ne 0) { throw 'Dependency setup failed' }
New-Item -ItemType Directory -Force artifacts | Out-Null
& ./.venv/Scripts/python.exe -m tmnt_mod inspect-game $gameRoot
& ./.venv/Scripts/python.exe -m unittest discover -s tests -v
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
# Optional, only if the user supplied an archive:
# & ./.venv/Scripts/python.exe -m tmnt_mod inventory-assets 'E:/references/assets.zip'
```

Record reports locally. Archive inventory reads ZIP headers without opening or extracting members; it has no archive-size/member-count cap. Use selective, bounded export only when the relevant native format or art subset is known.

## Compatibility gate

The supported reference is game version `1.0.0.349`, observed Steam build `15664053`. Version labels alone are insufficient: build and exporter verify these SHA-256 values.

| File | SHA-256 |
| --- | --- |
| TMNT.exe | `36435CC7E1063F76E4641C92F95601414B62C1FAEDA39A64CCC270EEF6D82CC6` |
| ParisEngine.dll | `02FAE3072962C2304F9F086DB1680D8111D24CBF2F16A362C262809C62E4E7DE` |
| ParisSerializers.dll | `12A8F1B1288664D343EE8E8E68BB5EB593696D4CEB62D6ADD27AF5B72B81F554` |

`inspect-game` reports evidence; it is not the native compatibility gate. If the installed hashes differ, keep baseline Steam use available and inspect changed types, serializers, signatures, paths and scene identities before porting. Add regression tests and evidence for the new adapter. Do not merely replace expected hashes to force a build through.

## Build, launch, stop

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/build.ps1 -SourceGameDirectory $gameRoot
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
& ./local/playtest/Malcolm.Runtime.exe --self-test
if ($LASTEXITCODE -ne 0) { throw 'Runtime self-test failed' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Start -Baseline -Capture
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Status
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Stop
```

The executable retains the historical filename `Malcolm.Runtime.exe` for every character. Build defaults to `local/playtest` and `local/dependencies`; these are ignored and disposable. Optional build parameters `-PlaytestDirectory` and `-DependencyDirectory` select separate local directories. Pass the same stage with `-PlaytestDirectory` to every playtest command if overridden. Session records default to `artifacts`; keep `-ArtifactsDirectory` consistent when overridden.

Nonempty stages require the framework ownership marker. Let the builder create it for a fresh directory; never label an original Steam install as disposable. The scripts reject reparse paths and source/destination overlap. Stop the tracked process before rebuilding. Do not globally kill all game processes.

Choose exactly one primary mode: `-Baseline`, `-Encounter <json>`, or `-Residential <art-directory>`. Optional `-Character <manifest.json>` works alongside any primary mode. The current basename `manifest.json` is required by the C# loader. Example after creating/reviewing a new pack:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Start -Residential "$PWD/art/backgrounds/new-street" -Character "$PWD/art/characters/new-hero/manifest.json" -Capture
```

Use visible launch (`-WindowStyle Normal` in the script) for the requested playtest. Hidden process launch can hide the actual game window while sound continues. Keep a working display mode during verification; a prior live display-mode change crashed FNA backbuffer recreation. Do not automate security exclusions if a build is blocked.

Rollback is tracked Stop followed by normal Steam launch. Saves/deletes remain suppressed in the custom launcher, including baseline mode; this is a test environment, not a permanent progression install. Never package `local/playtest`, Steam assemblies or native content for another user. They rebuild from their own supported installation.
