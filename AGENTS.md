# Agent Instructions

Read and apply [`.github/copilot-instructions.md`](.github/copilot-instructions.md) in full before working in this repository.
It defines the optional project overlay, the Tech Load Protocol, and the workflow router.

The workflow kit keeps its **single source of truth** in `.github/`.

## SSOT map

| Concern | Location |
|---------|----------|
| Always-on policy, quality contract, tech triggers, terms | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) |
| Optional overlay (skip if missing) | `custom_instructions.md` at repo root |
| Workflow stages | [`.github/skills/workflow-*.md`](.github/skills/) |
| Technology rules | [`.github/skills/tech-*.md`](.github/skills/) |

Workflows: `/evolve`, `/dream`, `/requirements`, `/plan`, `/implement`, `/review`, `/council`, `/commit-message`, `/illustrate`, `/docs-repair`, `/port-dotnet-rust`, `/port-dotnet-rust-loop`.

Tech: C#, Rust, Python, .NET → Rust port mapping, tests (`tech-test.md` plus TUnit / pytest / Rust / Playwright), Web UI (`tech-web.md`), Blazor, SourceGen, build, documentation (`tech-docs.md`, `tech-docs-templates.md`), living spec and domain knowledge (`tech-spec.md`), the trace check (`tech-reqtrace.md`), reports to a person (`tech-reporting.md`), agent access (`tech-agent-access.md`), perspective sweep (`tech-sweep.md`), limits (`tech-limits.md`), instruction and skill authoring (`tech-instructions.md`).

Architecture of this repository: [`docs/architecture.md`](docs/architecture.md) (components, where they are, what is easy to miss).

## Shared skill pointers

| Mechanism | Path | Read by |
|-----------|------|---------|
| Agent skills | [`.claude/skills/`](.claude/skills/) | Claude Code, Cursor, VS Code (GitHub Copilot) |

Each `SKILL.md` points to its `.github/skills/` file. Keep this per-skill pointer set shared by the three tools above.

## Codex integration (VS Code extension)

Codex loads this file as repository guidance. Its [`$repo-workflow` skill](.agents/skills/repo-workflow/SKILL.md)
routes requests to the central workflow and technology skill tables. See [Codex usage](README.md#codex-usage-vs-code-extension).

## Claude Code integration (CLI and VS Code extension)

| Mechanism | Path | Role |
|-----------|------|------|
| Bootstrap | [`CLAUDE.md`](CLAUDE.md) | Imports `.github/copilot-instructions.md`; tool mapping |
| Workflows | `/workflow-plan`, `/workflow-review`, … | Skill names; `/plan` and `/review` are built-in Claude Code commands |

## Cursor integration

| Mechanism | Path | Role |
|-----------|------|------|
| Always-on rule | [`.cursor/rules/copilot-ai-workflow.mdc`](.cursor/rules/copilot-ai-workflow.mdc) | Bootstrap + SSOT paths |
| Slash commands | [`.cursor/commands/`](.cursor/commands/) | Same names as workflows above |

## GitHub Copilot integration

| Mechanism | Path |
|-----------|------|
| Repository instructions | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) |
| Custom prompts | [`.github/prompts/`](.github/prompts/) |

All tools share the same `.github/` content. Update rules in one place only.
