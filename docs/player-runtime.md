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
CharacterRuntime.Install(harmony, gameAssembly, engineAssembly, artDirectory, log);
CharacterArtRuntime.Install(harmony, engineAssembly, artDirectory, log);
```

The companion renderer owns art loading, validation, original sprite rendering,
and portrait rendering. A renderer installation failure must abort this launch
before the game starts. Maintain the existing save-suppression hooks. The original
native assets remain available to profiles without these hooks.

Compile `tools/runtime/CharacterRuntime.cs` into the launcher. Compile
`CharacterRuntimeTests.cs` only as a separate test executable: it deliberately
declares native-shaped stand-ins and must never enter the launcher assembly.

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
`_characterName.OverrideString` to `Malcolm`. Switching to another character
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
