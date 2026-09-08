# Reusable workflow: add a playable character

This is the end-to-end workflow used for Malcolm. Start here, then use the narrower references for [manifest tooling](character-pipeline.md), [native asset export and rendering](player-assets.md), and [selection/runtime integration](player-runtime.md). The checked-in example is `art/characters/malcolm/manifest.json`; its sibling `generation.json` preserves the actual OpenAI prompts and generation revisions.

## 1. Establish the integration boundary

The first Malcolm implementation replaces the appearance and displayed selection name of the existing Leonardo slot. It retains Leo's internal identity, roster index, actor template, movement, attacks, voices, progression, and native animation metadata. It is not an additional roster slot or a newly authored axe combat system. The wooden axe is part of the generated artwork, animated over the donor's combat state machine.

Decide these independently for another character:

- Approved appearance, outfit, weapon, and intended sprite proportions.
- Donor actor/moveset and whether its gameplay silhouette fits that appearance.
- Display-name and portrait overrides versus a separate internal roster identity.
- Complete original poses versus explicitly documented shared pose families.
- Native VFX/audio to retain versus newly authored effects and sounds.

The current runtime adapter is explicitly scoped to Leonardo and Leo's UI collections. A new art pack for the same donor can reuse its rendering/manifest workflow. Another donor requires inspecting its own content paths, animation names, frame contract, UI collections, and adding an exact adapter with tests; changing only `character_id` does not do this. A separate roster entry additionally needs the preload, save/progression, and roster-index work described in [runtime research](custom-player-runtime-research.md).

Use an isolated feature worktree. Keep original game files, raw native exports, personal reference photographs, credentials, and playtest logs under ignored directories. Commit original generated art, prompts, reviewed manifests, source, tests, and original documentation.

## 2. Export the donor contract before generating animation

Use the bounded `tools/player-assets/PlayerAssetExporter.cs` workflow documented in [player assets](player-assets.md). It uses the pinned installed assemblies in the separately authorized local runtime workflow; it does not change the original Python inspectors' read-only guarantees. Keep its native JSON under ignored `artifacts/`.

The exporter preserves animation names and ordered frames, rectangles, offsets, durations, freeze/cycle flags, boxes, anchors, and event messages. For the inspected Leonardo version there are 150 named animations and 1,148 ordered frame references. Some are VFX animations, not body poses. Do not render a second Malcolm body for an effect animation.

Record the source and assembly hashes. A game update requires re-export and review rather than assuming the old map is compatible. Raw native metadata is a local integration input, not part of the distributable custom-art package.

## 3. Approve a compact character reference

Create one portrait and several consistent full-body views. Malcolm's approved reference uses swept brown hair, a light-green shirt, navy shorts, green-and-white sneakers, and a flat pale wooden double-headed axe with two cutouts, a top loop, and a shaped handle.

Keep the reference roles explicit in generation prompts: identity, outfit/weapon construction, palette/style, and camera direction. Use the approved concept as the next generation's reference instead of reinterpreting the initial photograph on each call. Version changes; do not overwrite accepted candidates. Malcolm's personal concept sheets are local under the earlier character worktree's ignored `local/character-concepts/` directory.

## 4. Generate small, reviewable pose banks

Use OpenAI's built-in image generation for each sheet. The current workflow needs no API key and does not contain an automatic generation API client. It consists of saved prompts, generation with explicit references, local source files, deterministic import, validation, and runtime integration.

Generate small pose banks rather than asking for every native animation in one large sheet. Malcolm uses four 4-column by 2-row banks plus a separate portrait:

| Sheet | Authored poses | Intended review |
| --- | --- | --- |
| `movement.png` | Two idle, four walk, two run | Foot anchoring, stride differences, weapon retention |
| `combat.png` | Windup, strike, follow-through, guard, overhead, slam, hurt, crouch | Weapon silhouette, anticipation/contact/recovery readability |
| `actions.png` | Jump, air attack, fall, land, knockdown, getup, grab, throw | Ground anchor versus airborne placement, complete limbs/axe |
| `specials.png` | Spin windup/front, taunt, victory, seated defeat, recovery, flying kick, ready | Clear state differences and pose reuse |
| `portrait.png` | One selection/HUD portrait | Likeness and readability at native UI scale |

Prompt for identical identity, proportions, outfit, weapon, direction, scale, cell margins, and background. Specify each cell in row-major order. Inspect the actual returned dimensions; asking for an evenly divisible grid does not guarantee one. In this run the wide sheets are 1774×887 and the portrait is 1254×1254.

Keep both the selected prompt and any corrective prompts in provenance. Two failures from this run are especially reusable:

1. Asking for transparent output produced an RGB image with a painted checkerboard. Check actual mode/alpha; a checkerboard appearance is not transparency. This candidate was rejected.
2. The first combat sheet let an axe cross cell boundaries. It was regenerated with smaller figures and more margin. Do not silently clip a weapon to make a grid pass.

Do not assume every requested pose is anatomically or temporally correct. The first two run frames, for example, still need native-scale stride review. A structurally accepted sheet is not a production animation-quality certification.

## 5. Import color and transparency deterministically

For these selected RGB sheets, the generation prompt uses a solid magenta background. The manifest declares `chroma_key: [255, 0, 255]` and `chroma_tolerance: 32` per sheet. Imported pixels whose maximum absolute RGB-channel difference from the key is within that tolerance become transparent. Remaining pixels are premultiplied by their alpha for the renderer.

The importer and browser preview perform this conversion in memory. Source PNG bytes stay unchanged. No resizing, palette quantization, repainting, or automatic seam repair is applied to the saved art. A key color should not occur in the actual character design; use a different declared key or genuine alpha if it does. Inspect edge halos and holes in equipment after import.

This full-color render path is deliberate: native player data normally uses an indexed shader and separate palette. Do not drop full-color pixels into a native indexed texture and assume they will render correctly. See [asset research](custom-player-assets-research.md) for the distinction.

## 6. Author explicit rectangles, pivots, and display scale

Each frame has a stable ID, source sheet ID, integer pixel rectangle, and a pivot measured relative to that rectangle. Rectangles may be authored manually or initialized with `grid_rectangles(width, height, columns, rows)`, which uses floor boundaries and covers the entire image even when cells differ by one pixel.

The pivot anchors the art to the native player position. Review the feet/body anchor for every pose; do not simply center every image. Grounded and airborne poses may need different anchors. These art pivots are separate from the retained native hitboxes, event locations, and attack timings.

Set a global `render_scale` and optional sheet override in source-pixels-to-world-pixels units. Malcolm movement uses 0.19; combat/actions/specials use 0.27 because the corrected sheets have smaller drawn figures. These are reviewed initial settings, not formulas for every character. UI portrait fitting uses native UI bounds rather than the gameplay scale.

Inspect rows for pivot jitter, pose shrink/growth, complete axe silhouettes, clipping, and left/right flips. Prefer a targeted generation revision or an explicit reviewed rectangle/pivot adjustment over hidden importer heuristics.

## 7. Map every native name explicitly

`animations` contains authored frame-ID sequences. Their `duration_ms` values drive the standalone art preview only. The runtime selects art using the current native animation/frame; it does not replace native timing, execute new attack events, or derive hitboxes from pixels.

`native_animation_map` maps exact native body-animation names to authored sequences. `native_passthrough` lists exact native effect names that retain original rendering. These sets must be disjoint, and their union must cover the donor collection. Malcolm's current map has 134 body names and 16 native effect names.

The initial body map shares 32 original poses across those 134 names. This prevents unnoticed missing-name fallback but does not mean 134 bespoke animations were authored. Review especially attacks, airborne movement, thrown states, and contextual cutscenes: semantic family reuse can preserve gameplay while still looking rough or mismatched. Document those limits and refine the affected mappings/poses rather than overstating coverage.

The runtime must not guess the mapping from a substring at draw time. Materialize, review, and version the mapping so each assignment is inspectable. Keep gameplay metadata unchanged unless the task explicitly includes designing a new moveset.

## 8. Validate and preview before launching

From the worktree, with the package installed in a Python environment:

```powershell
$env:PYTHONPATH = Join-Path $PWD 'src'
python -m tmnt_mod inspect-character art/characters/malcolm/manifest.json
python -m tmnt_mod preview-character art/characters/malcolm/manifest.json artifacts/malcolm-preview.html
python -m unittest discover -s tests -v
```

Create the ignored output directory first. Preview output is exclusive: choose a new filename for a new review rather than overwriting a prior result. The HTML embeds the original sheets, applies declared color keys transiently, and animates the selected sequence; it does not load native game files or send imagery over the network.

Review structural errors separately from warnings. Validate paths, input bounds, IDs/references, rectangles, pivots, finite scales/durations, alpha/key behavior, and exact donor name coverage. Compare the manifest's coverage union against the freshly exported contract. Inspect at world scale; enlarged attractive art can be unreadable in play.

## 9. Integrate selection and the render adapter

The source-built launcher accepts a character manifest independently of the primary baseline/encounter/residential mode. The identity adapter presents Malcolm when the native Leo slot is selected, while retaining the underlying native identity. The art adapter intercepts only the exact donor animation collection and named Leo UI collections.

The render adapter passes through native position, flip, angle, scale, tint, and depth, applies the reviewed custom rectangle/pivot and art scale, and restores renderer shader state after drawing. Native animation state, events, attack boxes, and progression remain outside this render-only substitution. Config validation and renderer failure handling must leave diagnostics rather than falsely claim integration success.

Verify the meaning of the native path property instead of assuming it is the file name. The first live attempt displayed the Malcolm name with Leo artwork because `AnimatedObject2dData.LoadTextures` sets `Path` to the asset's containing folder. The renderer initially matched `.../leonardo/leonardo`; the native value is the `.../leonardo` folder. A successful hook-install log without any custom-render logs exposed this mismatch. Exact folder matching, separator normalization, and a regression test are required for each donor adapter.

Selection, HUD, pause, world-map, and completion collections are separate dependencies. Verify each deliberately; one portrait replacement does not automatically prove all UI is correct. Replacing a complete UI frame may also replace native decorative framing, so visual inspection remains required.

## 10. Build, playtest, and capture evidence

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/build.ps1 -SourceGameDirectory 'D:/SteamLibrary/steamapps/common/TMNT'
& ./local/playtest/Malcolm.Runtime.exe --self-test
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Start -Residential "$PWD/art/backgrounds/residential" -Character "$PWD/art/characters/malcolm/manifest.json" -Capture
```

Use the isolated visible playtest, select Malcolm on the donor slot, and verify the portrait and label before starting a level. Check idle, walk, run, facing flips, jumps, attacks, damage, knockdown/getup, grabs/throws, specials, and victory/defeat. Correlate screenshots with character validation, coverage, render, and fallback logs. A compiled build or synthetic test is not proof of a rendered playable character.

Use the tracked controls to stop before rebuilding. They identify the owned process rather than killing every game process. For rollback:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/runtime/playtest.ps1 -Action Stop
```

Then launch the normal Steam game. The original installation is unchanged. Keep the first custom-character profile local; donor identity is preserved, but online visual/mod compatibility is not established by these tests.

## 11. Package and hand off another character

Create a sibling original-art directory, copy the reviewed manifest structure, assign a unique pack ID, replace source sheets and prompts, and review every frame/pivot/sequence. Keep the donor adapter unchanged only when its exact native contract still applies. Run the validation, preview, coverage comparison, tests, build, and live checklist again.

Include generation provenance, the donor/build contract, known shared poses/effects/audio, verified versus pending behaviors, exact commands, and rollback instructions. Never package Steam assemblies, native atlas data, raw exported metadata, private photographs, or credentials. Keep generated results versioned so another developer can trace a visible regression to a specific prompt, source hash, rectangle, mapping, or renderer change.
