# Custom playable character: native runtime research

## Scope and conclusion

Read-only research against owned Steam build 15664053, assembly version
1.0.0.349. TMNT.exe SHA-256:
`36435cc7e1063f76e4641c92f95601414b62c1faeda39a64ccc270eef6d82cc6`.
Evidence is reflection metadata, method-body inspection, and bounded inspection
of native content reader identifiers. Native dumps remain in ignored artifacts.
No game was launched, stopped, patched, or saved for this research.

An existing-slot visual replacement is the smaller first prototype: preserve the
slot's internal identity, native actor template, attack definitions, animation
names/events, palette selection, and progression. Replace compatible visuals and
selected display assets in the isolated profile. This keeps the original moveset;
it does not create a new combat system or independently selectable character.

A new roster slot is plausible because roster enumeration and selection are
dynamic. It is not verified as a supported extension mechanism. A new entry
requires coordinated identity, content, UI, preload, progression, and network
handling; adding one image or one CharacterInfo is insufficient.

## Verified roster and identity interfaces

| Native interface | Observed behavior |
| --- | --- |
| `Paris.Game.System.CharacterManager.Characters` | Mutable `List<Paris.Game.Data.CharacterInfo>`. |
| `CharacterManager..ctor()` | Resolves the content `Characters` directory, recursively loads entries, sorts by `ListPriority`. |
| `AddCharactersInFolder(string)` | Calls `DirectoryInfo.GetFiles()` and native `LoadContent<CharacterInfo>` for each file; skips `Disabled` records and recurses into subdirectories. It is not a JSON configuration loader. |
| `GetCharacterIndexByInternalName(string)` | Exact string comparison; returns `Int32.MaxValue` when absent. |
| `GetNextAvailableCharacterIndex(int, SaveGame, bool)` / `GetPreviousAvailableCharacterIndex(int, SaveGame, bool)` | Delegate to `GetAvailableCharacter(int, bool, SaveGame, bool)` using the roster count, save lock state, DLC availability, and game-mode rules. |
| `Paris.Game.Data.CharacterID.RefreshIndex()` | Resolves its string `ID` to a cached roster index. Changing roster order after references resolve is unsafe without cache invalidation. |
| `Paris.Game.System.GamePlayerInfo.SelectedCharacter` | `byte` roster index; `255` is the unselected sentinel. `SelectedPalette` is also a byte. |

No fixed character enum was found on this selection/spawn path. Byte identity
reserves 255, so indices 0..254 are representable; this is a storage bound, not a
claim that the UI or game supports 255 characters. `ListPriority` is a byte and
its comparer uses only that value; a future extension should avoid ties and
preserve existing ordering.

The owned content has eleven `Characters/*.zpbn` records and corresponding
`2d/Actor2d/Player` templates: April, Casey, Donatello, Karai, Leo, Michelangelo,
Mona, Mondo, Raphael, Splinter, and Usagi. Presence does not establish entitlement
or current selectability. Preserve native DLC checks.

`CharacterInfo` exposes `ActorTemplate`, `InternalName`, `AnimationProjectName`,
`Disabled`, `ShortName`, `FullName`, `ListPriority`, `UnlockCondition`, and
`DLCRequirement`. Presentation fields include `MenuColor`, `MenuStarAColor`,
`MenuStarBColor`, `StatRange`, `StatSpeed`, `StatThrow`, selection/interaction sound
IDs, special/radical-mode colors, `SuperJumpFXAnimation`, and five survival palette
indices. `CharacterInfo.Init()` supplies localization IDs `chrShort{InternalName}`
and `chr{InternalName}` when the explicit name IDs are empty, and initializes
voice/effect resources. These fields are metadata, not a complete moveset.

## Verified spawn and moveset path

`CharacterManager.Setup()` calls each CharacterInfo's `Init()` and registers its
ActorTemplate with `GameObjectPoolManager.AddPool(string, int, bool)`, using six
instances. `CharacterManagerPreload.Preload()` waits for `AttackList.Loaded`
before Setup. A new entry therefore needs to exist before these preload and pool
steps, or must reproduce their dependencies explicitly.

`GamePlayerInfo.GetActorTemplate()` returns
`Characters[SelectedCharacter].ActorTemplate` for a selected character.
`Paris.Engine.Scene.Scene2d.SpawnPlayer(PlayerInfo, bool, Vector3, bool)` obtains
that template, resolves spawn position, calls the native pool's `SpawnObject`,
and calls `SpawnIn` on the resulting actor. Preserve this lifecycle instead of
constructing a detached player object.

Player templates name character-specific readers: for example,
`ParisSerializer.LeonardoReadWriter` in `2d/Actor2d/Player/Leo.zpbn`, and equivalent
April, Casey, Donatello, Raphael, and other readers. Runtime classes such as
`Paris.Game.Actor.Leonardo` contain special behavior: its
`StateSuperFlyingKick(StateStep, float)` uses the native state machine, attack
stats, body displacement, and named animations including
`SpecialflyingkickStartup`. A custom visual can reuse that behavior; a new
behavioral archetype needs more than a new animation atlas.

`Paris.Game.Actor.Player.Init()` resolves named attacks through
`Paris.Game.System.AttackList.GetAttack(string)`, casting results to
`PlayerAttackStats`; examples include moving attack and aerial attack properties.
It also initializes a separate VFX animator from the player's AnimationSet.
`Player.LoadPlayerInfo(GamePlayerInfo)` applies `SelectedPalette` to
`Animator.CurrentAnimationTextureID` and applies progression unlocks.
`Player.CharacterInfo` resolves the selected roster index rather than discovering
identity from the actor's visible appearance.

Consequently, preserve animation event timing, frame geometry, attack names,
VFX layers, and palette semantics in the first reskin. Ordinary full-color PNG
replacement has not been proven equivalent to this indexed animation pipeline.
Exact sprite-sheet layout and animation-authoring requirements belong to the
separate asset investigation.

## UI: dynamic selection, named content dependencies

`Paris.Game.Menu.CharacterSelectionPanel.set_CurrentSelection(int)` wraps using
`Characters.Count`; negative input wraps to the last entry. `StateSelectCharacter`
uses the manager's next/previous available methods. There is no fixed nine-slot
bound in these inspected methods.

`Paris.Game.Menu.Control.CharacterSelectionContainer` allocates its panel array
using `EngineSettings.MaxPlayers`. Those are simultaneous player panels, not a
fixed-size roster. This distinction supports a data-driven roster extension but
does not verify every other screen.

`CharacterSelectionPanel.UpdateCharacterSelection()` constructs these paths from
the selected `InternalName` (shown as `{name}`):

- `2d/Animations/Menu/CharacterSelect/{name}/{name}`, with a start animation named
  `{name}`.
- `2d/Animations/Menu/WorldMap/PlayerPanels/{name}/{name}`.
- `2d/Animations/HUD/PlayerHUD/{name}/{name}`, including animation names
  `Panel{name.ToLower()}` and `IconLife{name.ToLower()}`.

`Paris.Game.Data.ParisPreloadedGlobalAssets.Assets` iterates the entire roster and
adds animation data plus corresponding `Texture` and `Palette` resources for
CharacterSelect, WorldMap/PlayerPanels, Pause/PowerLevel, LevelComplete, and
HUD/PlayerHUD. Missing new-name resources can therefore fail during startup,
before the new character is chosen. A prototype retaining an existing internal
name can retain these resources, or override chosen visuals deliberately.

## Save and network consequences

`Paris.Game.SaveGame.Heroes` is a `List<HeroInfo>`. `ResetHeroProgression(false)`
rebuilds it by enumerating Characters and sets initial lock states from each
UnlockCondition. It must not be used on an existing save merely to add a slot:
it clears existing progression. Existing GamePlayerInfo score/progression access
indexes Heroes directly by SelectedCharacter, so a late-added roster entry needs
an aligned in-memory HeroInfo before selection.

`SaveGame.WriteStoryProperties(BinaryWriter, bool)` writes each hero's
InternalName before its progression. The read path resolves that name through
`GetCharacterIndexByInternalName`; unknown entries are consumed into a temporary
HeroInfo rather than retained in Heroes. This suggests some name-based tolerance,
but removing a custom slot and later writing a stock save could discard its
progress. No save migration or round-trip compatibility was tested.

`GamePlayerInfo.WritePacket(BinaryWriter)` and `ReadPacket(BinaryReader)` exchange
SelectedCharacter and SelectedPalette as bytes. They do not exchange a custom
character name at those fields. Different roster order/content across peers can
resolve the same byte differently. Keep the first prototype local; online
compatibility requires an explicit matching roster/content contract and further
protocol research. No networking change is proposed here.

Roster-dependent achievements also exist: `CompleteCastAchievementInfo.Init()`
counts enabled, available characters; `NoMutagenAchievementInfo.Init()` counts
enabled characters. An additional slot can change these requirements. Existing
internal-name special cases exist too: `Player.LoadPlayerInfo` recognizes Casey
for an achievement. Reusing an internal identity preserves such native behavior.

## Suggested implementation boundaries and remaining unknowns

| First existing-slot prototype | Later additional-slot experiment |
| --- | --- |
| Keep original index/InternalName, actor class/template, attacks and native progression. | Register unique metadata before roster-dependent preload, pool setup and save initialization; preserve existing indices. |
| Replace compatible player visuals and selected presentation assets in the isolated profile. | Supply all name-derived UI, localization, animation, texture, palette and voice fallbacks; initialize matching HeroInfo without resetting others. |
| Verify idle, walking, attacks, damage, grabs, airborne states, KO/revive, palette effects, menu portraits and stage completion. | Verify selection wrapping, unavailable-character filtering, every roster-consuming screen, achievement semantics, save migration and matching peer identity. |

The exact safe texture/animation replacement hook is not selected by this
research. The donor slot and custom character's art/moveset are not chosen.
No new-roster runtime test, complete search for hardcoded character cases,
save round trip, asset preload trial, controller test, or network test was run.
The inspected dynamic menu code establishes plausibility, not compatibility.
