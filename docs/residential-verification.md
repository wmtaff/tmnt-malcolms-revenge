# Summer residential prototype verification

The prototype depicts a light-pink suburban house, a green connecting street, and a community park with playground. Episode 1 selection redirects to native Stage 12 at world position 4250,360. Six intervening native encounter blocks are skipped; three existing Foot Soldiers precede the original Baxter introduction and fight. Ten exact laboratory decorations are hidden only during rendering.

Verified on 2026-09-08:

- All 61 Python tests pass, including bounded background diagnostics, repeat previews, residential argument serialization, and mode selection.
- Launcher self-tests pass, including actual Harmony surrogate hooks, encounter checks, and background validation/fallback tests.
- Separate residential synthetic tests pass transfer, rollback, route identity preflight, repeated reset, boss preservation, and decorative selectors.
- Framework build and isolated staging succeed against the pinned installed game assemblies.
- The playtest starts, installs residential hooks, and renders its introduction. Level traversal and boss victory are not yet verified.

Read-only native inspection confirms a straight camera waypoint path and floor tiles throughout world X4240..6455, Y352..455, with a wall at X6456. This supports the route but does not replace checking actor collision, dynamic objects, and camera behavior in play. The initial viewport could reveal unchanged scenery to the left of the replaced ground.

The art is a first visual prototype: full-color 1774×887 generated images scaled to three contiguous native spans totaling 2368×456. The image generation prompts and measured edge/palette warnings are saved with the assets. Seam continuity, intended pixel density, and composition still require in-game review. Existing Baxter cutscene poses and gameplay props may look incongruous outdoors; native boss dependencies remain intact.

Run instructions are in [playtest controls](playtest-controls.md). Stop the tracked playtest and launch Steam normally to return to the original game. Do not distribute the ignored staged game files, references, or diagnostics.
