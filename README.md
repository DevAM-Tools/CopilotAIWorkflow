# CopilotAIWorkflow

Structured AI agent workflow for **GitHub Copilot** and **Cursor**: planning, implementation, and review with layered instructions and on-demand skills.

---

## Why this repository exists

Default chat behavior is fast but inconsistent across sessions. This setup enforces deterministic, auditable agent behavior.

- No implementation before plan approval.
- Explicit phases: plan, implement, review, complex-task.
- Review loops until zero Error findings (or explicit block).
- Warnings treated as defects.
- Token-efficient chat; high-quality plan and review artifacts.

---

## Repository structure

| Path | Role |
|------|------|
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | **SSOT** — always-on policy, quality contract, tech-load protocol, terse communication |
| [`.github/skills/workflow-*.md`](.github/skills/) | **SSOT** — workflow stages (loaded on `/plan`, `/implement`, etc.) |
| [`.github/skills/tech-*.md`](.github/skills/) | **SSOT** — technology rules (loaded on scope trigger) |
| [`.github/prompts/`](.github/prompts/) | GitHub Copilot entry points (~10 lines); no stage duplication |
| [`.cursor/rules/copilot-ai-workflow.mdc`](.cursor/rules/copilot-ai-workflow.mdc) | Cursor always-on bootstrap (pointers only) |
| [`.cursor/commands/`](.cursor/commands/) | Cursor slash commands (`/plan`, `/implement`, `/review`, `/complex-task`) |
| [`.cursor/skills/`](.cursor/skills/) | Cursor skill discovery wrappers → `.github/skills/` |
| [`AGENTS.md`](AGENTS.md) | Cross-tool SSOT map and integration overview |

---

## SSOT layering

Each concern has exactly one authoritative file under `.github/`. Copilot prompts and Cursor rules/commands/skills reference by path; they do not copy rules.

| Concern | SSOT |
|---------|------|
| Quality, C#, templates, terse, tech triggers | `.github/copilot-instructions.md` |
| Workflow stages and gates | `.github/skills/workflow-*.md` |
| Build, CPM, dependencies | `.github/skills/tech-solution.md` |
| TUnit / Blazor / SourceGen | respective `.github/skills/tech-*.md` |
| Copilot user entry | `.github/prompts/` |
| Cursor user entry | `.cursor/commands/`, `.cursor/skills/` |

Entry points read `copilot-instructions.md` plus one workflow skill. No partial checklists.

---

## Configuration precedence

1. [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
2. Skills loaded per Tech Load Protocol (Section 3) and workflow triggers (Section 6)

Cursor: [`.cursor/rules/copilot-ai-workflow.mdc`](.cursor/rules/copilot-ai-workflow.mdc) is a bootstrap only — it does not override SSOT content.

---

## On-Demand skills

| Skill | Load when |
|-------|-----------|
| `tech-tunit.md` | Test files or test projects in scope |
| `tech-blazor.md` | `.razor` / `.razor.cs` / `.razor.css` in scope — exhaustive **bUnit** component tests |
| `tech-sourcegen.md` | Generator code in scope |
| `tech-solution.md` | Build files, `.csproj`, `GlobalUsings.cs` |
| `workflow-plan.md` | `/plan` |
| `workflow-implement.md` | `/implement` |
| `workflow-review.md` | `/review` |
| `workflow-complex-task.md` | `/complex-task` |

Agents must `Read` matching skills before edits. Missing skill when trigger matches = Error in review.

---

## Workflow overview

Stages live in workflow skills only. Prompts do not repeat them.

- **Plan:** `workflow-plan.md` — gather context, Grill Me, write artifact
- **Implement:** `workflow-implement.md` — prepare, execute steps with review gates, verify
- **Review:** `workflow-review.md` — scope, load, review, output
- **Complex-task:** `workflow-complex-task.md` — orchestrates plan → checkpoint → implement/review loop

Review gates and checklist updates: `workflow-implement.md` Stage 2 and plan artifact Task Checklist.

---

## Cursor usage

Cursor picks up the workflow automatically when this repo (or a copy) is open:

| Mechanism | How to use |
|-----------|------------|
| Always-on rule | `.cursor/rules/copilot-ai-workflow.mdc` loads on every session |
| Slash commands | Type `/plan`, `/implement`, `/review`, or `/complex-task` in chat |
| Agent skills | Skills auto-discover via descriptions; each points to `.github/skills/` SSOT |
| `AGENTS.md` | Overview and SSOT map for any agent reading project instructions |

Natural-language triggers (`plan this feature`, `review the PR`) work the same as slash commands per `copilot-instructions.md` Section 6.

---

## GitHub Copilot usage

Custom prompts in [`.github/prompts/`](.github/prompts/) mirror [`.cursor/commands/`](.cursor/commands/) — same three-line body, different tool format.

---

## Adopting in another repository

1. Copy [`.github/`](.github/) (instructions, skills, prompts).
2. Copy [`.cursor/`](.cursor/) and [`AGENTS.md`](AGENTS.md) for Cursor support.
3. Copy [`COPYRIGHT`](COPYRIGHT) and [`LICENSE`](LICENSE).
4. Validate paths after copy (`.github/` and `.cursor/` references are relative to repo root).

---

## Solution components

| Project | Role |
|---------|------|
| [`CSharpStyleValidator`](src/CSharpStyleValidator/) | Roslyn analyzer NuGet (CSV001–CSV007) |
| [`ExitPoints`](src/ExitPoints/) | Roslyn exit-point collection for coverage workflows |
| [`CoverageGapAnalysis`](src/CoverageGapAnalysis/) | Cobertura parsing and exit/branch gap reporting library |
| [`CoverageGap.Tool`](src/CoverageGap.Tool/) | CLI — `report project` and `manifest project` |
| [`samples/CSharpStyleValidator.Demo`](samples/CSharpStyleValidator.Demo/) | Local analyzer demo with violation fixtures |

**CI** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)): Release build; test matrix on Linux, Windows, and macOS (x64 and ARM); exit-gap report (`exitGapCount == 0`) on every production project. Release builds auto-pack NuGet packages to `artifacts/`.

---

## CSharpStyleValidator

Roslyn analyzer package enforcing CopilotAIWorkflow C# style as **compiler errors** (CSV001–CSV007).

| ID | Rule |
|----|------|
| CSV001 | Line length ≤ 160 (strings/comments masked) |
| CSV002 | No `var` |
| CSV003 | Private `_PascalCase` |
| CSV004 | No `.Result` / `.Wait()` on `Task` |
| CSV005 | `using` only in `GlobalUsings.cs` |
| CSV006 | At most one exit point per source line per callable |
| CSV007 | Volatile fields via `Volatile` or `Interlocked` only |

XML documentation on public APIs is enforced by built-in **CS1591** in production projects (`GenerateDocumentationFile` + `TreatWarningsAsErrors`).

**Consumer setup:**

```xml
<PackageReference Include="CSharpStyleValidator" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Optional: `dotnet_diagnostic.IDE0008.severity = none` to avoid duplicate `var` IDE warnings.

Optional line length override (CSV001): set `build_property.csv_max_line_length = N` in `.editorconfig` (requires `CompilerVisibleProperty` in the analyzer package).

**Demo:** [`samples/CSharpStyleValidator.Demo/README.md`](samples/CSharpStyleValidator.Demo/README.md) — try rules locally; use `-p:IncludeViolations=true` to see diagnostics.

Build and test (Release build writes `.nupkg` files to `artifacts/` for packable projects):

```bash
dotnet build CopilotAIWorkflow.slnx -c Release
dotnet test CopilotAIWorkflow.slnx -c Release -- --coverage --coverage-output-format cobertura
dotnet run --project src/CoverageGap.Tool -c Release -- report project path/YourProject.csproj --search-root src --repo-root .
```

Coverage gap report (`coveragegap report`): exit-point gaps first, then branch gaps (informational only). Formats: `--format agent` (JSON) or `compact` (minimal tokens). Optional `--include-snippet`. Release gate: `exitGapCount == 0` (`copilot-instructions.md` §4.5); workflow and commands in `tech-tunit.md`.

Optional exit manifest export:

```bash
dotnet run --project src/CoverageGap.Tool -c Release -- manifest project path/YourProject.csproj -o exits.json
```

Install globally after NuGet publish:

```bash
dotnet tool install -g CoverageGap.Tool
coveragegap report project path/YourProject.csproj --search-root src --repo-root .
```

---

## License

Copyright © 2026 DevAM. Licensed under MIT. See [LICENSE](LICENSE).

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history.
