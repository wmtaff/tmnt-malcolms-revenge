# Photo to playable character and animation

## Decide identity and gameplay separately

Use the supplied photo for likeness, then establish outfit, weapon construction, side-facing silhouette and proportions in an approved concept. Do not infer hidden clothing details as facts. Keep the photo local; a shareable package contains original generated art and prompts without the personal reference. A new character may reuse the tested Leo donor; a different donor or independent roster identity needs adapter implementation.

The existing `CharacterRuntime` uses manifest `display_name` for selection while retaining Leo's internal identity, actor type, native moveset, voice and progression. `character_id` labels the pack. A wooden axe drawn over Leo does not create an axe combat system. Explain this before presenting a donor appearance as a distinct new fighter.

## Export the actual donor contract

After the setup compatibility gate, from the framework worktree:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
& C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /nologo /reference:System.Web.Extensions.dll /out:artifacts/PlayerAssetExporter.exe tools/player-assets/PlayerAssetExporter.cs
if ($LASTEXITCODE -ne 0) { throw 'Exporter compilation failed' }
& ./artifacts/PlayerAssetExporter.exe $gameRoot ./artifacts/leo-contract-01.json
if ($LASTEXITCODE -ne 0) { throw 'Native export failed' }
```

Use a fresh JSON output; the exporter refuses overwrite. It executes the installed serializers with assembly hash guards and bounded decompression. Keep native metadata ignored. It preserves names, ordered frames, rectangles, offsets, durations, cycle/freeze flags, boxes, anchors and messages. This adapter expects 150 Leo animations and the inspected contract has 1,148 ordered frame references. Neither count implies that many distinct new drawings are required. Compare exact names, not count alone.

## Generate a stable reference, then small cycles

Use available OpenAI image generation or the user's chosen provider. This framework has no automatic generation command or bundled API credential. In Codex use its image-generation workflow when available; if it is missing, resolve access or accept user-supplied art rather than inventing outputs. The built-in workflow used here did not require an API key. Do not assume an API fallback has equivalent billing, model or permissions.

1. Create one consistent full-body right-facing master and one portrait. Make face, outfit, outline, palette, body proportions and weapon silhouette explicit. For a cutout weapon, describe its holes and flat material separately from its outer contour.
2. Test idle and a walk cycle first at the intended game scale. Generate coherent ordered motion, not unrelated attractive poses. Use the accepted master as the identity reference for every cycle.
3. Keep frame canvas, body scale, baseline and cell margins fixed. Require the whole weapon and limbs inside each cell. Inspect returned dimensions rather than assuming the requested grid divided evenly.
4. Generate attacks as anticipation/contact/recovery, then jump/fall/land, hurt/knockdown/getup, grabs/throws, specials, taunt and victory. Associate the contact pose with the donor's actual hit/event frame after reading native metadata.
5. Save original outputs unchanged, selected/corrective prompts, provider/model if known, reference roles, date, revision and source SHA-256. Keep rejected candidates locally for comparison.

Reusable prompt structure: describe the accepted identity and equipment; specify side-scroller camera/facing, target pixel scale and palette; enumerate frames in order; specify constant scale/baseline/margins and real transparency; prohibit cell overlap and labels. Use the checked-in `art/characters/malcolm/generation.json` as a worked provenance example, not a universal prompt.

## Alpha and import geometry

Prefer real alpha. A painted checkerboard in RGB is not transparency; inspect mode and pixel data. If using an intentionally solid key background, declare per-sheet `chroma_key: [255,0,255]` and `chroma_tolerance` (default32, range0–64). All three RGB channel differences must be within tolerance to key a pixel. Avoid a key present in clothing/weapon colors. The importer keys in memory and premultiplies remaining RGBA for rendering; it preserves original PNG bytes.

Use explicit integer rectangles `[x,y,width,height]` and frame-local pivots `[x,y]`. `grid_rectangles(width,height,columns,rows)` in `tmnt_mod.characters` uses floor boundaries, preserving remainder pixels. Pivots may be outside the crop within ±4096; review deliberately. Place grounded pivots at the intended contact point; airborne motion comes from native position and should not be accidentally canceled by a pose pivot.

Top-level `render_scale` defaults to1; sheet overrides are permitted from0.01 to4. Choose scale from the master/reference body size, then compare corresponding poses. Do not scale every crouch/jump to the same visible height: legitimate pose changes differ in height. Changing only sheet scale cannot fix inconsistent body proportions within a sheet.

## Manifest contract and integration

Create `art/characters/<pack-id>/manifest.json` and colocated original sheets. The complete existing Malcolm manifest is an example, not a template to rename without reviewing its frames. Required structure:

| Field | Purpose |
| --- | --- |
| `schema_version: 1`, `character_id`, `display_name` | Format, pack identity, UI label |
| `sheets[]` | Unique id, relative PNG path, columns/rows for preview, optional scale/key |
| `frames[]` | Unique id, sheet id, explicit rect and pivot |
| `animations[]` | Unique id, loop flag, ordered `{frame,duration_ms}` entries |
| `native_animation_map` | Exact native body name to authored animation id |
| `native_passthrough[]` | Exact native effects retained without drawing another custom body |

Provide a `portrait` animation. Map and passthrough must be disjoint and cover all150 actual donor names. The current loader requires the basename `manifest.json`. Preview timings are not runtime timings: native frame `i` of `N` samples `floor(i * artFrameCount / N)`. Adding more art frames than available native samples can skip them; more generated frames alone will not smooth the game. Changing the sampler to use time requires a tested renderer extension that still preserves native event/hitbox clocks.

Python preparation is deliberately stricter than some C# bounds. Keep packs within the intersection: up to8 sheets,4096 frames,256 art sequences,16,384 total sequence entries; each single-frame PNG ≤16MiB, ≤4096 per side and ≤4,000,000 pixels; total inputs ≤32MiB/16,000,000 pixels. Manifest ≤1MiB. IDs and numeric/path constraints are detailed in checkout `docs/character-pipeline.md`. Passing Python inspection may still leave runtime warnings such as missing portrait or coverage; resolve those before launch.

```powershell
& ./.venv/Scripts/python.exe -m tmnt_mod inspect-character art/characters/new-hero/manifest.json
& ./.venv/Scripts/python.exe -m tmnt_mod preview-character art/characters/new-hero/manifest.json artifacts/new-hero-review-01.html
```

Previews require an existing output parent and fresh filename. Inspect at shared body scale, not individually fitted frames. Compare exported native names against the union of map and passthrough. Keep mapping explicit; never guess state names at draw time. Restart the tracked playtest after pack changes. Verify portrait/name, HUD, pause, world-map and completion separately, because they are different native collections and portrait substitution can replace native decoration too.

## PixelLab handoff and jitter diagnosis

PixelLab is an optional animation provider, not integrated or benchmarked by this repository. Current official docs describe reference-image animation and skeleton guidance. Check the selected tool's current size/frame limits before preparing inputs: [skeleton guide](https://www.pixellab.ai/docs/tools/animate-with-skeleton), [text animation](https://www.pixellab.ai/docs/tools/animate-with-text-pro), [API](https://www.pixellab.ai/pixellab-api) (reviewed2026-09-20). These support a trial, not a fidelity guarantee.

Supply a clean transparent, side-facing master with its weapon, desired pixel dimensions and one motion brief. Run one walk-cycle comparison, retaining the OpenAI prototype as control. Review identity, complete weapon silhouette, foot contacts, loop seam and in-game sampling. Accept/export original PNG frames plus timing/order, then author explicit manifest rectangles/pivots/map. The existing importer is provider-independent; a PixelLab job URL is not a game-ready pack.

Diagnose observed jumpiness in this order:

1. Overlay neighboring frames around the same pivot; measure ground-contact drift and stable body features.
2. Check inconsistent body/weapon scale, facing and proportion changes, separately from intended squash/crouch.
3. Compare the actual native sample order and transition boundaries to the art cycle. Preview-only duration changes cannot fix runtime sampling.
4. Check whether attack contact and recovery were assigned to the correct native phases.
5. Regenerate or edit the affected cycle with shared pose/skeleton guidance; reimport and verify in motion. Do not claim the provider alone fixes the problem.

The initial Malcolm pack used32 body poses shared across134 body names plus16 native effects. The user confirmed he appeared but reported jumpiness. This is the starting quality baseline, not a smooth-animation certification.
