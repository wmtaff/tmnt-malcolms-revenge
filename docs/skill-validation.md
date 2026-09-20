# Skill validation record

Date: 2026-09-20. Framework baseline: `8be3c86`. Scope: portable skill and documentation; no new art generation, native behavior changes, route reconstruction or game launch.

## Work checklist

- [x] Audit commands, prerequisites, native contracts and historical evidence.
- [x] Run a fresh-context baseline without the skill before authoring.
- [x] Author a discoverable entrypoint, focused references and usable planning/evidence assets.
- [x] Run the new-install request with the completed skill.
- [x] Independently review source accuracy and capabilities.
- [x] Validate frontmatter, internal links, JSON templates and installed-copy equivalence.
- [x] Run the repository Python suite and record actual results.
- [x] Prepare reviewed source for publication and install the standalone skill.

## Baseline and forward test

The baseline request combined a fresh Windows Steam installation, photo-based hero, smooth animation and nonrepeating Maps route ending in Baxter. Without the skill, the agent correctly avoided inventing framework specifics, but could not provide the actual build/export/launch commands, hashes, fixed donor contract, native sampling behavior or three-panel/continuous-route distinction. It said: “I need the prior mod framework and a known-good build before I can give reliable framework-specific build or install commands.” This was a reference-retrieval gap, not a safety violation.

A separate fresh-context agent then received only the bundle path and the same scenario, plus changed-hash and preview-smooth/runtime-jumpy scenarios. It retrieved the correct commands and three hashes, distinguished photo-based appearance from internal Leo identity, explicitly identified the unimplemented arbitrary-route loader, and did not invent a launch command for it. It explained the native sampling formula and why preview duration changes do not fix game motion. It required contract inspection and porting before admitting changed hashes. All three scenarios passed; no external actions were executed.

The evaluator noted genuine implementation gaps rather than hidden skill features: no arbitrary-route loader, generic unsupported-build investigation rather than an automated port, no quantitative smoothness tool, and detailed schemas/examples living in the required framework checkout. These are documented limitations. A moving publication branch also needs revision recording for reproducibility; the setup reference includes this.

## Independent source review

The reviewer checked hashes, commands, bounds, sampling, scene spans, folder selectors, mode flags, privacy and capability claims against actual source. No blocking defect was found. One correction was made: archive inventory reads ZIP headers without opening/extracting members, but it does not enforce an archive-size/member-count limit. The skill no longer labels that whole operation bounded.

User feedback that Malcolm appeared and was jumpy comes from the conversation, now recorded separately in character verification. It does not fill unobserved live acceptance checkboxes.

## Structural and repository checks

The official skill-creator `quick_validate.py` accepted both repository and installed copies. Its PyYAML dependency was installed only in this worktree's ignored validation environment, not added to the mod's runtime dependencies.

The bundle audit found 10 files, resolved all 10 internal relative Markdown links within the standalone bundle, parsed both JSON planning templates, and confirmed only Markdown/JSON/YAML files were packaged. All 10 installed file hashes matched the source. The local installed directory is the active user's Codex `skills/tmnt-shredders-revenge` folder; no worktree symlink is needed.

`python -m unittest discover -s tests -q` passed all 76 tests in the new editable-install environment. `git diff --check` passed. No native code changed, so a game launch was not used as evidence for this documentation package. Fresh-context scenarios and independent source review passed as described above.
