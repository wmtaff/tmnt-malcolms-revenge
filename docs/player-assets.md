# Player artwork contract and rendering

This experiment replaces the existing Leonardo slot appearance. It preserves native identity, progression, animation clocks, movement, collision, messages, attack boxes and hurtboxes. It does not add an independent roster slot. Generated art is an appearance prototype; native visual effects explicitly remain native.

## Export the supported native contract

Run from the repository worktree using .NET Framework 4.x. The exporter is separate from the original read-only inspection modules and executes the authorized native serializers without launching the game.

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
& C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /nologo /reference:System.Web.Extensions.dll /out:artifacts\PlayerAssetExporter.exe tools\player-assets\PlayerAssetExporter.cs
& ./artifacts/PlayerAssetExporter.exe 'D:/SteamLibrary/steamapps/common/TMNT' './artifacts/leo-contract.json'
```

Choose a fresh output filename on every run. The tool verifies three supported assembly SHA-256 values before loading serializers, reads only Leonardo's fixed collection, limits decompression to 16 MiB, nesting to eight levels and frame references to 4096. It creates the output exclusively and never overwrites native assets. Keep the report in ignored artifacts: it contains proprietary native metadata.

The report records source and assembly hashes, palette/texture references, 150 independent named animation records, and 1148 ordered frame references. Animation/frame public serialized data includes durations, rectangles, positions, message types and values, attack boxes, vulnerability boxes and anchors. The 644 distinct native texture/rectangle pairs previously measured are not a requirement for 644 new drawings: frames and rectangles can be reused. Inspect each native animation's frame sequence and events when choosing an art sequence; do not infer combat timings from generated pictures.

## Original art manifest

Use a self-contained directory containing manifest.json and original PNG sheets. Schema version 1 requires character_id, display_name, sheets, frames, animations and native_animation_map. This adapter targets the supported Leo collection regardless of descriptive donor metadata. The optional native_passthrough list and mapping must be disjoint, unique, and total exactly 150 names; live collection validation confirms every native name is represented. Map body animations to semantic art sequences. Put effects such as attack sparks in passthrough so they retain their native drawing and shader.

Each sheet has id, relative path, optional render_scale (0.01 through 4), optional chroma_key [r,g,b], and chroma_tolerance (default 32, maximum 64). Sheet scale overrides top-level render_scale, whose default is 1. Grid columns/rows are preview metadata; runtime always reads each frame's explicit integer rect [x,y,width,height]. Non-divisible sheets therefore retain their last row/column remainder. A frame also has a pivot [x,y] in source pixels, bounded to +/-4096; external pivots are allowed. Animation frames refer to frame IDs. Preview duration_ms and loop do not replace native clocks.

For a new appearance, produce original pose sheets, identify transparent margins and foot pivots, create explicit crop rectangles, group poses into semantic sequences, and map every native body animation deliberately. Supply an animation named portrait. Native frame i of N samples floor(i * artFrameCount / N), clamped to valid indices. This preserves events and timing but does not guarantee the illustrated contact pose aligns perfectly with every native hit frame. Review attacks, throws, airborne movement and special actions in gameplay.

The loader bounds manifest bytes to 1 MiB, sheets to 16, each PNG to 16 MiB and 4096 pixels per side/4M pixels, cumulative pixels to 32M, and frames to 4096. It rejects escaping or reparse-point asset paths, invalid numbers, duplicate IDs, missing references and opaque sheets without a usable key. PNGs are decoded in memory; original files remain unchanged. A key match means the maximum absolute RGB channel difference is at most tolerance. Matching pixels become zero RGBA; other pixels preserve alpha and premultiply RGB by alpha/255 for native AlphaBlend.

## Rendering integration

Compile CharacterArtConfig.cs with System.Drawing.dll and System.Web.Extensions.dll. Compile CharacterArtRuntime.cs alongside it and the existing pinned Harmony reference. Launcher integration calls Configure(manifestPath, log) before game assemblies, reads DisplayName, then Install(harmony, engine). Call Dispose in launcher cleanup; SelfTest verifies chroma/premultiplication and native-frame sampling without loading the game.

The Harmony prefix targets the verified 12-argument AnimatedObject2dData.Render method. It matches the exact Leonardo player collection and the five known Leo UI portrait collection paths. It uploads premultiplied RGBA through Texture2D.SetData<byte>, selects SimpleShader with a renderer Push, and restores prior renderer state with Pop. It avoids the native indexed palette shader for custom artwork; palette-index channel encoding and indexed atlas conversion remain unimplemented.

The nine-argument DrawTexture receives the native position, tint, priority and rotation, the custom crop and pivot, and native scale multiplied by sheet scale. Horizontal flip mirrors the origin and rotation; vertical flip mirrors its origin. UI artwork instead fits within the native frame rectangle and derives its origin from native frame position, preserving layout without applying gameplay scale. Palette-square UI widgets remain native. Texture resources are reused per device and released on device changes or disposal.

CHARACTER_ART_VALIDATED confirms CPU validation; CHARACTER_COVERAGE confirms live native name coverage; CHARACTER_ART_RENDERED confirms the draw hook ran. These are separate from visual gameplay acceptance. CHARACTER_ART_FALLBACK logs reflection/upload/draw failures and resumes native appearance for the session. A renderer-state restoration failure propagates rather than silently rendering with corrupt state. Check asset paths, image transparency, supported assembly version and exact method signatures before changing guards. No game-ready claim follows solely from manifest or compile success.

## Verification evidence

The supplied five-sheet candidate passed CPU loading with 33 frames, 134 mapped animations and 16 native passthrough effects. Its scales were movement 0.19, combat/actions/specials 0.27, portrait 1. The exporter produced 150 animations/1148 frame references. The renderer compiled against .NET Framework and pinned Harmony. GPU drawing and pose quality require the separately authorized local playtest; this component task does not launch the game.

Native selector correction: AnimatedObject2dData.Path is a texture folder, not the collection asset filename. Native Load calls LoadTextures(GetFolderSafe(assetPath)); LoadTextures assigns that folder to Path. The renderer therefore normalizes slash direction, trailing slash and case, and matches exact folders such as 2d/animations/players/leonardo and 2d/animations/menu/characterselect/leo. A bounded first-24-folder diagnostic reports CHARACTER_ART_OBSERVED; a Path getter failure reports CHARACTER_ART_PATH_ERROR once. This distinguishes a selector mismatch from a draw/upload failure.
