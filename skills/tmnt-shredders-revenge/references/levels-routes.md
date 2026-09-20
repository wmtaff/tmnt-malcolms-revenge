# Address-to-address scenery and level construction

## Distinguish the available paths

**Implemented scenic prototype:** three PNGs named `home.png`, `street.png`, `park.png` render in fixed native Stage12 spans through `-Residential`. New original art can reuse this adapter. Its world spans are4096–4896,4896–5664,5664–6464; draw height456. It draws each panel once. The standalone repeat preview demonstrates tiling; neither it nor the runtime is a route reconstruction engine.

**Requested continuous route:** an ordered sequence of unique geographic segments with landmarks preserved as if walking between addresses. Acquisition, route manifest, adjacency review and runtime segment-loader work must be completed. [Issue #1](https://github.com/wmtaff/tmnt-malcolms-revenge/issues/1) tracks this extension. Do not squeeze a long route into the three filenames and describe it as fully implemented geographic traversal.

## Establish the route brief

Record start and end, direction, desired walking path, season/time, which side of the road the side-scroller depicts, recognizable landmarks, desired gameplay length, enemies and boss/objective. Request missing endpoints rather than using a previous family's address. Confirm ambiguous map matches before capturing the wrong location. Distinguish geographic distance from compressed gameplay distance.

Use `assets/route-plan.example.json` as a **planning format**, not a supported runtime input. Keep filled addresses, precise coordinates, personal notes and provider references under ignored `local/routes/<id>/`. Publish a redacted segment plan and original artwork only as appropriate for the user's task.

## Inspect Google Maps / Street View

Use an available browser or authorized official API. Resolve both endpoints and inspect the intended walking route, then examine successive views along that route. A nearest-panorama lookup can jump to a parallel road, so verify continuity and orientation manually. Record source location/date and direction for each segment; note coverage gaps and historical/seasonal mismatches. A winter view does not establish what foliage looks like in summer.

Treat Google imagery as provider-controlled material. Check current permitted use, attribution and storage rules before retaining imagery or sending it to another generation provider; access to a map is not permission for arbitrary image reuse. Use original user-supplied photographs or user-authored location descriptions when needed. Do not remove provider attribution, bulk scrape panoramas, or bypass unavailable coverage. Keep the generated game's original art separate from source imagery.

Official references, reviewed2026-09-20:

- [Street View metadata](https://developers.google.com/maps/documentation/streetview/metadata): location/date/panorama evidence, not a pedestrian route.
- [Street View policies](https://developers.google.com/maps/documentation/streetview/policies): current storage/attribution requirements.
- [Street View tiles](https://developers.google.com/maps/documentation/tile/streetview): official panorama access where that API is selected and authorized.

Provider access or missing reference coverage should be reported precisely. Continue with an explicitly labeled approximation only when consistent with the user's request; do not fabricate observed houses or intervening roads.

## Turn observations into distinct segments

1. Order segments from start to destination. Give each a unique ID, landmark description, source status (`observed`, `user-described`, `missing`) and private evidence reference. Use normalized route distance for planning if exact coordinates should not appear publicly.
2. Create an art-direction sheet: shared palette/outline, horizon, sidewalk/road baseline, lighting/season, side-scroller camera, world-to-image scale and safe gameplay lane. Street View perspective must be interpreted into side-on art; simply stitching perspective photos does not create a beat-'em-up level.
3. Assign landmarks once in the correct order. Separate nonrepeating buildings/parks from reusable texture motifs. Define left/right adjacency notes and overlap regions without repeating the same house at a seam.
4. Generate the first original segment; use it as style reference for neighbors, with their distinct observed content and explicit edge continuity. Preserve prompts, reference roles, returned dimensions, revisions and original hashes. Use image generation through the available provider workflow, not a nonexistent `generate-route` CLI.
5. Review consecutive joins at game scale and moving-camera scale. Check street/sidewalk continuity, horizon, seasonal consistency, duplicate landmarks and road direction. A low opposite-edge difference measures self-tiling, not adjacency between different route panels.
6. Export approved segment geometry and redacted art manifest. Missing segments remain explicit gaps until supplied or deliberately designed; do not silently fill them with a generic repeat.

## Existing three-panel validation and launch

Generate original `home.png`, `street.png`, `park.png` in a new art directory for a short scenic prototype. The background inspector accepts larger inputs than the renderer: for runtime use8-bit RGB/RGBA PNGs ≤16MiB, width≤4096,height≤2048,total≤4,194,304pixels. Review opacity; the current background renderer does not implement the character importer's chroma-key pipeline.

```powershell
& ./.venv/Scripts/python.exe -m tmnt_mod inspect-background art/backgrounds/new-street/street.png
& ./.venv/Scripts/python.exe -m tmnt_mod preview-background art/backgrounds/new-street/street.png artifacts/new-street-repeat-01.html
```

Inspect all three source images, not just the sample command's middle panel. Repeat preview needs an existing parent and fresh output filename. Seam thresholds are heuristics, not production acceptance. High-resolution shaded imagery may look sharp with point sampling while still lacking the game's deliberate low-resolution pixel clusters.

Build and launch using [setup](setup.md). `-Residential` selects the fixed native Baxter approach. It does not read a route plan or dynamically scale collision to new artwork.

## Implement the continuous-route extension when requested

Replace the hardcoded `BackgroundRuntime.Plan()` only through a reviewed loader contract: ordered relative image paths, unique IDs, explicit world spans, source crop, layer/depth, and adjacency. Validate finite bounds, monotonic coverage/no unintended overlaps or gaps, bounded bytes/pixels and local paths before replacing native rendering. Tests should cover out-of-order/duplicate segments, missing files, invalid geometry and rollback on failed preflight. A planning JSON is not launchable until this reader and mode/config wiring exist.

Fit the route within proven native traversable bounds for a first version, documenting compressed geography. Extending world length requires camera blocks, collision, traversal, spawns and level completion work; adding a wider background is insufficient. Implement streaming/culling if the art budget exceeds bounded resident textures. Validate route order in a scrolling preview and then the actual game.

Place Foot Soldiers and Baxter using inspected native contracts. Existing residential combat gating and boss victory remain incompletely verified. Check spawn activation, enemy-death thresholds, boss intro/banner/fight, defeat/retry, barrier release and final completion. Custom bosses or acid/sword behavior are separate gameplay extensions tracked in [issue #2](https://github.com/wmtaff/tmnt-malcolms-revenge/issues/2), not delivered by background generation.
