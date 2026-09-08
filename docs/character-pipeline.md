# Reusable character artwork workflow

Generate a consistent set of named poses with OpenAI image generation, keep each original PNG, describe its frames explicitly in `manifest.json`, inspect alpha and geometry, then review the animation in a browser. The Python pipeline performs no generation calls, background painting, interpolation, automatic pose invention, or source-file rewriting.

Use actual PNG dimensions, not requested dimensions. For example, a generated 1774×887 sheet with four columns and two rows does not have equally sized integer cells. `grid_rectangles(1774, 887, 4, 2)` uses floor boundaries to allocate all pixels without resizing: the last cell is `[1330, 443, 444, 444]`. Confirm the generated poses actually respect those grid boundaries before assigning them; a regular grid alone does not prove correct artwork placement.

## Manifest format

All sheet paths are relative to the directory containing the manifest. Original generated artwork and its prompt/manifest may be versioned under `art/characters/<character-id>/`. Keep native extracted game art and generated diagnostic reports in ignored `local/` or `artifacts/` directories.

```json
{
  "schema_version": 1,
  "character_id": "malcolm",
  "display_name": "Malcolm",
  "render_scale": 0.2,
  "sheets": [
    {"id": "basic", "path": "basic.png", "columns": 4, "rows": 2,
     "render_scale": 0.19, "chroma_key": [255, 0, 255], "chroma_tolerance": 32}
  ],
  "frames": [
    {"id": "idle_a", "sheet": "basic", "rect": [0, 0, 443, 443], "pivot": [221.5, 430]},
    {"id": "idle_b", "sheet": "basic", "rect": [443, 0, 444, 443], "pivot": [222, 430]}
  ],
  "animations": [
    {"id": "idle", "loop": true, "frames": [
      {"frame": "idle_a", "duration_ms": 120},
      {"frame": "idle_b", "duration_ms": 120}
    ]}
  ],
  "native_animation_map": {},
  "native_passthrough": []
}
```

Rectangles are integer `[x, y, width, height]` in source pixels, with the source image's top-left at `(0,0)`. Pivots are explicit `[x, y]` relative to the rectangle's top-left corner, each finite and within −4096..4096. A pivot may lie outside the rectangle; this supports deliberate native-style origins but can place art off-screen if misconfigured. Inspect the foot/contact position and choose pivots deliberately. No hitbox or pivot is inferred. IDs start with a letter and contain only letters, digits, `_`, or `-`, up to 64 characters. IDs must be unique within sheets, frames, or animations.

`display_name` is required nonempty text of at most 256 characters without control characters. The selection UI uses it as a presentation label while keeping the native donor identity unchanged. `character_id` is the stable machine identifier and does not itself create a new roster slot.

`render_scale` is an optional finite source-pixel-to-world-pixel scale within 0.01..4, defaulting to 1. A sheet's `render_scale` overrides the global value, allowing pose sheets with different drawn character heights to share a consistent in-game size. Choose it against visible alpha bounds and the desired in-game size, then verify in game; the example value is not universal. Body animations share one fixed browser camera and zoom so sheet-scale differences remain visible. The `portrait` animation is fitted separately because runtime portraits fit native UI regions rather than using body world scale.

`duration_ms` is strictly for art preview. Values must be positive finite numbers of at most 60,000 milliseconds. Runtime animation integration preserves native combat timing, events, and hitboxes. `native_animation_map` maps exported native animation names to these animation IDs. `native_passthrough` lists exact native animation names whose original rendering must remain, such as effects, flame effects, and light motes. Its names must be unique, nonempty, at most 256 characters, and disjoint from the map; it is limited to 150 entries. Preserving native effects prevents a second Malcolm body from appearing where a native effect should render.

The small sample above is a preparation template, not a launch-ready pack. The current runtime additionally requires a `portrait` animation and exactly 150 names across the native map and passthrough list. The inspector warns when these requirements are missing but has no native animation inventory and cannot prove that every name matches the installed collection. The runtime checks the actual collection before substituting it. Mapping 150 names to a small pose bank does not imply 150 bespoke animations; document shared poses and incomplete motion quality honestly.

## Alpha and declared chroma keys

A sheet must contain transparency after its declared import rule. Prefer actual transparent PNGs. A painted checkerboard in an RGB PNG is opaque artwork; it is rejected without an explicit import rule.

For a deliberately generated flat key background, add sheet metadata such as:

```json
{"id":"basic","path":"basic.png","columns":4,"rows":2,
 "chroma_key":[255,0,255],"chroma_tolerance":32}
```

With a key, a pixel becomes transparent for analysis and browser rendering only when **every RGB channel** is within `chroma_tolerance` of the corresponding key channel. Other pixels retain their original alpha. Tolerance defaults to 32 and is bounded to 0–64. This is explicit import processing, not guessed background removal or fringe repair. Inspect edges and confirm that clothing or skin colors are not removed. The original PNG bytes remain unchanged.

## Inspect and preview

From an installed project environment, use the CLI:

```powershell
python -m tmnt_mod inspect-character art/characters/malcolm/manifest.json
python -m tmnt_mod preview-character art/characters/malcolm/manifest.json artifacts/malcolm-review-01.html
```

Both commands print JSON. A valid inspection or written preview returns exit status 0. Invalid JSON, references, geometry, alpha/key rules, missing files, or existing output paths return status 2 with a JSON `error`; they do not rewrite source sheets. Structural warnings still return 0. `preview-character` expects an existing output parent directory and a fresh `.html` filename. The Python API supports scripted preparation:

```python
import json
from tmnt_mod.characters import grid_rectangles, inspect_character_manifest, write_character_preview

print(grid_rectangles(1774, 887, 4, 2))
report = inspect_character_manifest('artifacts/malcolm/manifest.json')
print(json.dumps(report, indent=2))
write_character_preview('artifacts/malcolm/manifest.json', 'artifacts/malcolm/review-01.html')
```

The report includes source SHA256 hashes, dimensions, transparent/partial/keyed pixel counts, effective per-sheet scales, and frame-local visible bounding boxes with exclusive right/bottom coordinates. `visible_size_world` is the visible width/height multiplied by the effective sheet scale. It identifies unmapped whole grid cells and warns about invisible frames. An intentional portrait crop can trigger an unmapped-cell warning without being structurally invalid. These are analytical findings; source rectangles are not cropped into new image files or repainted.

The HTML is self-contained and embeds original sheet bytes. It offers named animation selection and pause/play, with pivot-aligned frames on a checkerboard. Declared key processing occurs in a temporary browser canvas. Existing output files and manifest/output aliases are rejected; the output parent directory must already exist. Use a fresh HTML filename for every revision.

Review identity, silhouette, proportions, facing direction, foot stability, motion continuity, keyed edges, and whether each named action communicates its intent. A sparse pose sequence is an animation prototype, not evidence of full game animation coverage. The workflow currently samples original sheets; it does not pack or export a native game atlas.

## Bounds

The manifest is limited to 1 MiB, 8 sheets, 4096 mapped frames, 256 animations, and 16,384 total sequence entries. Each PNG must be a single frame, at most 16 MiB, no more than 4096 pixels per axis, and at most 4,000,000 pixels. Combined input is limited to 32 MiB and 16,000,000 pixels; mapped frame rectangles total at most 32,000,000 pixels for bounded alpha analysis. Invalid references, nonfinite geometry/timing, out-of-sheet rectangles, duplicate IDs/JSON keys, and paths escaping the manifest directory are rejected.

## Common failures and review steps

| Finding | Next step |
| --- | --- |
| Opaque sheet / no matching key | Inspect the actual PNG mode. Regenerate with real alpha or a deliberate flat key; never treat a painted checkerboard as transparency. |
| Key removes clothing or leaves magenta fringes | Compare key/tolerance with the source pixels. Adjust only the declared import rule or regenerate; the tool does not paint repairs. |
| Missing sheet or path outside the manifest folder | Keep assets with the manifest and use relative paths. Runtime also refuses reparse paths; use ordinary files for launch packs. |
| Out-of-bounds or empty frame | Recalculate the explicit grid rectangles from actual dimensions, then visually inspect pose alignment and margins. |
| Feet jump or body sizes vary | Check explicit pivots and effective per-sheet scales against alpha bounds at a shared preview zoom. Preserve action-specific poses rather than forcing every silhouette to the same size. |
| Tiny body preview beside a portrait | Portraits are fitted independently; compare body animations with each other rather than assigning a UI portrait's world scale to the body. |
| Existing preview output | Use a fresh revision filename. No overwrite mode is provided. |
| Native coverage failure | Compare exact names from the native export with map-plus-passthrough membership. Keep native effects in passthrough and provide `portrait` explicitly. |
| Runtime falls back to native art | Inspect `CHARACTER_ART_FALLBACK`/renderer-state diagnostics; a successful static inspection does not prove renderer compatibility. |

For every new character, preserve the reference and prompt, original sheets, explicit manifest, inspection report, fresh preview, and runtime verification notes. Record native donor identity separately from the displayed character name. Verify selection UI, idle/movement, attacks, airborne/hurt states, native effects, and resource/state cleanup in the isolated playtest before claiming playable-character coverage. No gameplay launch is performed by the inspection or preview commands.
