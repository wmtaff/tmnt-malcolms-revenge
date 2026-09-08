# Encounter prototype implementation plan

**Goal:** A configurable three-wave Episode 1 prototype with safe visible launch and rollback.
**Spec:** docs/encounter-design.md
**Architecture:** Reuse native wave progression; strictly validated JSON specifies bounded edits to exact existing enemies/waves. Reflection adapter applies edits through verified hooks. PowerShell owns playtest lifecycle.

- [ ] Research agent: identify native wave progression and read first three wave records; return selectors and hook semantics. Own tools/wave-probe only.
- [ ] Configuration agent: implement bounded strict schema parsing and synthetic rejection tests. Own EncounterConfig.cs and EncounterConfigTests.cs.
- [ ] Controls agent: implement visible Start/Status/Stop with process identity checks and tests. Own playtest.ps1, its Python tests, and usage doc.
- [ ] Coordinator: integrate native adapter, configurable replacement for hard-coded spawn proof, sample three-wave config, compilation and CI changes. Test baseline/no-match/idempotency and whole-wave preflight.
- [ ] Independently review changes, build isolated copy, launch sample, verify live application and playtest transitions.
- [ ] Publish branch with accurate verification record and passing CI.

Do not invent serialized fields or group membership. Coordinate the final schema after native research; do not launch from an unverified partial configuration. Original inspection commands remain read-only.
