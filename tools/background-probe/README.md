# Background render contract probe

This separate diagnostic invokes installed serializers; it does not launch the
game, create a graphics device, or write Steam files. Compile with the Framework
64-bit `csc.exe` and keep executables/reports under ignored `artifacts/`.

```powershell
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:artifacts\BackgroundProbe.exe tools\background-probe\BackgroundProbe.cs
& .\artifacts\BackgroundProbe.exe --self-test
& .\artifacts\BackgroundProbe.exe 'D:/SteamLibrary/steamapps/common/TMNT' 'D:/SteamLibrary/steamapps/common/TMNT/Content/2d/Level/Scene2d/Stage/Stage_12/Level_12_art.zpbn' > artifacts/background-contract.txt
```

It locates bounded length-prefixed TextureGameObject reader candidates and invokes
the game's native reader. The read deliberately stops at TexturePath's setter
when that setter tries to load graphics content through the inert context. Only
fields serialized before that point are reported. This is partial native parsing,
not a complete scene deserialization or supported content import API.

Verified local metadata and partial native records:

- Stage 12 `BG_Ground02`, ID `0b1d668c-f0fa-4ec2-a90f-dbe122c9c83d`, starts
  at world `(4096,0,0)`. Crop/origin/parallax are zero. Priority is `BG/Main Ground`.
- Texture path is `2d\Level\Tileset\Level12_Ground02`. Its `.zxnb` raw-deflates
  to an XNB Texture2D with Color format 0, dimensions 2368 by 456, one mip.
- `TextureGameObject.Render()` submits the ordinary ten-argument
  `Renderer.DrawTexture(Texture2D, Vector2 cropPos, Vector2 cropSize,
  Vector2 position, Vector2 drawSize, float rotation, Vector2 origin,
  SpriteEffects, Color, float priority)` overload. Crop and destination bounds
  become integer rectangles; destination positions are rounded.
- Renderer default Begin uses SimpleShader, AlphaBlend and PointClamp. This draw
  path accepts full-color textures; it does not require an indexed sprite atlas.
- The last static foreground texture `BG_OL08` starts at `(5296,168,0)`, is
  536 by 288, and has horizontal parallax 0.1. It ends before the Baxter arena.
  Animated boss machinery is outside this static texture probe's scope.

`tools/runtime/BackgroundRuntime.cs` provides an optional render-thread replacement
for that exact scene/object/path/position. Parent integration should call
`BackgroundRuntime.Configure(installedEngineAssembly, imageDirectory, Log)`, then install
a `TextureGameObject.Render` Harmony prefix returning
`BackgroundRuntime.TryRender(__instance)`. Call `Dispose()` at shutdown on the
render thread. It needs no additional compile references. `SelfTest()` exercises
tile layout, PNG dimension bounds, and fallback on reflection mismatch without
game assemblies or Harmony.

The caller supplies a directory containing `home.png`, `street.png`, and
`park.png`; no location, reference address, or draft art is embedded in source.
Each file accepts bounded 8-bit RGB/RGBA PNG input, at most 16 MiB and 4,194,304
pixels (three files maximum). All headers validate before configuration changes,
and all three GPU uploads complete before the first replacement draw submission.
Each texture loads once per graphics device and is released on reset/disposal.

The complete source image fills its corresponding world rectangle:

| Image | World X | Width | World Y | Height |
| --- | --- | --- | --- | --- |
| home.png | 4096..4896 | 800 | 0..456 | 456 |
| street.png | 4896..5664 | 768 | 0..456 | 456 |
| park.png | 5664..6464 | 800 | 0..456 | 456 |

The segments meet exactly and cover the native ground's original 2368 by 456
extent. The parent can build a repeating pattern inside `street.png`; this
runtime draws that middle image once. Native Baxter X=6326 lies inside the park
segment. Supply matching opaque edges and align the curb near image Y=70% to put
it around world Y=319, above the current actor foot positions near Y=360.
Nearest sampling is inherited from the native renderer; source dimensions may
differ but are scaled to these fixed rectangles. This visual replacement does
not establish the underlying route's collision traversability.

Camera, collision, enemy behavior, native animation objects, and boss machinery
remain controlled by the scene. A failed optional replacement preserves native
rendering and logs `BACKGROUND_FALLBACK`; successful draw submission logs
`BACKGROUND_RENDERED`. Compilation and these synthetic tests do not verify a
GPU upload, visual alignment, or a game-ready residential environment. Those
require an authorized playtest of the caller's selected PNG.
