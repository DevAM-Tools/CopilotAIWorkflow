# CopilotAIWorkflow packages

NuGet packages from [CopilotAIWorkflow](https://github.com/DevAM/CopilotAIWorkflow) — agent workflow tooling with mechanical quality gates.

## CSharpStyleValidator

Roslyn analyzer enforcing C# style as **compiler errors** (CSV*). Add as a development dependency:

```xml
<PackageReference Include="CSharpStyleValidator" Version="2.*" />
```

Analyzers load from `analyzers/dotnet/cs` (includes bundled `ExitPoints.dll`). See [README.md — CSharpStyleValidator](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#csharpstylevalidator).

## CoverageGapAnalysis

Library for Cobertura parsing, branch metrics, and exit-point gap reports. Consumer: `net10.0`+.

## CoverageGap.Tool

Local dotnet tool — register once per repo, run via `dotnet tool run coveragegap`:

```bash
dotnet new tool-manifest
dotnet tool install CoverageGap.Tool --version 2.*
dotnet tool run coveragegap --repo-root .
```

Requires .NET SDK 10.0, MTP + TUnit in test projects. Workflow SSOT: [tech-tunit.md](https://github.com/DevAM/CopilotAIWorkflow/blob/main/.github/skills/tech-tunit.md). User quickstart: [README.md §2](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#2--exit-point-coverage-gate-cli).

## Requirements

| Package | Consumer / host | Notes |
|---------|-----------------|-------|
| CSharpStyleValidator | SDK-style `netstandard2.0`+ or .NET Core App | .NET SDK 9.0.300+ or VS 2022 17.14+ (Roslyn 4.14) |
| CoverageGapAnalysis | `net10.0`+ | Not referenceable from `netstandard2.0` class libraries |
| CoverageGap.Tool | .NET SDK 10.0 | Local tool manifest; not a project reference |
| This repository | .NET SDK 10.0 | `global.json` pins 10.0.100 |

**Repo clone:** `Directory.Build.targets` auto-applies `CSharpStyleValidator`. NuGet consumers use `PackageReference`.

## License

MIT — see [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
