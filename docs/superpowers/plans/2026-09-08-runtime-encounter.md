# Runtime encounter implementation plan

> Use coordinated subagents in explicit worktrees with independent final review.

**Goal:** Verify one runtime patch and one visible encounter modification on the installed Steam version.
**Architecture:** Source-built .NET launcher, exact metadata-derived hooks, isolated playtest files and ignored evidence.
**Spec:** docs/runtime-design.md

## Tasks

- [x] Runtime agent: determine runtime/entrypoint, inspect historical loader strategy, return exact supported hook signatures.
- [x] Scene agent: inspect the first level's serializer/object types and identify one bounded spawn change.
- [x] Coordinator: prepare isolated playtest copy, establish unmodified launch with user-operated menu controls, implement version-checked launcher/patch based on agent evidence.
- [x] Verify runtime logging first, then encounter mutation with screenshots and logs. Record what is directly observed versus inferred. Exact coordinates are verified by logs; gameplay captures establish rendering, not a visual measurement of enemy displacement.
- [x] Add repeatable build/launch/rollback instructions and synthetic tests for reusable logic.
- [x] Independent code review, complete tests, publish feature branch and CI verification.

Compatibility-dependent implementation details will be recorded after the two metadata probes; do not invent signatures or patch unknown fields.
