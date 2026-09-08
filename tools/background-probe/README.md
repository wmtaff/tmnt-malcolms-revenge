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
`BackgroundRuntime.Configure(installedEngineAssembly, pngPath, Log)`, then install
a `TextureGameObject.Render` Harmony prefix returning
`BackgroundRuntime.TryRender(__instance)`. Call `Dispose()` at shutdown on the
render thread. It needs no additional compile references. `SelfTest()` exercises
tile layout, PNG dimension bounds, and fallback on reflection mismatch without
game assemblies or Harmony.

The image path is supplied by the caller; no location, reference address, or draft
art is embedded in source. It accepts bounded 8-bit RGB/RGBA PNG input, at most
16 MiB and 4,194,304 pixels, loads it once per graphics device, and draws three
horizontal repeats over native X 4096..6464 at world height 480 and repeat width
960. For source 1774 by 887, that means about 1.85 source pixels per world pixel.
The final repeat is cropped to the native ground's right edge. Nearest sampling
is inherited from the native renderer. Supply opaque art to cover the old floor.

Camera, collision, enemy behavior, native animation objects, and boss machinery
remain controlled by the scene. A failed optional replacement preserves native
rendering and logs `BACKGROUND_FALLBACK`; successful draw submission logs
`BACKGROUND_RENDERED`. Compilation and these synthetic tests do not verify a
GPU upload, visual alignment, or a game-ready residential environment. Those
require an authorized playtest of the caller's selected PNG.
