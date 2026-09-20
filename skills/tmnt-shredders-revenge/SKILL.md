---
name: tmnt-shredders-revenge
description: Use when creating or extending a Teenage Mutant Ninja Turtles Shredder's Revenge Steam mod, adding a photo-based playable character, animating sprites, designing a street-route level from addresses, or diagnosing this framework's asset and runtime hooks on another Windows installation.
---

# TMNT Shredder's Revenge modding

Reuse the source-built framework while treating native contracts and visible playtests as separate evidence. This skill includes proven procedures and explicitly marked extension designs; it does not turn those designs into implemented tools.

## Start with the actual request

Locate the framework checkout and installed Steam game. If either is missing, use [setup](references/setup.md). Do not assume this skill's installation directory is the repository, reuse another PC's paths, or clone the repository's old default branch by accident. Read the checkout's `AGENTS.md` and record the framework revision.

Create a task-local brief using [project-brief.json](assets/project-brief.json): requested character identity/weapon, donor versus new roster slot, start/end route and walking direction, season, objective, and available providers. Ask only for unresolved choices needed for the task. Keep personal photographs and precise route details local.

## Choose the relevant procedure

| Task | Read |
| --- | --- |
| New machine, install compatibility, build and rollback | [Setup](references/setup.md) |
| Native hooks, scene/encounter changes, reusable architecture | [Framework](references/framework.md) |
| Photo to character, manifest, animation, selection UI | [Characters and animation](references/characters-animation.md) |
| Maps addresses to scenery and playable level | [Levels and routes](references/levels-routes.md) |
| Debug, acceptance checks, GitHub delivery | [Verification and delivery](references/verification-delivery.md) |

## Capability boundaries

- Implemented: bounded inspectors, source-built isolated launcher, guarded three-wave edits, three background panels over the native Baxter approach, full-color Leo-slot artwork/name override, explicit character manifests and previews.
- User-observed: residential background prototype and Malcolm appearing in game. Malcolm animation was reported jumpy; smooth motion is not established.
- Extension work: another donor, an independent roster slot, custom combat/projectiles, arbitrary route-segment loading, continuous Street View reconstruction, and a tested PixelLab animation adapter. Explain this distinction when one is requested, then implement the missing piece within the authorized task.

## Working invariants

Check the three supported assembly hashes before native execution; on mismatch inspect and port the adapter rather than changing the allowlist alone. Keep original Steam files untouched, use the owned stage, and preserve save suppression. Native probes execute assemblies; Python inspectors do not.

Keep animation names, frame rectangles, pivots, events and collision metadata explicit. Preview durations do not replace native timing. Full-color art needs the custom shader path; a valid image is not a native indexed atlas.

Generate through available, authorized providers. Preserve originals and provenance, validate import geometry/alpha, and compare motion at game scale. Missing provider access is a prerequisite to resolve, not a reason to claim generation occurred.

Deliver a reproducible mod source/art pack, exact commands, supported build, tests, observed playtest evidence and limitations. Use the [evidence template](assets/verification-record.md). Do not distribute game assemblies, ripped assets, private photographs, exact addresses or credentials as part of a public skill/mod package.
