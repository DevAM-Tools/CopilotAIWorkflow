# Changelog

All notable changes to this project are documented in this file.

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
