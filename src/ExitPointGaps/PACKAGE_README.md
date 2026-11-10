# ExitPointGaps

Local dotnet tool that lists exit-point gaps (`exitGaps[]`): exit points of your C# methods that no test reached, from Cobertura coverage and Roslyn exit-point collection. Analysis is part of the tool; there is no library package.

```bash
dotnet new tool-manifest
dotnet tool install ExitPointGaps --version 1.*
dotnet tool run exitpointgaps --repo-root .
```

Gate: `summary.exitGapCount == 0`. Exit codes: `0` pass · `1` gap or failure · `2` usage. Multi-project summaries use schema v3 (`reportFile` indirection).

Commands and options: [README — ExitPointGaps](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#exitpointgaps). Changes: [CHANGELOG](https://github.com/DevAM/CopilotAIWorkflow/blob/main/src/ExitPointGaps/CHANGELOG.md).

## Requirements

.NET SDK 10.0; Microsoft.Testing.Platform and TUnit in the test projects. Install it in a local tool manifest, not as a project reference.

## Migration

| Deprecated | Use instead |
|------------|-------------|
| `dotnet tool install CoverageGap.Tool` | `dotnet tool install ExitPointGaps` |
| `PackageReference CoverageGapAnalysis` | `ExitPointGaps` CLI only |
| `dotnet tool run coveragegap` | `dotnet tool run exitpointgaps` |

## License

MIT. See [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
