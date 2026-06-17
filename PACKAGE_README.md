# CopilotAIWorkflow packages

NuGet packages from [CopilotAIWorkflow](https://github.com/DevAM/CopilotAIWorkflow) — agent workflow tooling with mechanical quality gates.

## CSharpStyleValidator

Roslyn analyzer enforcing C# style as **compiler errors** (CSV001–CSV007). Add as a development dependency:

```xml
<PackageReference Include="CSharpStyleValidator" Version="1.*" />
```
Analyzers load from `analyzers/dotnet/cs` (includes bundled `ExitPoints.dll`). No separate `ExitPoints` package or MSBuild targets are required.
See the repository [README.md](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#csharpstylevalidator) for rule details and demo usage.

## CoverageGapAnalysis

Library for Cobertura parsing, branch metrics, and exit-point gap reports. Used by agents and CI to enforce `exitGapCount == 0`.
**Consumer target:** `net10.0` or later (`lib/net10.0` only — not referenceable from `netstandard2.0` class libraries).

## CoverageGap.Tool

Global .NET tool (`coveragegap`):

```bash
dotnet tool install -g CoverageGap.Tool
coveragegap report project path/YourProject.csproj --search-root src --repo-root .
coveragegap manifest project path/YourProject.csproj -o exits.json
```

Analyzes any restored SDK-style project (including `netstandard2.0` source generators). Requires .NET 10 runtime.

## Requirements

| Package | Consumer / host | Notes |
|---------|-----------------|-------|
| CSharpStyleValidator | SDK-style `netstandard2.0`+ or .NET Core App | Minimum **.NET SDK 9.0.300** or **VS 2022 17.14+** (Roslyn 4.14) |
| CoverageGapAnalysis | `net10.0`+ library or app | Cannot be referenced from `netstandard2.0` projects |
| CoverageGap.Tool | .NET 10 runtime | Global CLI; not a project reference |
| This repository | .NET SDK 10.0 | `global.json` pins 10.0.100; app projects target `net10.0` |

**Repo clone only:** root `Directory.Build.targets` auto-applies `CSharpStyleValidator` to eligible projects. NuGet consumers use `PackageReference` instead.

## License

MIT — see [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
