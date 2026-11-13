# CopilotAIWorkflow

Structured AI agent workflow for **GitHub Copilot**, **Cursor**, and **Claude Code** (CLI and VS Code extension), plus NuGet packages for C# style enforcement, invariant string interpolation, exit-point coverage, and requirement traceability.

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![CSharpStyleChecker](https://img.shields.io/nuget/v/CSharpStyleChecker)](https://www.nuget.org/packages/CSharpStyleChecker/)
[![ExitPointGaps](https://img.shields.io/nuget/v/ExitPointGaps)](https://www.nuget.org/packages/ExitPointGaps/)
[![InvariantStringHelper](https://img.shields.io/nuget/v/InvariantStringHelper)](https://www.nuget.org/packages/InvariantStringHelper/)
[![ReqTrace](https://img.shields.io/nuget/v/ReqTrace)](https://www.nuget.org/packages/ReqTrace/)

**Current packages** — **CSharpStyleChecker** 2.0.0 (**breaking:** CSC001 defaults to 130 characters; reference `2.*`) · **ReqTrace** 0.2.0 (new: `reqtrace syntax` prints the spec form; fixes quote it) · **ExitPointGaps** 1.1.2 (package cleanup) · **InvariantStringHelper** 1.0.0. Each package releases on its own; histories: [CSharpStyleChecker](src/CSharpStyleChecker/CHANGELOG.md), [ReqTrace](src/ReqTrace/CHANGELOG.md), [ExitPointGaps](src/ExitPointGaps/CHANGELOG.md), [InvariantStringHelper](src/InvariantStringHelper/CHANGELOG.md). See also [Migration](#migration-from-deprecated-packages).

---

## Quickstart

Three packages on [nuget.org](https://www.nuget.org/packages?q=Owner%3ADevAM). No extra wiring: add **CSharpStyleChecker** or **InvariantStringHelper** as a `PackageReference`, or register **ExitPointGaps** as a local dotnet tool — `ExitPoints` ships inside **CSharpStyleChecker**; no second analyzer reference.

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
<PackageReference Include="CSharpStyleChecker" Version="2.*" />
```

Rebuild. Violations surface as compiler errors (CSC001–CSC009). No `PrivateAssets`, `IncludeAssets`, or separate `ExitPoints` package — the NuGet metadata handles that.

[Rule reference](#csharpstylechecker) · [Demo sample](samples/CSharpStyleChecker.Demo/README.md)

### 2 — Exit-point coverage gate (CLI)

**ExitPointGaps** lists exit-point gaps (`exitGaps[]`). Tests + Cobertura + exit-gap JSON in one call. **Gate:** `summary.exitGapCount == 0` (branch gaps informational). Analysis code lives inside the tool — no separate library package.

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
| [CSharpStyleChecker](https://www.nuget.org/packages/CSharpStyleChecker/) | `netstandard2.0`+ (SDK-style) | `PackageReference` | Roslyn style analyzer (CSC001–CSC009); bundles `ExitPoints` |
| [InvariantStringHelper](https://www.nuget.org/packages/InvariantStringHelper/) | `net6.0`+ | `PackageReference` | `Invariant($"…")`: invariant-culture interpolation that allocates only the result |
| [ExitPointGaps](https://www.nuget.org/packages/ExitPointGaps/) | N/A (CLI) | Local `dotnet tool install` | `dotnet tool run exitpointgaps` — lists exit-point gaps |
| [ReqTrace](https://www.nuget.org/packages/ReqTrace/) | N/A (CLI, any project language) | Local `dotnet tool install` | `dotnet tool run reqtrace check` — checks that each requirement reaches a passing test ([setup and use in the workflows](.github/skills/tech-reqtrace.md), [reference](docs/agents.md)) |

`ExitPoints` is not published separately; it is included in **CSharpStyleChecker**.

### Migration from deprecated packages

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `CSharpStyleValidator` | `CSharpStyleChecker` | Rule IDs **CSV001–CSV008** → **CSC001–CSC008**; editorconfig `csc_max_line_length` |
| `CoverageGap.Tool` | `ExitPointGaps` | CLI: `dotnet tool run exitpointgaps` (was `coveragegap`) |
| `CoverageGapAnalysis` | `ExitPointGaps` | Analysis merged into the tool assembly — no `PackageReference` |

Each package releases on its own version line, packs alone (`dotnet pack src/{Project}/{Project}.csproj -c Release` → `artifacts/release/`, after emptying the `bin/` output; [guide](docs/guides/release-a-package.md)), and ships its own `PACKAGE_README.md`. The lines restarted at **1.0.0** with the rename; CSharpStyleChecker **2.0.0** continues that new line, not the old `CSharpStyleValidator` 2.x. Package histories: [CSharpStyleChecker](src/CSharpStyleChecker/CHANGELOG.md), [ReqTrace](src/ReqTrace/CHANGELOG.md), [ExitPointGaps](src/ExitPointGaps/CHANGELOG.md), [InvariantStringHelper](src/InvariantStringHelper/CHANGELOG.md). Repo / workflow: [CHANGELOG.md](CHANGELOG.md).

---

## Why this repository exists

Default chat behavior is fast but inconsistent across sessions. This setup enforces deterministic, auditable agent behavior.

- Requirements-driven development: `/evolve` takes what the software must do and specifies, plans, builds, tests, verifies, and documents it. A person acts at three gates only, and receives short packets ranked by where attention pays off ([decision record](docs/decisions/requirements-driven-loop.md)).
- Agents decide details themselves and ask only about decisions that change a requirement, public API, format, security posture, limit, dependency, or architecture boundary; other work continues meanwhile (`copilot-instructions.md` Section 4.16).
- Requirements and test content live in a tracked `spec/` with stable IDs. `/dream` consolidates domain knowledge and repairs the trace between requirements, tests, and code.
- Used alone, `/implement` still requires an approved plan.
- Explicit phases: evolve, requirements, plan, implement, review, dream, council, commit-message, illustrate, docs-repair.
- Docs move with code: a small doc set (README, architecture with a one-screen Overview, concepts, domain, decision records); agents read in tiers before acting, so context stays small; the architecture is a contract; learned knowledge lands in its home in the same run; contract doc comments on every item with Related links by meaning or dependency (never usages); an Impact search before edits and a Sync check before anything is done ([`tech-docs.md`](.github/skills/tech-docs.md)).
- Every editing run ends with the same run report, also when it stops early: what is **not done** comes first, then decisions, problems, and deviations. Questions carry context and always allow a free-text answer ([`tech-reporting.md`](.github/skills/tech-reporting.md)).
- APIs cannot be used wrongly by accident and do not drift apart: misuse-prone shapes and state-dependent members are removed or justified, a new parallel path for an existing task is weighed in planning, and reviews find misuse-prone, redundant, or inconsistent APIs in existing code too (`copilot-instructions.md` Section 4.12).
- Review loops until zero Error findings (or explicit block).
- Warnings treated as defects.
- Plans lock the implementation (Before/After) so a weaker agent can execute them; reviews lock the fix the same way (`How` + Problem/Fix). Prefer snippets over prose in those artifacts; extra illustration code is welcome. Chat may use natural prose.

---

## Repository structure

| Path | Role |
|------|------|
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | **SSOT** — always-on policy, quality contract, tech-load protocol, communication |
| `custom_instructions.md` | Optional overlay at repo root — extra instructions to follow when the file exists |
| [`.github/skills/workflow-*.md`](.github/skills/) | **SSOT** — workflow stages (loaded on `/plan`, `/implement`, `/requirements`, …) |
| [`.github/skills/tech-*.md`](.github/skills/) | **SSOT** — technology rules (loaded on scope trigger) |
| [`.github/prompts/`](.github/prompts/) | GitHub Copilot entry points (~10 lines); no stage duplication |
| [`.cursor/rules/copilot-ai-workflow.mdc`](.cursor/rules/copilot-ai-workflow.mdc) | Cursor always-on bootstrap (pointers only) |
| [`.cursor/commands/`](.cursor/commands/) | Cursor slash commands (`/evolve`, `/dream`, `/requirements`, `/plan`, `/implement`, `/review`, `/council`, `/commit-message`, `/illustrate`, `/docs-repair`, `/port-dotnet-rust`, `/port-dotnet-rust-loop`) |
| [`.claude/skills/`](.claude/skills/) | The one skill pointer set → `.github/skills/`; read by Claude Code, Cursor, and VS Code (GitHub Copilot) |
| [`CLAUDE.md`](CLAUDE.md) | Claude Code bootstrap (imports the SSOT instructions; tool mapping) |
| [`AGENTS.md`](AGENTS.md) | Cross-tool SSOT map and integration overview |
| [`docs/architecture.md`](docs/architecture.md) | Components of this repository, where they are, and what is easy to miss when changing them |

---

## SSOT layering

Each concern has exactly one authoritative file under `.github/`. Copilot prompts, Cursor rules and commands, `CLAUDE.md`, and `.claude/skills/` reference by path; they do not copy rules.

| Concern | SSOT |
|---------|------|
| Quality, tech triggers, artifact fidelity | `.github/copilot-instructions.md` |
| Optional repo overlay | `custom_instructions.md` (root; skip if missing) |
| Workflow stages and gates | `.github/skills/workflow-*.md` |
| Build, CPM, dependencies | `.github/skills/tech-solution.md` |
| C# / Rust / Python / tests / Blazor / SourceGen | respective `.github/skills/tech-*.md` |
| Copilot user entry | `.github/prompts/` |
| Cursor user entry | `.cursor/commands/` |
| Claude Code user entry | `CLAUDE.md`, `/workflow-*` skills |
| Skill pointers (all tools) | `.claude/skills/` |

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
| `tech-csharp.md` | `*.cs` in scope — style, types, errors and logging, security, async, thread safety, allocation and performance |
| `tech-rust.md` | `*.rs` / `Cargo.toml` / `Cargo.lock` in scope |
| `tech-python.md` | `*.py` / `pyproject.toml` / `*.pyi` in scope — strict typing |
| `tech-csharp-profiling.md`, `tech-rust-profiling.md`, `tech-python-profiling.md` | Only when a measurement is needed — profiling without admin rights |
| `tech-test.md` | Tests or production APIs in scope — case design |
| `tech-tunit.md` | `*Tests.cs` / `{Project}.Tests` / `{App}.UiTest`; `global.json`; also C# production review |
| `tech-pytest.md` | pytest files in scope |
| `tech-rust-test.md` | Rust unit/integration tests |
| `tech-web.md` | Product web UI (plan time, even before HTML files) — responsive layout, locked breakpoints per device class, width policy, tables that become cards, overflow, icons and tooltips, CSS/JS placement, a11y |
| `tech-playwright.md` | Web UI (plan time, even before spec files) — test and debug; create-time illustration verify |
| `tech-blazor.md` | `.razor` / `.razor.cs` / `.razor.css` — Blazor mechanism on top of `tech-web.md` |
| `tech-sourcegen.md` | Generator code in scope |
| `tech-solution.md` | Build files, `.csproj`, `.editorconfig`, `GlobalUsings.cs`, packing or releasing a package, NuGet restore failures from vulnerable packages |
| `tech-dotnet-rust.md` | .NET → Rust port, `port/**` — Idiom Map, ownership, `Text`, optimizations |
| `tech-docs.md` | Code, comments, or docs in scope (every plan, implement, review) — doc set, reading tiers, architecture as contract, doc-comment contract, Related and `Sync:` references, comment tags, large-file sections, language contract, Impact search, Sync check |
| `tech-docs-templates.md` | Creating or restructuring a project doc; `/docs-repair` — README, architecture, concepts, domain, decision record, guide |
| `tech-spec.md` | Requirements, plan, implement, review, evolve, dream; `spec/**`, `docs/domain/**` — living spec format, stable IDs, trace, integrity checks, domain knowledge |
| `tech-reqtrace.md` | Requirements, plan, implement, review, evolve, dream; `spec/trace.lock`, `.config/dotnet-tools.json` — setting up and calling `reqtrace`: which command at which moment, what to do per finding |
| `tech-reporting.md` | Before a blocker, decision card, Spec delta, briefing, or run report — reader-first forms and length budgets |
| `tech-agent-access.md` | An application's UI features, CLI, REST, MCP, or library API — how agents use the product |
| `tech-sweep.md` | `/plan`, `/review`, `/council` — the five named views |
| `tech-limits.md` | A trust-boundary limit in scope (plan, review) — principles, mechanism, project decisions, audit and register, reference implementations |
| `tech-instructions.md` | Instructions, skills, prompts, or pointer files in scope — authoring rules, compaction without loss, workflow-kit change coupling |
| `workflow-docs-repair.md` | `/docs-repair`; realign docs with code in an existing project; decision cards; resumable ledger |
| `workflow-port-dotnet-rust.md` | `/port-dotnet-rust`; intake, alignment into `port/MANIFEST.md`, closing audit |
| `workflow-port-dotnet-rust-loop.md` | `/port-dotnet-rust-loop`; autonomous unit loop with balanced books |
| `workflow-evolve.md` | `/evolve`; continuous requirements-driven development with three human gates |
| `workflow-dream.md` | `/dream`; consolidate and repair domain knowledge, requirements, test content, and the trace |
| `workflow-requirements.md` | `/requirements`; atomic `REQ-<AREA>-<TOPIC>` entries in `spec/`; also before `/plan` when none exist |
| `workflow-plan.md`, `workflow-plan-templates.md` | `/plan`; the templates load at the writing stage |
| `workflow-implement.md` | `/implement`; docs per step; briefing; Closing Review |
| `workflow-review.md`, `workflow-review-templates.md` | `/review`; file mode default; `chat only` opt-in; public-release verdict; the templates load at the output stage |
| `workflow-council.md` | `/council`; Sweep on `/plan` (five views); `/review` uses Skeptic + Outsider only; Exam on request |
| `workflow-commit-message.md` | `/commit-message`; complete message; git after the file is out of scope |
| `workflow-illustrate.md` | `/illustrate`; didactic HTML zip; ask before vendoring libraries into `vendor/` |

Agents must read matching skills before edits. Missing skill when trigger matches = Error in review.

Always-on policy: [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
(terms in §5.1). Skills load only on trigger. Do not record instruction line
counts here.

`spec/` and `docs/domain/` are tracked. `plans/`, `reviews/`, `councils/`, `illustrations/`, and `docs-repair/` are git-ignored workflow history. Existing files there are historical unless the user names one. Follow the matching workflow-skill templates, not those artifacts.

---

## Workflow overview

Stages live in workflow skills only. Prompts do not repeat them.

- **Evolve:** `workflow-evolve.md` — dictated input → Spec delta → **Gate A** → one slice at a time: plan, tests first, implement, prove as a user → Heal → **Gate B**. Decisions wait as cards (**Gate E**) while other work continues. Work is sized S, M, or L. State resumes from `spec/state.md`. Where the tool can start a subagent on another model, a cheaper Builder writes each plan step's tests and code, and the session model checks it ([decision record](docs/decisions/evolve-builder.md))
- **Dream:** `workflow-dream.md` — folds accumulated knowledge back into one consistent state: integrity checks, domain inbox and pages, requirements, test content, trace, docs. `touched` after each slice, `full` on request. Never changes production code
- **Requirements:** `workflow-requirements.md` — atomic, contradiction-free requirements (`REQ-<AREA>-<TOPIC>`) kept in `spec/`, checked against the existing spec; Full or Delta mode; context prose has no IDs; skip reinventing when the user already supplied them
- **Plan:** `workflow-plan.md` — Interactive, or Autonomous inside `/evolve`; attached docs, sweep, Grill Me on Decision-class points only, locked `How` + Before/After, schematic API and interface snippets immediately before the steps, links to requirements and `TEST-<AREA>-<TOPIC>` cards, step Experience/Acceptance, an **Expected Experience** section right after Step Overview, an Impact search and a required `Docs` field per step, then a requirements walk **and** a conversation walk, Requirements-fit
- **Implement:** `workflow-implement.md` — read the plan in full, execute steps
  and `TEST-<AREA>-<TOPIC>` content, update the step’s docs and run the Sync
  check, verify requirements at step close (tick `Met` in the linked
  requirements file), Step `{n}R` on the step’s change, tick accepted finding
  Status in the review Summary together with the finding Status line,
  a **briefing** ranked by where attention pays off (concept, type roles, data
  flow, a card per Judge and Sample file), one full-scope
  **Closing Review** (Exam only on request), and a **run report** in chat on
  every exit (not done, needs you, problems, deviations)
- **Review:** `workflow-review.md` — skeptical auditor (Skeptic + Outsider);
  public-release verdict for the **scope**; Summary Findings table has a
  Status column (`⬜` at write time; `/implement` ticks it) and links each
  finding ID to its block, plus a
  hunt-results table (Docs code and Docs project are separate hunts); ignore prior `reviews/review_*.md`
  unless named; Section 3 skill load; locked finding `How` with Problem/Fix
  before/after code (extra illustration snippets welcome); Section 4.6 links;
  **default** runs tests and writes `reviews/review_<slug>_<n>.md`; static on
  explicit reduced scope; `chat only` / `ohne Review-Datei` opt-in
- **Council:** `workflow-council.md` — five views in the **same agent** (no subagents); Sweep in `/plan`; Lite/Full on `/council`; Exam on request after the Closing Review
- **Docs-repair:** `workflow-docs-repair.md` — existing projects: inventory docs and code units, audit each unit’s claims against code and tests, fix doc-side drift when evidence decides, ask with self-contained `DRIFT-<AREA>-<TOPIC>` decision cards when it does not, hand code-side fixes to `/implement` as findings, backfill doc comments with Related links, the architecture Overview and component sections, concepts, and the domain doc; resumable ledger in `docs-repair/<slug>.md` (git-ignored; durable answers move to `docs/`)
- **Commit-message:** `workflow-commit-message.md` — named scope, complete specific message (names, before/after, examples) in `commit_message.md`; `CHANGELOG.md` when the scope is a release; git after the file is out of scope
- **Illustrate:** `workflow-illustrate.md` — teaching design and storyboard first; every motion shows a real state change of the subject; teaching checks at create time; HTML zip, no build; asks before
  vendoring allowlisted libraries (download into `vendor/`, relative paths,
  offline); Playwright verifies at create time only

Review gates and checklist updates: `workflow-implement.md` Stage 2 and plan artifact Task Checklist.

---

## Cursor usage

Cursor picks up the workflow automatically when this repo (or a copy) is open:

| Mechanism | How to use |
|-----------|------------|
| Always-on rule | `.cursor/rules/copilot-ai-workflow.mdc` loads on every session |
| Slash commands | Type `/evolve`, `/dream`, `/requirements`, `/plan`, `/implement`, `/review`, `/council`, `/commit-message`, `/illustrate`, `/docs-repair`, `/port-dotnet-rust`, or `/port-dotnet-rust-loop` in chat |
| Agent skills | Cursor loads `.claude/skills/` for compatibility; each points to `.github/skills/` SSOT. Do not add `.cursor/skills/` — Cursor would list every skill twice |
| `AGENTS.md` | Overview and SSOT map for any agent reading project instructions |

Natural-language triggers (`plan this feature`, `capture requirements`, `review the PR`, `council this`, `commit message for staged`, `illustrate this API`, `repair the docs`) work the same as slash commands per `copilot-instructions.md` Section 6.

---

## Claude Code usage (CLI and VS Code extension)

| Mechanism | How to use |
|-----------|------------|
| Bootstrap | `CLAUDE.md` imports `.github/copilot-instructions.md` on every session |
| Workflows | Type `/workflow-plan`, `/workflow-implement`, `/workflow-review`, … followed by the argument. `/plan` and `/review` are built-in Claude Code commands, not these workflows |
| Skill list | `/skills`, or type `/` for the menu |
| Attach files (VS Code extension) | Hold `Shift` while dragging files into the prompt box, type `@` plus a file name, or press `Alt+K` to insert the current selection |

---

## GitHub Copilot usage

Custom prompts in [`.github/prompts/`](.github/prompts/) mirror [`.cursor/commands/`](.cursor/commands/) — same three-line body, different tool format. VS Code also loads the skill pointers from `.claude/skills/`.

---

## Adopting the agent workflow in another repository

1. Copy [`.github/`](.github/) (instructions, skills, prompts).
2. Copy [`.cursor/`](.cursor/), [`.claude/`](.claude/), [`AGENTS.md`](AGENTS.md), and [`CLAUDE.md`](CLAUDE.md) for Cursor and Claude Code support.
3. Copy [`COPYRIGHT`](COPYRIGHT) and [`LICENSE`](LICENSE).
4. Validate paths after copy (`.github/`, `.cursor/`, and `.claude/` references are relative to repo root).
5. Optionally add `custom_instructions.md` at the repo root for extra instructions. Do not create it unless you have overlay rules.

---

## Solution components

| Project | Role |
|---------|------|
| [`CSharpStyleChecker`](src/CSharpStyleChecker/) | Roslyn analyzer NuGet (CSC001–CSC009) |
| [`ExitPoints`](src/ExitPoints/) | Roslyn exit-point collection (bundled in analyzer package) |
| [`InvariantStringHelper`](src/InvariantStringHelper/) | NuGet library: `Invariant($"…")` for .NET 6+ |
| [`ExitPointGaps`](src/ExitPointGaps/) | CLI + analysis — `dotnet tool run exitpointgaps` (`run` / `plan`; lists exit-point gaps) |
| [`ReqTrace`](src/ReqTrace/) | CLI — checks the links between requirements, test cards, and tests ([usage](docs/agents.md)); dotnet tool `reqtrace` |
| [`samples/CSharpStyleChecker.Demo`](samples/CSharpStyleChecker.Demo/) | `netstandard2.0` analyzer demo (CSC*), source generator, packed NuGet verification |

---

## CSharpStyleChecker

Roslyn analyzer enforcing CopilotAIWorkflow C# style as **compiler errors**. The analyzer assembly targets `netstandard2.0` and applies to SDK-style consumers from `netstandard2.0` through current .NET (including source generators). Requires a host SDK with Roslyn 4.14+ (see Requirements above).

| ID | Rule |
|----|------|
| CSC001 | Line length ≤ 130 (string/character literal and raw-string content masked; comments and `$"..."` / `@$"..."` holes count) |
| CSC002 | No `var` |
| CSC003 | Private `_PascalCase` (exempt: private nested types, explicit interface impl, local functions) |
| CSC004 | No `.Result` / `.Wait()` / `GetAwaiter().GetResult()` on `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>`; no `Task.WaitAll` / `Task.WaitAny` |
| CSC005 | `using` only in `GlobalUsings.cs` |
| CSC006 | At most one exit point per source line per callable; `?:`, switch, `??`, `??=` allowed when arms are on separate lines. Covers methods, constructors, destructors, operators, conversions, accessors, expression-bodied properties and indexers, and local functions. Does not cover lambdas or anonymous methods. |
| CSC007 | `volatile` fields: plain read/write allowed; `++`/`--`/`+=` require `Interlocked` |
| CSC008 | Target-typed `new()` and `[]` (exempt: interface/base targets, polymorphic construction, ctor args, throw, non-collection-expression targets such as `ReadOnlyMemory<T>`) |
| CSC009 | Interpolated strings must use `Invariant($"…")` from the InvariantStringHelper package, `string.Create`, or `StringBuilder.Append` with `CultureInfo.InvariantCulture`; `FormattableString.Invariant` is an error where `string.Create` binds (.NET 6+) and allowed on `netstandard2.0`; culture-sensitive `ToString` must pass `CultureInfo.InvariantCulture` |

XML documentation on public APIs is enforced by built-in **CS1591** when `GenerateDocumentationFile` and `TreatWarningsAsErrors` are enabled.

**Optional configuration**

| Setting | Purpose |
|---------|---------|
| `build_property.csc_max_line_length = N` in `.editorconfig` | Override CSC001 line length (default 130) |

**Build from source**

```bash
dotnet build CopilotAIWorkflow.slnx -c Release
```

A build writes no package. `dotnet pack src/{Project}/{Project}.csproj -c Release` writes `artifacts/release/{Project}.{version}.nupkg` for `CSharpStyleChecker`, `InvariantStringHelper`, `ExitPointGaps`, and `ReqTrace` ([guide](docs/guides/release-a-package.md)).

---

## InvariantStringHelper

```xml
<PackageReference Include="InvariantStringHelper" Version="1.*" />
```

```csharp
global using static InvariantStringHelper.InvariantText;

string message = Invariant($"Rejection rate: {rate:P1}, count={count}");
```

Formats under the invariant culture and allocates only the result string. Requires `net6.0` or later; `netstandard2.0` projects keep `FormattableString.Invariant`. Together with **CSharpStyleChecker** use 1.2.0 or later: 1.1.0 reports the call as CSC009. Why this form and not the alternatives: [decision record](docs/decisions/invariant-interpolation.md). Package history: [InvariantStringHelper CHANGELOG](src/InvariantStringHelper/CHANGELOG.md).

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

See [CHANGELOG.md](CHANGELOG.md) (repo / workflow), [CSharpStyleChecker CHANGELOG](src/CSharpStyleChecker/CHANGELOG.md), and [ExitPointGaps CHANGELOG](src/ExitPointGaps/CHANGELOG.md).
