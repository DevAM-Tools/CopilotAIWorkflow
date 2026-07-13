# CopilotAIWorkflow packages

NuGet packages from [CopilotAIWorkflow](https://github.com/DevAM/CopilotAIWorkflow) — agent workflow tooling with mechanical quality gates.

**Release 1.0.0** — two packages replace the deprecated **CSharpStyleValidator**, **CoverageGap.Tool**, and **CoverageGapAnalysis**. Same purpose and scope; version line restarts at 1.0.0.

## CSharpStyleChecker

Roslyn analyzer enforcing C# style as **compiler errors** (CSC001–CSC008). Add as a development dependency:

```xml
<PackageReference Include="CSharpStyleChecker" Version="1.*" />
```

Analyzers load from `analyzers/dotnet/cs` (includes bundled `ExitPoints.dll`). See [README.md — CSharpStyleChecker](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#csharpstylechecker).

**Replaces:** `CSharpStyleValidator` (rule IDs CSV* → CSC*).

## ExitPointGaps

Local dotnet tool — lists exit-point gaps (`exitGaps[]`) from Cobertura and Roslyn exit-point collection. Analysis is integrated into the tool assembly (no separate library package).

```bash
dotnet new tool-manifest
dotnet tool install ExitPointGaps --version 1.*
dotnet tool run exitpointgaps --repo-root .
```

Requires .NET SDK 10.0, MTP + TUnit in test projects. Gate: `summary.exitGapCount == 0`. Multi-project summaries use schema v3 (`reportFile` indirection). Workflow SSOT: [tech-tunit.md](https://github.com/DevAM/CopilotAIWorkflow/blob/main/.github/skills/tech-tunit.md). User quickstart: [README.md §2](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#2--exit-point-coverage-gate-cli).

**Replaces:** `CoverageGap.Tool` + `CoverageGapAnalysis`.

### Performance (1.0.0)

- Per-project parallelism by default; cap with `--max-parallelism <n>`
- Solution pre-build once, then `--no-build` for repeat runs
- Scoped Cobertura parse, streaming writes, Roslyn `Compilation` dropped after exit collection
- Benchmark (this repo, Release, `--no-build`): serial ~22.8s → parallel ~12.0s (~47% faster)

Details: [CHANGELOG.md](CHANGELOG.md).

## Migration

| Deprecated | Use instead |
|------------|-------------|
| `PackageReference CSharpStyleValidator` | `CSharpStyleChecker` |
| `dotnet tool install CoverageGap.Tool` | `dotnet tool install ExitPointGaps` |
| `PackageReference CoverageGapAnalysis` | `ExitPointGaps` CLI only |
| Analyzer IDs `CSV001–CSV008` | `CSC001–CSC008` |
| `dotnet tool run coveragegap` | `dotnet tool run exitpointgaps` |

## Requirements

| Package | Consumer / host | Notes |
|---------|-----------------|-------|
| CSharpStyleChecker | SDK-style `netstandard2.0`+ or .NET Core App | .NET SDK 9.0.300+ or VS 2022 17.14+ (Roslyn 4.14) |
| ExitPointGaps | .NET SDK 10.0 | Local tool manifest; not a project reference |
| This repository | .NET SDK 10.0 | `global.json` pins 10.0.100 |

**Repo clone:** `Directory.Build.targets` auto-applies `CSharpStyleChecker` (opt out with `ApplyCSharpStyleChecker=false`). NuGet consumers use `PackageReference` on each project that should enforce CSC rules; not transitive to downstream libraries.

## License

MIT — see [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
