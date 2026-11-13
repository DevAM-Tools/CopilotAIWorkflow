# Architecture — CopilotAIWorkflow

## Overview

An agent-workflow kit (rules, workflows, tech skills for GitHub Copilot, Cursor, and Claude Code) plus .NET projects that enforce parts of those rules in code. What the repository is: [README](../README.md).

| Component | Folder · entry | Responsibility | May depend on |
|-----------|----------------|----------------|---------------|
| [Workflow kit](#workflow-kit) | [`.github/`](../.github/) · `copilot-instructions.md` | Always-on rules, workflow skills, tech skills | — |
| [Tool pointers](#tool-pointers) | [`.claude/`](../.claude/), [`.cursor/`](../.cursor/), [`.github/prompts/`](../.github/prompts/) | Entry points per tool; no rules | Workflow kit |
| [CSharpStyleChecker](#csharpstylechecker) | [`src/CSharpStyleChecker/`](../src/CSharpStyleChecker/) · one `DiagnosticAnalyzer` per rule | Roslyn analyzers CSC001–CSC009 | ExitPoints (bundled) |
| [ExitPoints](#exitpoints) | [`src/ExitPoints/`](../src/ExitPoints/) · `ExitPointCollector` | Collects the exit points of every callable | — |
| [InvariantStringHelper](#invariantstringhelper) | [`src/InvariantStringHelper/`](../src/InvariantStringHelper/) · `InvariantText.Invariant` | Invariant-culture string interpolation for .NET 6+ (NuGet) | — |
| [ExitPointGaps](#exitpointgaps) | [`src/ExitPointGaps/`](../src/ExitPointGaps/) · `Program.Main` | dotnet tool: exit points no test reached | ExitPoints, InvariantStringHelper |
| [ReqTrace](#reqtrace) | [`src/ReqTrace/`](../src/ReqTrace/) · `Program.Main` | dotnet tool: checks the links between requirements, test cards, and tests | InvariantStringHelper |

A dependency not listed in **May depend on** is a defect or needs a decision record. Build settings: [`Directory.Build.props`](../Directory.Build.props), [`Directory.Build.targets`](../Directory.Build.targets) (applies the analyzer in this repository; makes each project with a `PackageId` a package of its own, packed only by `dotnet pack -c Release` into `artifacts/release/` after emptying `bin/`: [decision record](decisions/release-pack-path.md)), [`Directory.Packages.props`](../Directory.Packages.props), [`global.json`](../global.json), [`.editorconfig`](../.editorconfig).

**Easy to miss:** a package (CSharpStyleChecker, InvariantStringHelper, ExitPointGaps, ReqTrace) → its `VersionPrefix` in its project file · its `PACKAGE_README.md` · its `CHANGELOG.md` · the root CHANGELOG package table · README § NuGet packages. Release steps: [guide](guides/release-a-package.md).

## Workflow kit

[`copilot-instructions.md`](../.github/copilot-instructions.md) is always loaded: quality contract, the Tech Load Protocol (§3), and the workflow router (§6). `workflow-*.md` skills hold stages; `*-templates.md` companions load at the writing stage; `tech-*.md` skills hold technology rules and load on a scope trigger. Authoring rules: [`tech-instructions.md`](../.github/skills/tech-instructions.md).

[`workflow-evolve.md`](../.github/skills/workflow-evolve.md) composes the other workflows into a requirements-driven loop with three human gates. Its session is the Lead; where the tool allows, a Builder subagent on a cheaper model builds plan steps ([decision record](decisions/evolve-builder.md)). [`tech-spec.md`](../.github/skills/tech-spec.md) defines the `spec/` format that a product using the kit keeps; this repository's [`spec/`](../spec/index.md) covers ReqTrace and a pilot on ExitPointGaps. Why this shape: [decision record](decisions/requirements-driven-loop.md).

**Invariants:**
- One home per rule. A rule stated in two files is a defect; the other file points to the home.
- `copilot-instructions.md` keeps its section numbers; skills cite them.
- A workflow names each skill it needs in the stage that uses it.

**Easy to miss:** adding, renaming, or removing a workflow or tech skill, a description, a stage name, a term, or a Shared Block field touches pointer files and README tables that nothing checks. The full list is the change-coupling table in [`tech-instructions.md`](../.github/skills/tech-instructions.md).

## Tool pointers

[`.claude/skills/`](../.claude/skills/) is the one skill pointer set, read by Claude Code, Cursor, and VS Code. [`.cursor/commands/`](../.cursor/commands/), [`.cursor/rules/copilot-ai-workflow.mdc`](../.cursor/rules/copilot-ai-workflow.mdc), [`.github/prompts/`](../.github/prompts/), [`CLAUDE.md`](../CLAUDE.md), and [`AGENTS.md`](../AGENTS.md) are entry points.

**Invariants:**
- A pointer holds name, description, and a read list; never a rule.

**Easy to miss:** a pointer `description` repeats the skill’s purpose in prose; it must change when the skill’s purpose changes.

## CSharpStyleChecker

Turns the C# style rules of [`tech-csharp.md`](../.github/skills/tech-csharp.md) into compiler errors. `DiagnosticIds` holds the rule IDs; `DiagnosticDescriptors` holds titles, messages, and severities. `LineMasking` masks literals for CSC001. CSC006 uses ExitPoints, which ships inside the package. Tests: [`src/CSharpStyleChecker.Tests/`](../src/CSharpStyleChecker.Tests/), one `{Analyzer}Tests` class per analyzer. Demo: [`samples/CSharpStyleChecker.Demo/`](../samples/CSharpStyleChecker.Demo/), one violation file per rule. The tests reference [InvariantStringHelper](#invariantstringhelper) only to pin the names the analyzer looks up.

**Easy to miss:** a new or changed rule → `DiagnosticIds` · `DiagnosticDescriptors` · the analyzer · its tests · [`AnalyzerReleases.Unshipped.md`](../src/CSharpStyleChecker/AnalyzerReleases.Unshipped.md) · README rule table · demo `Violations/` file and demo README table · `tech-csharp.md` lint list · package CHANGELOG. Docs and samples are not compiled against the rule list. CSC009 reports `FormattableString.Invariant` only when the compilation binds `string.Create` with an interpolated string handler, so its result depends on the target framework: [decision record](decisions/invariant-interpolation.md). CSC009 recognizes the [InvariantStringHelper](#invariantstringhelper) helper by metadata name.

## ExitPoints

Collects `return`, `throw`, `yield break`, implicit end, and expression arms per callable (`ExitKind`). `ExitPointCollector` is the entry; `CompletionWalker` walks callable bodies and completion expressions; `ExitPointExclusion` detects syntax excluded from code coverage. Tests: [`src/ExitPoints.Tests/`](../src/ExitPoints.Tests/).

**Invariants:**
- No dependency on another project of this repository; CSharpStyleChecker bundles it into its analyzer package.

**Easy to miss:** none.

## InvariantStringHelper

`Invariant($"…")` formats an interpolated string under the invariant culture. `InvariantText.Invariant` takes an `InvariantInterpolatedStringHandler`, which wraps `DefaultInterpolatedStringHandler` created with `CultureInfo.InvariantCulture`; the result string is the only allocation. Targets `net6.0` and `net10.0`. `netstandard2.0` is not supported (no `DefaultInterpolatedStringHandler`); the API allows a later implementation. Tests: [`src/InvariantStringHelper.Tests/`](../src/InvariantStringHelper.Tests/). Decision: [invariant-interpolation](decisions/invariant-interpolation.md).

**Invariants:**
- No package dependency and no dependency on another project of this repository.
- No shared state.

**Easy to miss:** CSC009 finds the helper by the metadata names `InvariantStringHelper.InvariantText` and `InvariantStringHelper.InvariantInterpolatedStringHandler` and the method name `Invariant`. A rename changes `InvariantCultureFormattingAnalyzer`, its pin test, [`tech-csharp.md`](../.github/skills/tech-csharp.md), and [`tech-solution.md`](../.github/skills/tech-solution.md).

## ExitPointGaps

A dotnet tool. `run` builds and tests with coverage and lists exit points no test reached; `plan` lists exit points without a test run. `Program.Main` → `CliParser` → `RunCommand` / `PlanCommand`; `GapRunOrchestrator` fans out per project; `ExitPointGapReportBuilder` joins exit points and Cobertura; `AggregatedGapReportSerializer` writes the summary. Tests: [`src/ExitPointGaps.Tests/`](../src/ExitPointGaps.Tests/).

**Easy to miss:**
- A CLI option → `CliOptions` · `CliParser` · `CliUsage` · README options table · package CHANGELOG. Usage text and README are strings.
- The aggregated report schema → `AggregatedGapReport` · `AggregatedGapReportSerializer` · README schema version · package CHANGELOG. Consumers parse JSON, not types.

## ReqTrace

A command-line tool that reads `spec/`, test markers, and test result files, and compares them with the state a person accepted. `Program.Main` → `TraceApp` → `TraceChecker`, which runs the layers in order: `SnapshotReader` (`SpecReader`, `MarkerScanner`) → `LinkChecker` → `ChangeChecker` → `EvidenceChecker`. `FindingWriter` and `ReportWriter` print. `SyntaxWriter` prints the forms of `SpecForm`; `syntax` runs before the repository search, so it needs no `spec/`. How agents call it: [`docs/agents.md`](agents.md). Why it exists: [decision record](decisions/spec-trace.md). Tests: [`src/ReqTrace.Tests/`](../src/ReqTrace.Tests/).

**Invariants:**
- An unreadable line is never skipped silently.
- Every finding comes from the `Rules` table, so it has a location, message, and fix.
- Only `accept` and `confirm` write, and only `spec/trace.lock`.
- A gap in an unchanged entry never fails the run.
- No regular expressions: start-up time counts on every agent call. One framework-dependent build runs on every platform.

**Easy to miss:**
- The spec format lives in [`tech-spec.md`](../.github/skills/tech-spec.md); in code its separators, value sets, and examples live in `SpecForm`, which `SpecReader` accepts, `syntax` prints, and form fixes quote. The compiler connects neither to `tech-spec.md`. Marker forms: `MarkerScanner` ↔ `SpecForm` ↔ `tech-spec.md` § Trace.
- A rule → `Rules` · [`docs/agents.md`](agents.md) § Rules · [`tech-reqtrace.md`](../.github/skills/tech-reqtrace.md) § Findings. A command, option, or output line → `CliOptions`, `FindingWriter`, `ReportWriter`, `SyntaxWriter` · `docs/agents.md` § Commands · `tech-reqtrace.md`.
