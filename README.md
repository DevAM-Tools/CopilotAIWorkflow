# CopilotAIWorkflow

Structured AI agent workflow for **GitHub Copilot** and **Cursor**, plus NuGet packages for C# style enforcement and exit-point coverage.

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![CSharpStyleChecker](https://img.shields.io/nuget/v/CSharpStyleChecker)](https://www.nuget.org/packages/CSharpStyleChecker/)
[![ExitPointGaps](https://img.shields.io/nuget/v/ExitPointGaps)](https://www.nuget.org/packages/ExitPointGaps/)

**Release 1.0.0** — two public packages replace the former three-package layout. **CSharpStyleChecker** and **ExitPointGaps** cover the same scope as the deprecated **CSharpStyleValidator**, **CoverageGap.Tool**, and **CoverageGapAnalysis** (no separate analysis library). See [Migration](#migration-from-deprecated-packages).

---

## Quickstart

Two packages on [nuget.org](https://www.nuget.org/packages?q=Owner%3ADevAM). No extra wiring: add **CSharpStyleChecker** or register **ExitPointGaps** as a local dotnet tool — `ExitPoints` ships inside **CSharpStyleChecker**; no second analyzer reference.

**Requires:**

| Component | Minimum |
|-----------|---------|
| **CSharpStyleChecker** (analyzer) | [.NET SDK 9.0.300+](https://dotnet.microsoft.com/download) or Visual Studio 2022 17.14+ (Roslyn 4.14; see `Directory.Packages.props`) |
| **ExitPointGaps** (CLI) | .NET SDK 10.0 (local tool manifest) |
| **This repository** | .NET SDK 10.0 (`global.json`) |

**NuGet vs. repo clone:** Adding `CSharpStyleChecker` via NuGet loads analyzers from `analyzers/dotnet/cs` (including bundled `ExitPoints.dll`) — no MSBuild targets in the package; analyzers apply only on the project that references the package. Cloning this repo applies the analyzer automatically via root [`Directory.Build.targets`](Directory.Build.targets) (opt out with `ApplyCSharpStyleChecker=false`).

### 1 — Enforce C# style (analyzer)

From your project directory:

```bash
dotnet add package CSharpStyleChecker
```

Or in `.csproj`:

```xml
<PackageReference Include="CSharpStyleChecker" Version="1.*" />
```

Rebuild. Violations surface as compiler errors (CSC001–CSC008). No `PrivateAssets`, `IncludeAssets`, or separate `ExitPoints` package — the NuGet metadata handles that.

[Rule reference](#csharpstylechecker) · [Demo sample](samples/CSharpStyleChecker.Demo/README.md)

### 2 — Exit-point coverage gate (CLI)

**ExitPointGaps** lists exit-point gaps (`exitGaps[]`). Tests + Cobertura + exit-gap JSON in one call. **Gate:** `summary.exitGapCount == 0` (branch gaps informational). Analysis code lives inside the tool — no separate library package. **SSOT:** [`.github/skills/tech-tunit.md`](.github/skills/tech-tunit.md).

**Setup** (repo root):

```bash
dotnet new tool-manifest
dotnet tool install ExitPointGaps --version 1.*
dotnet tool restore   # fresh clone / new machine
```

Add MTP to repo-root `global.json` and TUnit to `{Project}.Tests` — see [tech-tunit.md](.github/skills/tech-tunit.md#framework).

| Task | Command |
|------|---------|
| Gate repo (auto-discover solution) | `dotnet tool run exitpointgaps --repo-root .` |
| Gate named solution | `dotnet tool run exitpointgaps run solution path/File.slnx --repo-root . --configuration Release --format agent` |
| Gate project | `dotnet tool run exitpointgaps run project path/Proj.csproj --repo-root .` |
| Plan exits (no test run) | `dotnet tool run exitpointgaps plan project path/Proj.csproj -o exits.json --repo-root .` |

Fix every item in `exitGaps[]`; re-run until `exitGapCount == 0`. Exit codes: `0` pass · `1` gap/failure · `2` usage.

**This repo (contributors):** `dotnet run --project src/ExitPointGaps -c Release -- run --repo-root .`

CLI options: [ExitPointGaps](#exitpointgaps).

---

## NuGet packages

| Package | Consumer TFM | Usage | Role |
|---------|--------------|-------|------|
| [CSharpStyleChecker](https://www.nuget.org/packages/CSharpStyleChecker/) | `netstandard2.0`+ (SDK-style) | `PackageReference` | Roslyn style analyzer (CSC001–CSC008); bundles `ExitPoints` |
| [ExitPointGaps](https://www.nuget.org/packages/ExitPointGaps/) | N/A (CLI) | Local `dotnet tool install` | `dotnet tool run exitpointgaps` — lists exit-point gaps |

`ExitPoints` is not published separately; it is included in **CSharpStyleChecker**.

### Migration from deprecated packages

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `CSharpStyleValidator` | `CSharpStyleChecker` | Rule IDs **CSV001–CSV008** → **CSC001–CSC008**; editorconfig `csc_max_line_length` |
| `CoverageGap.Tool` | `ExitPointGaps` | CLI: `dotnet tool run exitpointgaps` (was `coveragegap`) |
| `CoverageGapAnalysis` | `ExitPointGaps` | Analysis merged into the tool assembly — no `PackageReference` |

Version line restarts at **1.0.0** (not a continuation of 2.x). Full history: [CHANGELOG.md](CHANGELOG.md).

---

## Why this repository exists

Default chat behavior is fast but inconsistent across sessions. This setup enforces deterministic, auditable agent behavior.

- No implementation before plan approval.
- Explicit phases: plan, implement, review, review-loop, complex-task, council, commit-message.
- Review loops until zero Error findings (or explicit block).
- Warnings treated as defects.
- Token-efficient chat; high-quality plan, review, and council artifacts.

---

## Repository structure

| Path | Role |
|------|------|
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | **SSOT** — always-on policy, quality contract, tech-load protocol, terse communication |
| `custom_instructions.md` | Optional overlay at repo root — extra instructions to follow when the file exists |
| [`.github/skills/workflow-*.md`](.github/skills/) | **SSOT** — workflow stages (loaded on `/plan`, `/implement`, etc.) |
| [`.github/skills/tech-*.md`](.github/skills/) | **SSOT** — technology rules (loaded on scope trigger) |
| [`.github/prompts/`](.github/prompts/) | GitHub Copilot entry points (~10 lines); no stage duplication |
| [`.cursor/rules/copilot-ai-workflow.mdc`](.cursor/rules/copilot-ai-workflow.mdc) | Cursor always-on bootstrap (pointers only) |
| [`.cursor/commands/`](.cursor/commands/) | Cursor slash commands (`/plan`, `/implement`, `/review`, `/review-loop`, `/complex-task`, `/council`, `/commit-message`) |
| [`.cursor/skills/`](.cursor/skills/) | Cursor skill discovery wrappers → `.github/skills/` |
| [`AGENTS.md`](AGENTS.md) | Cross-tool SSOT map and integration overview |

---

## SSOT layering

Each concern has exactly one authoritative file under `.github/`. Copilot prompts and Cursor rules/commands/skills reference by path; they do not copy rules.

| Concern | SSOT |
|---------|------|
| Quality, tech triggers, templates, terse | `.github/copilot-instructions.md` |
| Optional repo overlay | `custom_instructions.md` (root; skip if missing) |
| Workflow stages and gates | `.github/skills/workflow-*.md` |
| Build, CPM, dependencies | `.github/skills/tech-solution.md` |
| C# / Rust / TUnit / Blazor / SourceGen | respective `.github/skills/tech-*.md` |
| Copilot user entry | `.github/prompts/` |
| Cursor user entry | `.cursor/commands/`, `.cursor/skills/` |

Entry points read `copilot-instructions.md`, `custom_instructions.md` when present, plus one workflow skill. No partial checklists.

---

## Configuration precedence

1. [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
2. `custom_instructions.md` when present (overlay; cannot weaken Section 4)
3. Skills loaded per Tech Load Protocol (Section 3) and workflow triggers (Section 6)

Cursor: [`.cursor/rules/copilot-ai-workflow.mdc`](.cursor/rules/copilot-ai-workflow.mdc) is a bootstrap only — it does not override SSOT content.

---

## On-Demand skills

| Skill | Load when |
|-------|-----------|
| `tech-csharp.md` | `*.cs` in scope |
| `tech-rust.md` | `*.rs` / `Cargo.toml` / `Cargo.lock` in scope |
| `tech-tunit.md` | C# tests; also C# production review (ExitPointGaps) |
| `tech-blazor.md` | `.razor` / `.razor.cs` / `.razor.css` in scope — exhaustive **bUnit** component tests |
| `tech-sourcegen.md` | Generator code in scope |
| `tech-solution.md` | Build files, `.csproj`, `GlobalUsings.cs` |
| `workflow-plan.md` | `/plan` |
| `workflow-implement.md` | `/implement`; Review Brief; Closing Exam (council Exam mode) |
| `workflow-review.md` | `/review`; Skeptic pass (parts + composition) |
| `workflow-review-loop.md` | `/review-loop` |
| `workflow-complex-task.md` | `/complex-task` |
| `workflow-council.md` | `/council`; Sweep on `/plan` and `/review`; Exam after `/implement` |
| `workflow-commit-message.md` | `/commit-message`; paste-ready English commit message (`feat`/`fix`/…) for a named scope |

Agents must `Read` matching skills before edits. Missing skill when trigger matches = Error in review.

---

## Workflow overview

Stages live in workflow skills only. Prompts do not repeat them.

- **Plan:** `workflow-plan.md` — gather context, requirements (user view), perspective sweep, Grill Me, decision loop, write artifact, coverage check (conversation → plan); close with Requirements fit
- **Implement:** `workflow-implement.md` — prepare, execute steps with review gates, tick Step Overview + Shared Block + Task Checklist together, verify against R{n}, write review brief, Closing Exam of the built result
- **Review:** `workflow-review.md` — scope, load, Skeptic pass (parts + composition), perspective sweep, cross-file consistency, output
- **Review-loop:** `workflow-review-loop.md` — review → remediate → re-review until clean (no plan required)
- **Complex-task:** `workflow-complex-task.md` — orchestrates plan → checkpoint → implement/review loop → one Closing Exam
- **Council:** `workflow-council.md` — five-view pressure-test (Skeptic, Problem-First, Upside, Outsider, Builder); Sweep in plan/review; Lite/Full on `/council`; Exam after implement
- **Commit-message:** `workflow-commit-message.md` — named scope required, Grill Me if missing, write `commit_message.md` (`feat`/`fix`/… prefix + user-perspective purpose/effect)

Review gates and checklist updates: `workflow-implement.md` Stage 2 and plan artifact Task Checklist.

---

## Cursor usage

Cursor picks up the workflow automatically when this repo (or a copy) is open:

| Mechanism | How to use |
|-----------|------------|
| Always-on rule | `.cursor/rules/copilot-ai-workflow.mdc` loads on every session |
| Slash commands | Type `/plan`, `/implement`, `/review`, `/review-loop`, `/complex-task`, `/council`, or `/commit-message` in chat |
| Agent skills | Skills auto-discover via descriptions; each points to `.github/skills/` SSOT |
| `AGENTS.md` | Overview and SSOT map for any agent reading project instructions |

Natural-language triggers (`plan this feature`, `review the PR`, `council this`, `commit message for staged`) work the same as slash commands per `copilot-instructions.md` Section 6.

---

## GitHub Copilot usage

Custom prompts in [`.github/prompts/`](.github/prompts/) mirror [`.cursor/commands/`](.cursor/commands/) — same three-line body, different tool format.

---

## Adopting the agent workflow in another repository

1. Copy [`.github/`](.github/) (instructions, skills, prompts).
2. Copy [`.cursor/`](.cursor/) and [`AGENTS.md`](AGENTS.md) for Cursor support.
3. Copy [`COPYRIGHT`](COPYRIGHT) and [`LICENSE`](LICENSE).
4. Validate paths after copy (`.github/` and `.cursor/` references are relative to repo root).
5. Optionally add `custom_instructions.md` at the repo root for extra instructions. Do not create it unless you have overlay rules.

---

## Solution components

| Project | Role |
|---------|------|
| [`CSharpStyleChecker`](src/CSharpStyleChecker/) | Roslyn analyzer NuGet (CSC001–CSC008) |
| [`ExitPoints`](src/ExitPoints/) | Roslyn exit-point collection (bundled in analyzer package) |
| [`ExitPointGaps`](src/ExitPointGaps/) | CLI + analysis — `dotnet tool run exitpointgaps` (`run` / `plan`; lists exit-point gaps) |
| [`samples/CSharpStyleChecker.Demo`](samples/CSharpStyleChecker.Demo/) | `netstandard2.0` analyzer demo (CSC*), source generator, packed NuGet verification |

---

## CSharpStyleChecker

Roslyn analyzer enforcing CopilotAIWorkflow C# style as **compiler errors**. The analyzer assembly targets `netstandard2.0` and applies to SDK-style consumers from `netstandard2.0` through current .NET (including source generators). Requires a host SDK with Roslyn 4.14+ (see Requirements above).

| ID | Rule |
|----|------|
| CSC001 | Line length ≤ 160 (strings/comments masked) |
| CSC002 | No `var` |
| CSC003 | Private `_PascalCase` (exempt: private nested types, explicit interface impl, local functions) |
| CSC004 | No `.Result` / `.Wait()` on `Task` |
| CSC005 | `using` only in `GlobalUsings.cs` |
| CSC006 | At most one exit point per source line per callable; `?:`, switch, `??`, `??=` allowed when arms are on separate lines |
| CSC007 | `volatile` fields: plain read/write allowed; `++`/`--`/`+=` require `Interlocked` |
| CSC008 | Target-typed `new()` and `[]` (exempt: interface/base targets, polymorphic construction, ctor args, throw, non-collection-expression targets such as `ReadOnlyMemory<T>`) |

XML documentation on public APIs is enforced by built-in **CS1591** when `GenerateDocumentationFile` and `TreatWarningsAsErrors` are enabled.

**Optional configuration**

| Setting | Purpose |
|---------|---------|
| `build_property.csc_max_line_length = N` in `.editorconfig` | Override CSC001 line length (default 160) |

**Build from source**

```bash
dotnet build CopilotAIWorkflow.slnx -c Release
```

Release build writes `.nupkg` files to `artifacts/` for packable projects (`ExitPointGaps`, `CSharpStyleChecker`).

---

## ExitPointGaps

CLI options (commands and workflow: [§2](#2--exit-point-coverage-gate-cli) · [tech-tunit.md](.github/skills/tech-tunit.md)).

| Option | Purpose |
|--------|---------|
| `--repo-root <path>` | Repository root (default: current directory) |
| `--configuration <cfg>` | Build/test configuration (default: `Release`) |
| `--format agent\|compact\|text` | Report shape (`plan` supports `agent` / `text`) |
| `-o <path>` | Output file or directory (relative paths under `--work-dir`) |
| `--work-dir <path>` | Isolated run directory (default: unique temp folder per invocation) |
| `--test-project <path>` | Override paired test project (single `project` target only) |
| `--cobertura <file>` | Skip test run; use existing Cobertura (`run` only) |
| `--include-snippet` | Add source snippets to gap entries |
| `--no-build` | Skip `dotnet build` before test/compile |
| `--skip-no-tests` | On `run` only: skip production projects without a paired test project |
| `--no-fail` | Exit `0` even when the gate fails |
| `--keep-work-dir` | Retain work directory after completion |
| `--max-parallelism <n>` | Cap per-project parallelism (default: all projects in scope) |
| `--stream` | Emit NDJSON project lines to stdout (multi-project) |
| `--no-stream` | Disable default stdout streaming |

### Performance

Multi-project runs are parallel by default (all projects in scope concurrently). On this repository (`CopilotAIWorkflow.slnx`, Release, `--no-build`): serial ~22.8s vs parallel ~12.0s (~47% faster).

| Technique | Effect |
|-----------|--------|
| Default parallelism | All scoped projects run concurrently |
| `--max-parallelism <n>` | Cap concurrency on memory-constrained machines |
| Solution pre-build + `--no-build` | Skip repeated builds across projects |
| Directory `-o reports/` | `summary.json` (schema v3) + per-project `{Name}.json` |
| `--stream` / `--no-stream` | NDJSON stdout for multi-project runs without `-o` |

Aggregated output uses **schema v3** (`projects[].reportFile` indirection). Load each report file for `exitGaps[]`.

---

## License

Copyright © 2026 DevAM. Licensed under MIT. See [LICENSE](LICENSE).

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history.
