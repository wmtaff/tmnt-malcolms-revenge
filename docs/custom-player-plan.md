# Custom playable characters: recommended next milestone

Start with a custom visual character on one existing player slot. Keep the native character identity, movement, combat states, animation names, timing, attack and vulnerability boxes, and event messages. This isolates whether new art can render and animate correctly. A new roster slot and a unique moveset are separate later milestones.

## What the first player requires

Native Leonardo inspection found 150 animation records, 1,148 ordered frame references, and 644 distinct texture/rectangle pairs. These are not 1,148 independent drawings: animations reuse atlas regions, and the exact replacement-art workload depends on retained content and pose reuse. A complete visual replacement nevertheless needs coverage beyond idle, walk, and attack, including airborne, damage, throws, specials, recovery, and contextual animations.

The existing frame PNG archive helps as a local visual reference, but does not replace native metadata. Each exported frame must stay associated with its animation, frame index, source rectangle, pivot/offset, duration, texture identity, gameplay boxes, anchors, and event messages. Changes to attack timing or geometry require deliberate gameplay design and validation.

The native player renderer uses a separate palette texture and enabled shader palette. Ordinary opaque/full-color background PNG loading is not automatically suitable for players. Establish a palette-preserving round trip or a correctly integrated full-color player render path before selecting an export format.

## Implementation sequence

1. **Read-only player contract exporter.** Bound native content reads, pin the supported build, and export animation/frame/palette dependency metadata into ignored local artifacts. Preserve native data locally; commit only exporter source and synthetic fixtures. Include a coverage checklist that reports required animation names and unresolved dependencies.
2. **No-change round trip.** Load a local replacement for one existing player through an exact, opt-in runtime hook. Confirm identical placement, timing, palette behavior, outlines/flashes, and animation events. Keep the Steam installation untouched and retain native fallback when validation fails.
3. **Small custom-art proof.** Choose the character design and closest existing moveset. Use OpenAI image generation for a consistent design reference and controlled poses. Produce and review idle, walk, attack, and hurt sequences first, while clearly marking remaining native fallback animations. Do not present this subset as a finished character.
4. **Deterministic preparation and validation.** Add explicit frame alignment, palette/alpha checks, atlas packing, frame mapping, animation coverage, and preservation checks for timings/events/boxes. These are proposed tooling requirements, not capabilities of the current single-frame sprite validator. Generated images need native-scale visual review and likely manual cleanup.
5. **Complete character coverage.** Replace remaining poses, weapons/effects where appropriate, HUD/selection art, and optionally voice/audio. Run a controlled movement/combat checklist, including damage, recovery, throws, specials, defeat/revive, contextual scenes, and local co-op. Document every deliberate fallback.
6. **Optional independent roster identity.** Add character data, actor-template mapping, display/localization assets, save/progression support, unlock rules, and menu/navigation integration. Verify roster-index consistency before multiplayer; matching art files alone cannot establish network compatibility. A unique moveset additionally requires actor/state/attack behavior work.

## First deliverable and acceptance

The next implementation deliverable should be a Leonardo-based contract exporter and a no-change replacement proof, followed by a small custom-art animation set after the design is chosen. Acceptance requires a replacement player visible in the isolated playtest with stable pivots and preserved native events, an explicit coverage report, and successful fallback on incompatible assets. It does not require a new roster slot yet.

## Existing tools and external precedent

`validate-sprite` already checks individual image bounds, alpha, palette counts, and optional dimensions. It does not validate a complete animation set, encode native palette indices, pack an atlas, or preserve gameplay events. The new character pipeline must add those contracts instead of assuming a valid PNG is a valid player.

Historic TMNTModAPI source demonstrates asset-request interception and content replacement, and its texture extractor explicitly handles unpremultiplying alpha when exporting PNGs. These are useful precedents, not evidence that old binaries work with the installed build. Prefer extending the source-built, isolated runtime already validated here.

Sources inspected 2026-09-08: [ApiContentManager](https://github.com/Platonymous/TMNTModAPI/blob/main/src/ModApi/Content/ApiContentManager.cs), [TextureExtractor](https://github.com/Platonymous/TMNTModAPI/blob/main/src/TextureExtractor/Program.cs). Native findings and unresolved details are recorded in [asset research](custom-player-assets-research.md) and [runtime research](custom-player-runtime-research.md).
