# Malcolm player profile

The first Malcolm implementation replaces the presentation of the existing
Leonardo slot. Select **Malcolm** in character selection where Leonardo normally
appears. It keeps Leonardo's native combat behavior, progression, roster index,
and internal identity. It is not an additional roster slot or a new moveset.

The identity module is implemented and its synthetic tests pass. An animated
player and selection portrait require the companion `CharacterArtRuntime` and
validated original art. The identity tests alone do not establish that the
artwork renders correctly in game. Live movement/combat and portrait verification
remain separate acceptance checks.

## Integration

Install both presentation modules only for the explicitly selected Malcolm
profile, after the launcher's normal isolated-copy and assembly checks and before
entering the game:

```csharp
CharacterRuntime.Install(harmony, gameAssembly, engineAssembly, artDirectory, CharacterArtRuntime.DisplayName, log);
CharacterArtRuntime.Install(harmony, engineAssembly);
```

Before either installation, the launcher calls
`CharacterArtRuntime.Configure(manifestPath, log)` for CPU art validation, before
creating diagnostic files or loading game assemblies. The companion renderer owns art loading, validation, original sprite rendering,
and portrait rendering. A renderer installation failure must abort this launch
before the game starts. Maintain the existing save-suppression hooks. The original
native assets remain available to profiles without these hooks.

Compile `tools/runtime/CharacterRuntime.cs` into the launcher. Compile
`CharacterRuntimeTests.cs` only as a separate test executable: it deliberately
declares native-shaped stand-ins and must never enter the launcher assembly.

## Build, launch, and rollback workflow

From the repository root in PowerShell, first build the isolated copy using the
existing pinned-build workflow. This compiles source and runs separate synthetic
residential, character identity, and character art suites. It does not launch the
game:

```powershell
./tools/runtime/build.ps1 -SourceGameDirectory 'D:/SteamLibrary/steamapps/common/TMNT'
```

Prepare the validated character directory containing `manifest.json` and the
sheets it references. Keep native extracted contracts in ignored artifacts;
distribute only original art and original configuration. See the companion asset
documentation for the explicit animation map, source rectangles, pivots, and
coverage rules. The launcher accepts exactly one primary mode and optionally one
character manifest:

```powershell
# Malcolm in the unchanged native stage flow:
./tools/runtime/playtest.ps1 -Action Start -Baseline -Character './art/characters/malcolm/manifest.json' -Capture

# Malcolm in the residential level profile:
./tools/runtime/playtest.ps1 -Action Start -Residential './art/backgrounds/residential' -Character './art/characters/malcolm/manifest.json' -Capture

# Malcolm with the configurable encounter profile:
./tools/runtime/playtest.ps1 -Action Start -Encounter './encounters/episode1-lobby.json' -Character './art/characters/malcolm/manifest.json' -Capture
```

These are alternative launches. Stop the tracked playtest before starting
another. `-PlaytestDirectory` and `-ArtifactsDirectory` can select a different
owned copy and evidence directory. With no explicit primary mode, the controls
retain their existing default encounter configuration. `-Baseline` describes the
stage flow; adding `-Character` deliberately changes its player presentation.

```powershell
./tools/runtime/playtest.ps1 -Action Status
./tools/runtime/playtest.ps1 -Action Stop
```

For rollback, omit `-Character` on the next isolated launch, or launch the normal
Steam installation. The controls only stop the tracked playtest process. Changing
the manifest requires restarting the playtest; no live reload is implemented.

Direct launcher syntax is equivalent, and requires a fresh log filename whose
parent directory already exists:

```powershell
./local/playtest/Malcolm.Runtime.exe './local/playtest' './artifacts/malcolm-first.log' --baseline --character './art/characters/malcolm/manifest.json'
```

The parser accepts the character pair before or after the primary mode. It
rejects duplicate primary modes, duplicate character flags, missing values,
unknown flags, and character-only invocations. The controls verify a plain-path
existing file named `manifest.json`; the renderer performs the deeper asset
validation. The tracked session JSON records the selected character manifest.

## Reusable build contracts and gotchas

- Runtime source uses the .NET Framework compiler and references Harmony 2.2.1,
  `System.Web.Extensions.dll`, and `System.Drawing.dll`. The asset sources are
  `tools/player-assets/CharacterArtConfig.cs` and
  `tools/runtime/CharacterArtRuntime.cs`; both must accompany the launcher.
- `CharacterRuntimeTests.cs` and `CharacterArtRuntimeTests.cs` are separate
  executable entry points, excluded from launcher compilation. The identity
  fixtures declare `Paris.*` stand-ins; including those in the launcher could
  interfere with native type discovery. CI builds and runs both suites without
  game assets, alongside the existing launcher and residential tests.
- Harmony 2.2.1 does not propagate replacements made through an `object[] __args`
  copy. Hooks that replace a native argument must use explicit reference
  injection, as the residential stage hook does. These character hooks only
  apply UI/logging postfixes and do not replace native arguments.
- Player art is not interchangeable with background art: the original player
  uses indexed palettes. The companion renderer owns the narrowly scoped
  full-color rendering path; do not disable palette/shader flags globally.
- A different custom player needs a compatible art manifest and a deliberately
  chosen donor contract. Renaming an asset directory or editing display text
  does not establish a new native roster identity or moveset. The manifest's
  validated `display_name` drives the selection label, so another compatible
  pack can present its own name while retaining the fixed Leo donor. Keep the exact
  donor selector, native events, combat metadata, and coverage limitations
  explicit when adapting this workflow.

## Exact native scope

The supported donor metadata was read from owned `Content/Characters/Leo.zpbn`:
`InternalName=Leo`, `ActorTemplate=Player\Leo`,
`AnimationProjectName=Leonardo`, `ListPriority=0`, `Disabled=false`.
The profile requires the first three identity values; other characters do not
match. `CharacterRuntime.IsMalcolmCharacterInfo(object)` exposes that selector.
`IsMalcolmPlayer(object)` additionally requires the exact native runtime type
`Paris.Game.Actor.Leonardo`.

`Paris.Game.Menu.CharacterSelectionPanel.UpdateCharacterSelection()` is patched
with a postfix. The original method performs its normal selection update and
clears the name override. For the donor only, the postfix sets the existing
`_characterName.OverrideString` to the validated manifest display name (`Malcolm`
for this pack). Switching to another character
therefore restores the native label path. The profile does not rename
CharacterInfo.InternalName or change localization databases, actor templates,
save indexes, network bytes, or roster ordering.

`Paris.Game.Actor.Player.LoadPlayerInfo(GamePlayerInfo)` has a logging postfix
only. Native player initialization, palette handling, progression and controller
behavior complete before the profile records a binding. Native voice lines and
other presentation resources remain unless the companion art module explicitly
replaces them; this profile does not supply a new voice pack.

Expected evidence:

- `MALCOLM_CHARACTER_READY`: identity/presentation hooks installed.
- `MALCOLM_SELECTION_PRESENTED`: a matching selection panel displayed Malcolm.
- `MALCOLM_PLAYER_BOUND`: native Leonardo player initialized under the profile.
- Companion renderer evidence plus a visible animated Malcolm and selection
  portrait are required to claim the full visual player works.

## Tests and practical limits

The separate tests use Harmony 2.2.1 against native-shaped classes. They exercise
the actual installation and postfixes, including Malcolm selection, switching
away and back, preservation of native identity, execution of native player
initialization, and rejection of wrong actor, template, internal name, or
animation project. No proprietary fixtures are distributed.

The renderer must preserve native animation events, attack/vulnerability boxes,
timing and gameplay state. Generated pose reuse or uncovered animation families
must be documented by its coverage report. Until live verification covers
movement, attacks, damage, air states, grabs, KO/revival, and selection portraits,
the deliverable remains an experimental visual profile.

A future independent slot requires the additional preload/UI, save progression,
and network identity work described in
[native runtime research](custom-player-runtime-research.md). Keeping the
existing slot avoids introducing those migrations into this first player proof.
