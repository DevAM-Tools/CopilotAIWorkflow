# Agent Instructions

Structured agent workflow with **single source of truth** in `.github/`. Cursor-specific files under `.cursor/` are thin pointers only — no rule duplication.

## SSOT map

| Concern | Location |
|---------|----------|
| Always-on policy, quality contract, tech triggers | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) |
| Workflow stages (`/plan`, `/implement`, `/review`, `/complex-task`) | [`.github/skills/workflow-*.md`](.github/skills/) |
| Technology rules (C#, TUnit, Blazor, SourceGen, build) | [`.github/skills/tech-*.md`](.github/skills/) |

## Cursor integration

| Mechanism | Path | Role |
|-----------|------|------|
| Always-on rule | [`.cursor/rules/copilot-ai-workflow.mdc`](.cursor/rules/copilot-ai-workflow.mdc) | Bootstrap + SSOT paths |
| Slash commands | [`.cursor/commands/`](.cursor/commands/) | `/plan`, `/implement`, `/review`, `/complex-task` |
| Agent skills | [`.cursor/skills/`](.cursor/skills/) | Discovery wrappers → `.github/skills/` |

## GitHub Copilot integration

| Mechanism | Path |
|-----------|------|
| Repository instructions | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) |
| Custom prompts | [`.github/prompts/`](.github/prompts/) |

Both tools share the same `.github/` content. Update rules in one place only.
