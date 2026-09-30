# Reusable TMNT modding skill

The portable entrypoint is [`skills/tmnt-shredders-revenge/SKILL.md`](../skills/tmnt-shredders-revenge/SKILL.md). It captures the framework and its verified failure modes for another Codex session or Windows machine. The skill contains no personal photographs, addresses, generated Malcolm images, game files or credentials.

| Workflow | Reference | Current implementation |
| --- | --- | --- |
| Fresh install, compatibility, isolated build/rollback | [Setup](../skills/tmnt-shredders-revenge/references/setup.md) | Windows x64 launcher for the pinned build |
| Hooks, encounters and adapters | [Framework](../skills/tmnt-shredders-revenge/references/framework.md) | Guarded three-wave and native Baxter-approach prototypes |
| Photo-based design, PNG import, manifest, animation/UI | [Characters](../skills/tmnt-shredders-revenge/references/characters-animation.md) | Leo-slot artwork and selection label; shared pose bank |
| Maps endpoints to unique scenery and playable geometry | [Levels/routes](../skills/tmnt-shredders-revenge/references/levels-routes.md) | Three-panel adapter; continuous-route loader remains extension work |
| Playtests, troubleshooting and GitHub handoff | [Verification](../skills/tmnt-shredders-revenge/references/verification-delivery.md) | Distinct structural, runtime and visual evidence |

PixelLab is an optional reference/skeleton animation handoff. No implemented PixelLab client, benchmark, independent roster slot, arbitrary route loader or new axe combat system is claimed. The user confirmed Malcolm appeared and reported jumpiness; motion refinement remains required.

## Install on this or another computer

For work inside this repo, Codex and Claude Code use the [repository entrypoints](agent-quickstart.md) without a separate installation. The steps below are optional for making the skill available in other projects. Clone the default `main` branch, record its commit, and use that revision when reproducing an environment:

```powershell
git clone https://github.com/wmtaff/tmnt-malcolms-revenge.git tmnt-mod
Set-Location tmnt-mod
git rev-parse HEAD
$skillSource = (Resolve-Path './skills/tmnt-shredders-revenge').Path
$codexSkillRoot = if ($env:CODEX_HOME) {
    Join-Path $env:CODEX_HOME 'skills'
} else {
    Join-Path $env:USERPROFILE '.codex/skills'
}
$skillTarget = Join-Path $codexSkillRoot 'tmnt-shredders-revenge'
if (Test-Path -LiteralPath $skillTarget) { throw 'Skill exists; compare versions before replacing it.' }
New-Item -ItemType Directory -Force -Path $codexSkillRoot | Out-Null
Copy-Item -LiteralPath $skillSource -Destination $skillTarget -Recurse
```

Start a new Codex session and invoke `$tmnt-shredders-revenge`. Automatic discovery is also enabled for relevant tasks. If it is not listed, check the active host's skills directory or supply its `SKILL.md` path; do not assume an existing session's skill catalog refreshed.

The copied bundle uses internal relative references. Its executable dependency is the separately located framework checkout, which it explains how to obtain. The other user needs their own supported Steam installation. It does not depend on the original developer's worktrees, Python environment, photos or generated-image cache. Copying it does not install the game or provider credentials.

To update, compare the installed bundle against a reviewed revision, preserve local customizations, then replace only this skill folder. To uninstall, remove only that installed folder. Neither operation changes Steam files or the framework checkout.

## Example requests

> Use $tmnt-shredders-revenge with my Steam installation and this photo to create a playable hero with a wooden axe. Start with the supported donor appearance and verify idle, walking and attacking.

> Use $tmnt-shredders-revenge to plan a summer walking-route level between my two supplied addresses. Preserve distinct landmarks and explain the runtime extension needed for the full route.

> Use $tmnt-shredders-revenge to diagnose jumping feet in this pack. Compare pivots, source scale and native frame sampling before regenerating art.

## Structure and maintenance

The short entrypoint routes to five references. `agents/openai.yaml` provides discovery metadata; `assets/` contains project/route planning templates and a verification record. Planning templates are explicitly not runtime configuration. Executable tools stay in `src/` and `tools/` so the skill does not fork their implementation.

Update the relevant reference when flags, compatibility pins, loader schemas or verified capabilities change. Check links after copying outside the repository. Keep historical proposals separate from supported behavior. A screenshot does not establish every animation, UI state or boss victory.

See [skill validation](skill-validation.md) for baseline, forward evaluation, source review and structural checks. This documentation-only package does not require new generation calls or a game launch.
