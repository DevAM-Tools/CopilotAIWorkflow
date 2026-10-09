# Changelog

All notable changes to this project are documented in this file.

## 1.1.0

Package release notes (full detail):

| Package | Changelog |
|---------|-----------|
| CSharpStyleChecker | [src/CSharpStyleChecker/CHANGELOG.md](src/CSharpStyleChecker/CHANGELOG.md) |
| ExitPointGaps | [src/ExitPointGaps/CHANGELOG.md](src/ExitPointGaps/CHANGELOG.md) |

### CSharpStyleChecker

- **CSC009** (new): bare interpolated strings and culture-sensitive `ToString` require invariant culture (`Invariant($"…")`, `string.Create` / `StringBuilder.Append` with `CultureInfo.InvariantCulture`).
- **CSC001–CSC008** coverage and masking fixes (verbatim/raw string line length, more `var` sites, `ValueTask` / `WaitAll` / `WaitAny`, indexers, `[ExcludeFromCodeCoverage]` no longer waives CSC006).
- Repo clone: no nested MSBuild of the checker per `CoreCompile`; `ExitPoints.dll` wired as analyzer dependency after project references resolve.

### ExitPointGaps

- Builds clean under CSC009 (`Invariant($"…")`).
- Exit-point collection: `ExitPointEntry` is a `readonly record struct`; `Collect` takes optional `CancellationToken`; ternary groups use `QuestionToken`.
- CLI surface unchanged (`run` / `plan`, schema v3).

### Agent workflow

- Plan test content IDs are `TEST-<AREA>-<TOPIC>`. The ID names the case.
  A case that proves a requirement reuses that requirement’s area and, when
  it is that one claim, its topic (`REQ-EXPORT-NO-DUPLICATE` →
  `TEST-EXPORT-NO-DUPLICATE`).
- `/plan` writes **API and interfaces** immediately before the steps: schematic snippets of the public API and of interfaces, traits, or equivalent contract types.
- `/review` Summary is structured: release line, counts, a findings table, a checks table (one row per hunt), and a priority list.
- `/review` Summary **Findings** has a Status column (Section 5 legend). A new
  review starts each row at `⬜`. `/implement` sets that cell and the finding
  Status line together (`⚠️` before the edit, `✅` after Verify). **Release**
  and **Counts** stay the review-time verdict. A deferred finding stays `⬜`.
- `/requirements` IDs are `REQ-<AREA>-<TOPIC>` (the ID names the subject). The artifact is an atomic, contradiction-free list. Headings, context, and notes do not get IDs.
- Plan and review artifacts welcome extra illustrative code. Plans prefer snippets over a prose-only `How` or Target Solution. Review findings keep fenced Problem/Fix (before/after) as the primary handoff.
- Section 4.6: file references in plans, briefings, reviews (and other non-temp docs) are clickable Markdown links with a relative target that opens. Bare backticks and absolute paths are not enough.
- Chat handover is not global. Each workflow skill’s Completion (or Output Modes) defines the ending.
- `/review-loop` default is full clean: remediate every bucket, then stop only when the latest review has zero open findings (or explicit deferrals). `/complex-task` still defers Cosmetic, Refactoring, and Performance so the plan can finish and Exam can run.
- `/review` file mode is the default for standalone `/review` as well as
  `/review-loop` and `/complex-task`. Chat-only only when the user says
  `chat only` / `ohne Review-Datei`.
- `/illustrate`: if the user allows libraries, download pinned allowlist files
  into `vendor/` at create time. Shipped HTML uses relative paths only and works
  offline (no CDN). Playwright verifies during create; it is not part of the
  illustration. Illustrations are not product web UIs: not responsive-by-default,
  and `tech-web.md` does not apply.
- README does not publish instruction line counts (they go stale). Terms (Grill Me,
  Step `{n}R`, Shared Block, MTP, …) live in `copilot-instructions.md` §5.1.
- `/review` Perspective Sweep is two views (Skeptic, Outsider). Plan Sweep stays
  five views. Do not paste the plan fence into a review file.
- Tool commands that set env vars show Unix (`NAME=value`) and PowerShell
  (`$env:NAME = "value"`). Illustration Verify uses the stack CLI in
  `tech-playwright.md`, not a `pnpm`-only fence.
- `tech-rust.md` UTF-8 console sample has `// SAFETY:` on the `unsafe` block.
- `tech-blazor.md` `[EditorRequired]` sample uses `required string` with no
  empty-string default.
- `/commit-message` writes a complete English message to `commit_message.md` and stops. Git commit/push after that file is out of scope.
- `/review` is a skeptical auditor new to the project: trust nothing, hunt the hair in the soup, check with absolute precision. Findings lock `How` plus Problem/Fix for a weaker agent. Ignore existing `reviews/review_*.md` unless the user names one. Load skills from the Section 3 table for the confirmed scope. The review **must** answer whether that scope is ready for public release (`Ready for public release` or `Blocked by {IDs}`). Leading criteria: `custom_instructions.md` when present, loaded skills, Section 4. Compact always-on distillate (security, races, docs, orphans). No second closer.
- C# UI tests live in `{App}.UiTest` (TUnit + `TUnit.Playwright`), not `{App}.E2E`. Playwright asserts named UI requirements, not smoke-only.
- Design apps and libraries so an agent can debug them (smallest command, filterable tests, traces).
- C# tests (unit, bUnit, Playwright UI) are TUnit only (`TUnit.Playwright` in `{App}.UiTest`). Playwright, web, Blazor, and test skills no longer duplicate journey/viewport/runner rules.
- Try* APIs surface failure as code, message, or type (`out` preferred; documented TLS last-error only when `out` cannot be added). Prefer auto-properties; explicit `volatile` fields; least visibility and least setter.
- Profile C# hot paths with `dotnet-trace` (Release only) when the user asks or the step is hot-path optimization. GC counts belong in that session, not production.
- Section 6 is the workflow router (load skill; `/implement` after plan approval). Stages stay in the skill.
- 100% exit-path coverage is a C# / TUnit gate (`tech-csharp.md`, `tech-tunit.md`), not an always-on rule.
- Trust-boundary limits (size, count, range, time, rate) must be agreed with the user; record constant vs configurable.
- `/requirements` records required behavior and expected properties (shall / shall not / shall exhibit). Tests in plans are `TEST-<AREA>-<TOPIC>`. Implementation stays in `/plan`. `/plan` links an existing requirements file; otherwise it runs requirements first.
- Plans lock implementation (`How` + Before/After) for a weaker executing agent, with first-class test **content** (`TEST-<AREA>-<TOPIC>` cards: scenarios, edges, constellations, contradictions, gaps), step experience, and public-API snippets. Class and Layer may be named. Test code in the plan is allowed; it does not replace Content. Extra tests at implement time are allowed.
- Plans **link** the requirements file (real Markdown path) and do not copy the requirement list. Tick `Met` in that file.
- Requirements are verified at step close (Done-when runs). IDs stay out of product code.
- Council runs in the same agent. No subagent advisors or peer-review round.
- Section 3 Tech Load maps `GlobalUsings.cs` to `tech-csharp.md` + `tech-solution.md`, `.editorconfig` to `tech-solution.md`, `global.json` to `tech-tunit.md`, and C# tests as `*Tests.cs` in `{Project}.Tests/` (not `*.Tests.cs`). C# copyright syntax lives in `tech-csharp.md` so it applies when only `.cs` files are in scope.
- `/commit-message` writes a specific message (named outcomes, before/after, examples), not a slogan. Updates `CHANGELOG.md` when the scope is a release.
- Briefings (not “review briefs”) are the post-implement packet (try-this, acceptance evidence, walkthrough, per-file Look at).
- Shared product web UI rules live in `tech-web.md` (phone-first layout, locked breakpoints, CSS/JS placement, a11y). Blazor keeps stack mechanics in `tech-blazor.md`. Playwright stays journeys and debug.
- Read and write files with stream/reader APIs. Do not slurp unbounded files into a single string or byte array.
- Performance is a feature: general rules in always-on instructions; GC, SIMD, and pools in the language skills. Gen-1 survival costs more than gen-0 on .NET hot paths. `ref struct` is a special-case stack tool, not a default.
- Dependencies need approval (what, why, license, alternatives). No shell/PowerShell/Python scripts in repo or chat except last resort with explanation.
- Test strategy lives in `tech-test.md`; TUnit, pytest, Rust, and Playwright skills hold runner mechanics only (no duplicated case-design rules). C# test methods are PascalCase without underscores (`PairEmptyRepoReturnsNone`).
- Warning suppressions always need user approval and a tech-skill template (rule id, reason, scope). Global C# `NoWarn` needs one comment per id, including why it is global (`tech-csharp.md`). `IDE1006` naming is EditorConfig-only (`tech-solution.md`); `NoWarn` does not clear the IDE squiggle.
- Non-temporary documents are English. Chat may follow the user.
- After writing a plan, walk it twice: requirements coverage and conversation coverage.

### Documentation

- `README.md` and `AGENTS.md` list the new workflows and tech skills.
- `tech-web.md` locks phone-first responsive layout (320px floor; 768 / 1024 / 1280 bands), CSS layers, and JS-vs-CSS rules. Playwright product journeys use those viewports.

---

## 1.0.0

### ExitPointGaps performance

- **Per-project parallelism** (default: all projects in scope concurrently); cap with `--max-parallelism <n>`
- **Solution pre-build** once before multi-project loops (skipped with `--no-build` or single project)
- **Streaming output:** aggregated `summary.json` is **schema v3** with `reportFile` indirection; per-project reports remain schema v1
- **Memory:** drop Roslyn `Compilation` after exit collection; scoped Cobertura parse; incremental per-project writes
- **Pipeline overlap:** Cobertura parse runs in parallel with Roslyn compile per project
- **Benchmark (CopilotAIWorkflow.slnx, Release, `--no-build`):** serial ~22.8s vs parallel ~12.0s (~47% faster)

### Breaking changes (aggregated JSON only)

| v2 | v3 |
|----|-----|
| `schemaVersion: 2` with embedded `report` objects | `schemaVersion: 3` with `projects[].reportFile` |
| Single-file multi-project `-o out.json` | Multi-project requires directory `-o reports/` or stdout `--stream` |

Migration: read `summary.json` for counts; load each `{reportFile}` for `exitGaps[]`.

### CLI

- `--max-parallelism <n>` — bounded concurrency (`n >= 1`)
- `--stream` / `--no-stream` — NDJSON stdout for multi-project runs without `-o`

### Rebrand (at this point)

- **ExitPointGaps** replaces `CoverageGap.Tool` + `CoverageGapAnalysis` (analysis integrated into tool assembly)
- **CSharpStyleChecker** replaces `CSharpStyleValidator`
- Version line restarts at 1.0.0 (not 3.0 continuation)
- CLI: `dotnet tool run exitpointgaps` (was `coveragegap`)
- Diagnostic IDs renamed **CSV001–CSV008** → **CSC001–CSC008** (`csc_max_line_length` editorconfig property)
- See migration table below

### Migration

| Old | New |
|-----|-----|
| `dotnet tool install CoverageGap.Tool` | `dotnet tool install ExitPointGaps` |
| `dotnet add package CoverageGapAnalysis` | use `ExitPointGaps` CLI (no library package) |
| `PackageReference CSharpStyleValidator` | `CSharpStyleChecker` |
| Analyzer rule IDs `CSV001–CSV008` | `CSC001–CSC008` |

---

> **Historical (pre-rebrand):** Entries below describe packages and names before 1.0.0.

## 2.1.2

### CSharpStyleValidator

- **CSV003:** Clarified exemptions — private members inside **private nested types**, **explicit interface implementations**, and **local functions** are no longer flagged; outer-type private members still require `_PascalCase`
- **CSV008:** Confirmed exempt when created type differs from contextual target (e.g. `IA a = new A()`, `IList<int> x = new List<int>()`); regression test added
- **CSV008:** Exempt array and collection initializers when the contextual target cannot use a collection expression (`Memory<T>`, `ReadOnlyMemory<T>`) or differs from the created type (e.g. `ReadOnlyMemory<byte> m = new byte[] { … }`)
- **CSV006:** Count `??` and `??=` as dual completion arms; multi-arm `?:`, `??`, `??=`, and `switch` exits share an operator group so line breaks cannot hide multiple completion paths
- **CSV007:** Plain `volatile` read/write allowed; forbid non-atomic `++`, `--`, and compound assignments (use `Interlocked`)

### Documentation

- Demo `CompliantExample` includes manual `IEnumerable<int>` with private nested enumerator
- `tech-csharp.md` documents CSV003 and CSV008 exemptions

## 2.1.0

### CSharpStyleValidator

- **CSV008:** Require target-typed `new()` and collection expressions `[]` instead of redundant explicit type names when the type is known from context (declaration, assignment, return, or argument)
- **Violations:** parameterless `new Type()`, empty object initializer `new Type() { }`, `new List<T>()`, collection initializer `new List<T> { … }`, array initializer `new T[] { … }`
- **Exemptions (unchanged behavior):**
  - Constructor calls with arguments: `new Foo(1)`
  - Object initializers with members: `new Foo() { Prop = value }`
  - Polymorphic construction: `Base b = new Derived()`
  - Interface or base-type targets: `IList<int> x = new List<int>()`
  - Generic type parameters: `T t = new T()`
  - `throw new ArgumentException()` and similar throw contexts
  - Rank-sized arrays without element initializer: `new int[n]`

### Documentation

- `tech-csharp.md` and `copilot-instructions.md` aligned with CSV008 exemptions and allocation guidance
- Demo sample: isolated CSV008 violation file; Csv005 no longer cross-triggers CSV008

## 2.0.0

### CoverageGap.Tool

- **Single-call gate:** `dotnet tool run coveragegap` / `… run` runs tests, collects Cobertura, and reports exit gaps
- **Zero-arg default:** from repo root, gates all production projects in the auto-discovered solution
- **Scoped targets:** `run project <csproj>...`, `run solution [path]`, `plan project <csproj>`
- **Parallel-safe:** unique `--work-dir` per invocation; relative `-o` under work dir; default stdout
- **Solution gate:** projects run sequentially; parallel per-project runs planned for a future release
- **Gate scope:** `run` gates class-library production projects; executable hosts (`OutputType` `Exe`) are excluded (including the tool itself)
- **Cancellation:** Ctrl+C and Unix SIGINT/SIGTERM cancel in-flight `dotnet` subprocesses
- **Isolated Cobertura:** results read only from the invocation work directory, not a global `TestResults/` scan

### Documentation

- README, `PACKAGE_README.md`, and `tech-tunit.md` document the CLI workflow

## 1.1.0

### CSharpStyleValidator

- Repo clone: auto-apply via root `Directory.Build.targets` for `netstandard2.0`+ and .NET Core App projects (including Roslyn source generators)
- NuGet: analyzers load from `analyzers/dotnet/cs` (bundled `ExitPoints.dll`); no MSBuild targets in the package
- Sample: `samples/CSharpStyleValidator.Demo/` — CSV* violations, source generator, packed NuGet verification

### Documentation

- README Quickstart with NuGet shields, consumer TFM matrix, and minimum SDK (Roslyn 4.14 / SDK 9.0.300+)
- `PACKAGE_README.md` requirements per package
- CoverageGap.Tool usage in `tech-tunit.md`
- Central package version in `Directory.Build.props` (`VersionPrefix`)

### Packaging

- Symbol package (`.snupkg`) for `CoverageGapAnalysis`

## 1.0.0

Initial public release.

### CSharpStyleValidator (NuGet)

Roslyn analyzer package enforcing CopilotAIWorkflow C# style as compiler errors:

| ID | Rule |
|----|------|
| CSV001 | Line length ≤ 160 (strings/comments masked) |
| CSV002 | No `var` |
| CSV003 | Private `_PascalCase` |
| CSV004 | No `.Result` / `.Wait()` on `Task` |
| CSV005 | `using` only in `GlobalUsings.cs` |
| CSV006 | At most one exit point per source line per callable |
| CSV007 | `volatile` fields: plain read/write allowed; non-atomic ops require `Interlocked` |

### CoverageGap.Tool (global tool)

- `coveragegap run` — test, collect Cobertura, and gate exit-point coverage
- `coveragegap plan` — export exit-point manifest without running tests

### CoverageGapAnalysis (library)

Cobertura parsing, branch-gap analysis, and exit-point comparison for agent-oriented coverage workflows.

### Agent workflow

- Exit-point coverage gate in `copilot-instructions.md` §4.5 and `tech-tunit.md`
- Blazor/Razor: exhaustive bUnit component tests per `tech-blazor.md`

### Requirements

- .NET SDK 10.0 (`global.json` pins 10.0.100)
- Target framework `net10.0` for applications in this repository; analyzer packages target `netstandard2.0` and apply to consumers from `netstandard2.0` / `net5.0`+
