# Framework architecture and extension contracts

Paths in this reference are relative to the framework checkout, not the skill directory. Read their current implementation before adapting a hook.

| Component | Responsibility |
| --- | --- |
| `src/tmnt_mod/` | Read-only game/archive inspection, sprite/background/character validation and HTML previews |
| `tools/runtime/build.ps1` | Hash gate, owned staging, pinned Harmony dependency, compilation and separate synthetic suites |
| `RuntimeLauncher.cs`, `RuntimeOptions.cs`, `playtest.ps1` | Explicit modes, diagnostics/capture, save suppression, tracked visible process |
| `EncounterConfig.cs`, `EncounterRuntime.cs`, `EncounterHooks.cs` | Bounded JSON, exact native selectors, transactional edits and rollback |
| `ResidentialRuntime.cs` | Episode 1 redirect to native Stage 12 Baxter approach and native scene changes |
| `BackgroundRuntime.cs` | Exact ground-object substitution with three original PNGs |
| `CharacterRuntime.cs` | Display-name override and donor lifecycle diagnostics without identity migration |
| `CharacterArtRuntime.cs` | Full-color sprite/UI render replacement, shader-state restoration and fallback |
| `tools/player-assets/CharacterArtConfig.cs` | Bounded runtime manifest/image loading and native-to-art frame sampling |
| `tools/player-assets/PlayerAssetExporter.cs` | Hash-gated native serializer execution to export the fixed Leo contract |

## Native execution lessons

Initialize the game's NBug/crash-reporting setup before installing game hooks as the launcher does; early unrestricted reflection previously failed around Steamworks union types. Preserve the verified initialization order when adding modules.

With the pinned Harmony 2.2.1, modifying `__args[0]` did not propagate the stage replacement. The verified stage hook uses `ref object __0`. Test argument substitution with actual Harmony and native-shaped synthetic objects; a directly called helper cannot prove patch binding.

`AnimatedObject2dData.Path` is a texture **folder**, set by `LoadTextures`, not an asset filename. Normalize slash direction, case, and trailing separators; match exact folders. Do not broaden to suffix/substring matching when an exact selector fails. Current body folder: `2d/animations/players/leonardo`; UI folders: `2d/animations/hud/playerhud/leo`, and `2d/animations/menu/{characterselect,levelcomplete,pause/powerlevel,worldmap/playerpanels}/leo`. Native palette-square widgets pass through.

Character rendering targets `AnimatedObject2dData.Render` with 12 arguments and `Renderer.DrawTexture` with 9 arguments. The background adapter uses a different 10-argument DrawTexture overload. Verify signatures, properties and argument order against the selected assemblies. Counts alone are not a portable API guarantee. GPU resources are created on the render thread, reused per device, and disposed on changes/shutdown. Shader Push/Pop must balance even on failure. Current custom character rendering does not reproduce optional native `size`/`overrideTexture` behavior; inspect callers before using those features.

Native indexed player textures use a separate palette. Generated full-color pixels are uploaded as premultiplied RGBA under SimpleShader; replacing an indexed atlas with an arbitrary PNG is not equivalent. Preserve native positions, flips, angle, tint, scale and depth; custom pivots/scale affect artwork, not hitboxes.

## Encounters and levels

`encounters/episode1-lobby.json` is a real reusable three-wave example, not a generic level authoring schema. It targets the first three waves of one Episode 1 camera block, retains group membership and native thresholds, and edits positions plus the delay **to the next wave**. Exactly ordered wave indexes 0,1,2 are required. Preflight validates scene/block/group/enemy identities and original values before any edit; failure preserves native data. Arbitrary additional waves/boss spawning need new adapter work.

Use `NATIVE_ENEMY_SPAWN` to inspect positions after native reset, not only the positions written before activation. A progressing wave log is not proof that every prior enemy died: native overlap thresholds can permit advance.

The residential adapter loads the native Stage 12 boss approach through Episode 1, forces a known spawn, configures Foot Soldiers and preserves the boss lifecycle/camera. It is not an exported new level or independent episode slot. Scene GUIDs, positions, decoration selectors and camera-block decisions are specific to this supported build. For another level, export only the required native records, establish their contracts and revise exact selectors with preflight/rollback tests. Background art never defines collision, camera constraints, enemy activation or victory conditions by itself.

## Extending safely and reviewably

Use a feature worktree with explicit file ownership for parallel implementation. Keep probes and native exports under ignored `artifacts/`; commit original code, synthetic fixtures, original generated art, prompts and reviewed configuration. Standalone tests defining native-shaped classes must compile into separate test executables, never the actual launcher. Update both `build.ps1` and `.github/workflows/runtime.yml` source lists when adding a module.

For each adapter extension, document selectors, expected original state, mutation scope, failure/rollback, mode flag, synthetic coverage and live evidence. Configure all CPU-side inputs before native mutation. Keep package IDs/display names separate from internal roster identity. Independent roster slots require explicit preload/UI, save/index/progression and network work; a renamed manifest does not supply that infrastructure.

Detailed original contracts remain in the checkout's `docs/custom-player-assets-research.md`, `docs/custom-player-runtime-research.md`, `docs/boss-integration.md`, and `docs/encounter-prototype.md`.
