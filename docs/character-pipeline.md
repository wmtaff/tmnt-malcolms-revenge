# Reusable character artwork workflow

Generate a consistent set of named poses with OpenAI image generation, keep each original PNG, describe its frames explicitly in `manifest.json`, inspect alpha and geometry, then review the animation in a browser. The Python pipeline performs no generation calls, background painting, interpolation, automatic pose invention, or source-file rewriting.

Use actual PNG dimensions, not requested dimensions. For example, a generated 1774×887 sheet with four columns and two rows does not have equally sized integer cells. `grid_rectangles(1774, 887, 4, 2)` uses floor boundaries to allocate all pixels without resizing: the last cell is `[1330, 443, 444, 444]`. Confirm the generated poses actually respect those grid boundaries before assigning them; a regular grid alone does not prove correct artwork placement.

## Manifest format

All sheet paths are relative to the directory containing the manifest. Keep source images and generated review artifacts in ignored `local/` or `artifacts/` directories.

```json
{
  "schema_version": 1,
  "character_id": "malcolm",
  "render_scale": 0.2,
  "sheets": [
    {"id": "basic", "path": "basic.png", "columns": 4, "rows": 2}
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
  "native_animation_map": {}
}
```

Rectangles are integer `[x, y, width, height]` in source pixels. Pivots are explicit `[x, y]` relative to the frame's top-left corner; a pivot may lie on the frame boundary. Inspect the foot/contact position and choose pivots deliberately. No hitbox or pivot is inferred. IDs start with a letter and contain only letters, digits, `_`, or `-`, up to 64 characters. IDs must be unique within sheets, frames, or animations.

`render_scale` is an optional positive finite source-pixel-to-world-pixel scale, defaulting to 1. Choose it against visible alpha bounds and the desired in-game size, then verify in game; the example value is not a universal size. Browser preview zoom is independent of world scale.

`duration_ms` is strictly for art preview. Values must be positive finite numbers of at most 60,000 milliseconds. Runtime animation integration preserves native combat timing, events, and hitboxes. `native_animation_map` optionally maps exported native animation names to these animation IDs. The inspector validates mapping references but has no native animation inventory and therefore cannot prove complete coverage. Runtime may require a complete map. Do not silently fill missing motions with unrelated poses.

## Alpha and declared chroma keys

A sheet must contain transparency after its declared import rule. Prefer actual transparent PNGs. A painted checkerboard in an RGB PNG is opaque artwork; it is rejected without an explicit import rule.

For a deliberately generated flat key background, add sheet metadata such as:

```json
{"id":"basic","path":"basic.png","columns":4,"rows":2,
 "chroma_key":[255,0,255],"chroma_tolerance":32}
```

With a key, a pixel becomes transparent for analysis and browser rendering only when **every RGB channel** is within `chroma_tolerance` of the corresponding key channel. Other pixels retain their original alpha. Tolerance defaults to 32 and is bounded to 0–64. This is explicit import processing, not guessed background removal or fringe repair. Inspect edges and confirm that clothing or skin colors are not removed. The original PNG bytes remain unchanged.

## Inspect and preview

```python
import json
from tmnt_mod.characters import grid_rectangles, inspect_character_manifest, write_character_preview

print(grid_rectangles(1774, 887, 4, 2))
report = inspect_character_manifest('artifacts/malcolm/manifest.json')
print(json.dumps(report, indent=2))
write_character_preview('artifacts/malcolm/manifest.json', 'artifacts/malcolm/review-01.html')
```

The report includes source SHA256 hashes, dimensions, transparent/partial/keyed pixel counts, and frame-local visible bounding boxes with exclusive right/bottom coordinates. It identifies unmapped whole grid cells and warns about invisible frames. These are analytical findings; source rectangles are not cropped into new image files or repainted.

The HTML is self-contained and embeds original sheet bytes. It offers named animation selection and pause/play, with pivot-aligned frames on a checkerboard. Declared key processing occurs in a temporary browser canvas. Existing output files and manifest/output aliases are rejected; the output parent directory must already exist. Use a fresh HTML filename for every revision.

Review identity, silhouette, proportions, facing direction, foot stability, motion continuity, keyed edges, and whether each named action communicates its intent. A sparse pose sequence is an animation prototype, not evidence of full game animation coverage. The workflow currently samples original sheets; it does not pack or export a native game atlas.

## Bounds

The manifest is limited to 1 MiB, 8 sheets, 4096 mapped frames, 256 animations, and 16,384 total sequence entries. Each PNG must be a single frame, at most 16 MiB, no more than 4096 pixels per axis, and at most 4,000,000 pixels. Combined input is limited to 32 MiB and 16,000,000 pixels; mapped frame rectangles total at most 32,000,000 pixels for bounded alpha analysis. Invalid references, nonfinite geometry/timing, out-of-sheet rectangles, duplicate IDs/JSON keys, and paths escaping the manifest directory are rejected.
