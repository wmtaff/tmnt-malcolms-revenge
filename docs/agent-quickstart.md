# Point your agent at this repository

Clone the default branch and open its root in Codex or Claude Code:

```sh
git clone https://github.com/wmtaff/tmnt-malcolms-revenge.git
cd tmnt-malcolms-revenge
```

Codex's repository entrypoint is [AGENTS.md](../AGENTS.md). Claude Code's [CLAUDE.md](../CLAUDE.md) imports that same guide. Both route to the [bundled modding skill](../skills/tmnt-shredders-revenge/SKILL.md) as normal repository files. There is one shared source of instructions; no plugin, global skill copy, API key or previous conversation is needed just to understand and work on the project.

Give the agent a concrete task, for example:

> Read the repository instructions. Help me build a new playable character from my supplied image using my Steam installation. Identify the supported donor and game compatibility, then implement and verify the work. Document reusable changes.

> Read the repository instructions. Plan and implement a level along the walking route between the two addresses I provide. Preserve distinct landmarks and clearly identify the continuous-route loader work still needed.

For another harness, ask it to read `AGENTS.md` explicitly. Instruction discovery depends on harness settings and repository trust; if automatic loading is disabled, reading the file directly still works. Hosting a remote URL in a browser is not the same as giving an agent filesystem access to the checkout.

## Prerequisites depend on the task

- Documentation and source review need only the checkout.
- Python validation/tests need Python 3.11+ and the editable install in `AGENTS.md`; they do not need Steam files.
- Native build/playtest needs Windows x64, a locally owned supported Steam install and the documented compiler/dependency setup. Cloud/Linux agents can still perform source work and synthetic Python checks, but cannot claim live Windows gameplay verification.
- New artwork, PixelLab or Maps inspection needs the selected provider/tool access. The repo does not supply paid accounts, credentials or automatic route reconstruction.

Use [setup](../skills/tmnt-shredders-revenge/references/setup.md) for actual build/launch commands and [verification](../skills/tmnt-shredders-revenge/references/verification-delivery.md) for evidence and rollback. Local skill installation remains optional for using the workflow outside this repository; see [the installation guide](reusable-skill.md).

The default `main` includes the framework and skill. Older `codex/foundations` links refer to a historical branch and lack later capabilities. Record `git rev-parse HEAD` when reproducing a result; preserve existing local changes when moving from an older checkout.

Instruction conventions: [Codex project guidance](https://developers.openai.com/codex/guides/agents-md) and [Claude Code project memory](https://code.claude.com/docs/en/memory). The local entrypoint fallback avoids depending on optional skill-discovery features.
