# Custom playable character: verified local asset contract

Research date: 2026-09-08. Scope: installed Steam build, read-only assembly metadata,
bounded native serializer execution, and selected ZIP headers/text. No game was
launched, no installation files changed, and no game assets or proprietary dumps
are included here. Diagnostics remain in ignored artifacts. This is a planning
baseline, not a verified custom-character importer.

## Representative player: Leonardo

The installed files form several distinct layers:

| Layer | Installed content identifier | Evidence |
| --- | --- | --- |
| Character identity | `Characters/Leo` | `CharacterInfoReadWriter`; references `Player/Leo`, Leonardo, localized `chrLeo`/`chrShortLeo`, `FxLeo`, and many Leo voice cues |
| Gameplay actor template | `2d/Actor2d/Player/Leo` | `LeonardoReadWriter`; animation collection below, shared player actions and Leo-specific attack definitions |
| Animation collection | `2d/Animations/Players/Leonardo/Leonardo` | Fully deserialized with installed `AnimatedObject2dDataReadWriter` without loading textures |
| Atlas | same directory, `LeonardoTexture.zxnb` | Raw-deflate wrapper around ordinary XNB Texture2D, Color format 0, 3025 by 1735, one mip |
| Shader palette | same directory, `LeonardoPalette.zxnb` | XNB Texture2D Color format 0, 256 by 12, one mip |

The actor template includes references such as `LeoHeavySwingAttack`,
`LeoStationaryAttackFinisher`, `LeoMovingAttackFinisher`, `LeoSpecialAttack`, and
`LeoSuperFlyingKick`, alongside shared movement, damage, throw, and attack actions.
These are evidence that gameplay behavior extends beyond image frames. Their full
implementation and the roster-registration contract were not investigated here.

## Actual animation workload and metadata

The current native Leonardo collection contains:

| Measurement | Count | Meaning |
| --- | ---: | --- |
| Named animation entries | 150 | Entries in the native Animations list, including attacks, movement, reactions, special interactions, and transitions |
| Ordered frame references | 1148 | Sum of every animation's Frames list; includes reuse |
| Distinct `(TextureID, Rect)` tuples | 644 | Distinct atlas rectangle references, **not** a proven count of unique images or required new drawings |
| Attack-box records | 126 | Summed across all frame references |
| Vulnerability-box records | 519 | Summed across all frame references |
| Anchor-point records | 57 | Summed across all frame references |
| Animation-message records | 103 | Summed across all frame references |

These counts describe this installed Leonardo asset. They are not a universal
minimum for a new character, and the 150 entries are not 150 independent drawings.
Some entries share atlas rectangles; distinct rectangles might also contain
identical or related pixels. No image-equality analysis was performed.

The native `AnimatedObject2dData.Frame` exposes `TextureID`, `Rect`, `Pos`,
`Duration`, `Freezable`, `AttackBoxes`, `VulnerabilityBoxes`, `AnchorPoints`, and
`Messages`. Hit boxes carry a Name and Rectangle; anchors carry a Name and Vector2.
Each animation carries Name, Frames, CycleOnLastFrame, and FreezeOnAllowedFrames.
Preserve the flags according to native semantics rather than treating their names
as an interchangeable GIF-loop setting.

Concrete native examples, using zero-based frame indexes:

- Idle: 8 frames. Frame 0 uses atlas rectangle `(2458,840,61,77)`, Pos `(-35,-75)`,
  Duration `0.1`, and vulnerability box `(-19,-63,34,62)`. Idle durations include
  both `0.1` and `0.14`; they are not uniform.
- Walk: 6 frames, each Duration `0.1`, with changing crop rectangles and offsets.
- Attack: 6 frames with durations `0.03, 0.03, 0.01, 0.03, 0.06, 0.03`.
  Frame 2 carries the `EndOfAntic` message. Frame 3 carries an attack box
  `(0,-62,76,56)` and `Leo_Attack_01` sound cue. Frame 5 carries `StartOfRecovery`.

The draw implementation obtains the source rectangle from Frame.Rect and the
sprite origin from **negative Frame.Pos**, adjusting it when flipped. A copied
256-by-256 export canvas is therefore not the native cropped-frame/pivot contract.
Frame messages and gameplay rectangles cannot be reconstructed reliably from
pixel appearance. Replacing timing or pose extents without preserving or reviewing
these fields can desynchronize anticipation, damage, recovery, and sounds.

## Indexed rendering versus an exported PNG

The native collection reports BaseTextureCount=1, TextureFilenames containing
`LeonardoTexture`, ShaderPalette=`LeonardoPalette`, and
IsShaderPaletteEnabled=true. `LoadTextures` loads both atlas and palette, creates
IndexedShaderData, and assigns its PaletteTexture. `StartShaderEffect(paletteId)`
sets PaletteID and pushes the engine IndexedShader before rendering.

A headless data-only read reports HasIndexedShader=false because its backing
shader-data object is created by LoadTextures. This does **not** mean the shipped
player uses ordinary full-color rendering. Unlike the static ground replacement,
swapping an arbitrary full-color PNG into this atlas path is not yet a valid
import strategy.

The XNB atlas uses a Color storage format, but that alone does not establish the
semantic meaning of its channels. The precise palette-index channel encoding and
alpha handling were not verified from the compiled shader. A small indexed
texture/palette round-trip must prove those details before an exporter is called
game-ready. The palette dimensions establish 12 stored rows, not proof that every
row is an independently selectable unlocked skin in every mode.

## What the supplied visual-assets ZIP provides

ZIP inventory found 9769 nonempty files under `characters/Leonardo`, including
12 `.pal` files, a Tag.txt, 756 files in each of most palette variants, 887 files
under `01 [Default]`, and 553 under `[Pre-DLC]`. These inventory counts include
different exports and versions and must not be substituted for native animation
counts or used to infer frame timing.

A bounded read of `01 [Default]/Idle_00.png` found a **256-by-256, 8-bit indexed
PNG (color type 3)**. It is not RGBA on disk. It can be decoded into RGBA for art
work, but its PNG palette/canvas remains distinct from the engine's packed atlas,
frame metadata, and separate shader palette texture.

The archive's Tag.txt describes a default export reference point `(128,224)` and
repeated frames encoded in filenames; larger canvases have adjusted reference
points. That is an export convention, not verified engine metadata. The native
installed data supplies the authoritative frame ordering, crop, offset, messages,
and combat boxes. Only that small text member and one PNG header were opened;
the archive was not extracted wholesale. Source assets have their original rights
holders; the project should version original custom art and importer code.

## UI and identity assets are separate

Native data-only deserialization also confirmed:

| Collection under `2d/Animations` | Animations | Frame references |
| --- | ---: | ---: |
| `HUD/PlayerHUD/Leo/Leo` | 2 | 2 |
| `Menu/CharacterSelect/Leo/Leo` | 3 | 12 |
| `Menu/LevelComplete/Leo/Leo` | 1 | 1 |
| `Menu/Pause/PowerLevel/Leo/Leo` | 1 | 1 |
| `Menu/WorldMap/PlayerPanels/Leo/Leo` | 1 | 1 |

Each of these collections references its own LeoTexture and LeoPalette in that
directory. The installation also has a separate Leonardo story-ending animation
collection and Leo-specific ending cutscene. This list is representative, not a
proven exhaustive dependency closure. Voice cues, localization, effects, character
selection identity, and ending presentation need explicit decisions for a complete
custom character.

## Practical implementation choices

**Replacement appearance in an existing player slot:** retain Leonardo's gameplay
class, action definitions, named animations, ordered frame references, timing,
messages, boxes, and anchors. Create matching original art and either preserve
the atlas layout or write a validated metadata-aware repacker. Prove the native
palette conversion, or deliberately build and test a separate full-color render
path. Replace the relevant UI art if the character should be presented as a new
identity. This is the smallest useful playable-character milestone, but it still
inherits Leonardo's behavior and weapon-dependent poses.

**Independent playable character:** additionally establish character catalog and
selection registration, actor template/class behavior, action and state-machine
bindings, combat parameters, sound/voice/effect dependencies, and save/network
identity compatibility. A new roster slot and unique moves are separate engineering
work; they do not follow automatically from a sprite sheet.

The next bounded proof should replace one existing-slot idle and one attack while
preserving their native metadata, verify the palette/origin round-trip in game,
and exercise hit timing and a HUD portrait. Then expand to the required animation
families with explicit coverage checks. The full 150-entry asset offers a concrete
coverage checklist, not a mandate to generate every frame independently.
