# Reusable workflows

This index connects the procedures for building and verifying the mod. Start with the workflow for the result you want; research notes explain why an approach was chosen but do not replace its acceptance checks.

| Result | Procedure | Required evidence |
| --- | --- | --- |
| Inspect an installed game or asset archive | [Quick start](../README.md#quick-start), [inspection verification](verification.md) | Bounded JSON reports, source fingerprints, no changes to original files |
| Build the isolated runtime | [Runtime build](runtime-build.md) | Compiler success, runtime self-test, owned staging marker |
| Start, inspect, stop, or recover a playtest | [Playtest controls](playtest-controls.md) | Tracked process identity and fresh log/capture paths |
| Configure enemy waves | [Encounter prototype](encounter-prototype.md) | Configuration checks, spawn logs, live progression |
| Generate and import a background | [Background pipeline](background-pipeline.md) | Saved prompts and originals, validation, native-scale render inspection |
| Route the level to the native Baxter encounter | [Boss integration](boss-integration.md), [residential verification](residential-verification.md) | Scene redirection and boss-state evidence; separately verify victory |
| Create another playable character | [Adding characters](adding-characters.md) | Approved reference, explicit manifest, donor coverage, selection and gameplay checks |
| Validate and preview a character pack | [Character pipeline](character-pipeline.md) | Synthetic tests, manifest report, reviewed original art and preview |
| Adapt character identity and selection | [Player runtime](player-runtime.md) | Exact donor hooks and identity-preservation tests |

Continuous, nonrepeating address-to-address Street View generation remains [tracked work](https://github.com/wmtaff/tmnt-malcolms-revenge/issues/1). The current background workflow generates a summer residential prototype; it does not reconstruct every road segment.

## What every workflow records

For each reusable change, record its prerequisites, inputs, exact commands, output locations, validation rules, failure modes, rollback, and known limitations. Distinguish a passing structural check from a live visual result. Include source/build hashes when behavior depends on the installed game, and preserve generation prompts and selected original images when art is generated.

Keep private references, credentials, original game content, and raw runtime reports in ignored local directories. Documentation should describe how to recreate the result without requiring another person's local absolute paths or distributing native game files.

## Coordinated development

Create an isolated integration worktree and separate branches for independent implementation. Assign each agent explicit file ownership and an interface contract before it edits shared functionality. Integrate completed commits through the coordinator, inspect the combined diff, obtain independent review, run the relevant synthetic tests, and then perform the live acceptance checks. Record any difference between a tested component and the integrated result.

Do not infer completion from an agent's status or a successful compilation. Capture the actual commands and outcomes for the final integrated revision. When a limitation remains, name the unverified behavior and the next check needed to resolve it.
