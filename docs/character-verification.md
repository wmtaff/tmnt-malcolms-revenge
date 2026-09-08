# Malcolm character verification

## Scope

This is a visual replacement for the existing Leonardo slot with a custom selection name and original Malcolm artwork. It retains the native moveset, collision, progression, voice, and explicitly mapped native effects. It is not an extra roster slot or a custom axe moveset.

## Reproducible inputs

The pack is `art/characters/malcolm/manifest.json`. Its 5 original PNG sheets and their SHA-256 values are recorded in sibling `generation.json`, along with generation prompts and corrections. There are 33 frames including the portrait, 21 art sequences, 134 body-animation mappings and 16 native effect passthrough names. Comparing the mapping/passthrough union against the local exported Leo contract produced no missing or extra names.

Use [the complete authoring workflow](adding-characters.md) for export, generation, import, validation, preview, build, launch and rollback commands.

## Structural and build checks

The actual pack passed Python inspection and C# CPU loading. The portrait intentionally crops part of a declared single grid cell, producing a nonblocking grid-coverage warning. That warning is not a transparency or reference error.

The combined isolated launcher compiled against the pinned Harmony dependency. Runtime, residential, identity and art-config self-tests passed. The native-shaped identity tests exercise real Harmony patches; the art self-test checks configuration/import logic rather than GPU drawing. Native reflection and IL checks separately verified the draw/push signatures, texture upload API, projection construction and flip/origin math.

After integrating the folder-path fix, the complete Python suite passed 76 tests and the launcher self-test passed. A separate reviewer confirmed the folder-path behavior from native IL and tested normalized paths plus unrelated/lookalike donor negatives.

## First live failure and diagnosis

The first character-enabled session started on 2026-09-08 at 23:51 UTC. Its log confirmed manifest preflight, identity and renderer hook installation, and `MALCOLM_SELECTION_PRESENTED`. A live selection-screen inspection showed the name MALCOLM with Leo artwork. There were no custom artwork draw or fallback messages.

Native IL inspection showed that `AnimatedObject2dData.LoadTextures` stores the containing folder in `Path`. The original renderer compared that value to a full asset path, so no target matched. This failure demonstrates why compilation, synthetic tests, and an installed-hook message do not establish visual integration. The fix must compare normalized exact donor folders, with regression tests for unrelated donors and misleading path prefixes.

The corrected session began at 23:55 UTC. At 23:57:59 its log recorded live coverage of all 150 names/1,148 native frame references and a successful custom body draw. At 23:58:05 it recorded a custom HUD draw. These occurred while navigating the native tutorial and establish GPU-hook execution, not yet complete gameplay acceptance. Automated key presses did not reliably advance the tutorial; visual review continued with user navigation.

## Live acceptance checklist

- [ ] Custom selection portrait and Malcolm label appear together.
- [ ] Malcolm renders in the level, with correct transparency and approximate native scale.
- [ ] Walk/run and facing changes keep the ground anchor stable.
- [ ] Jump and attack poses render; native combat and progression remain functional.
- [ ] Damage, knockdown/getup, grab/throw, specials and end-of-level states are reviewed.
- [ ] HUD, pause, world-map and completion UI are visually reviewed independently.

These are visual checks, not inferred from mapping completeness. Update this checklist only with observed results. Shared poses, retained Leo effects/voices, and unreviewed state transitions remain explicit prototype limitations.
