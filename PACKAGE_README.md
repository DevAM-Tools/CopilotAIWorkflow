# CopilotAIWorkflow packages

NuGet packages from [CopilotAIWorkflow](https://github.com/DevAM/CopilotAIWorkflow) — agent workflow tooling with mechanical quality gates.

## CSharpStyleValidator

Roslyn analyzer enforcing C# style as **compiler errors** (CSV001–CSV007). Add as a development dependency:

```xml
<PackageReference Include="CSharpStyleValidator" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

See the repository [README.md](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#csharpstylevalidator) for rule details and demo usage.

## CoverageGapAnalysis

Library for Cobertura parsing, branch metrics, and exit-point gap reports. Used by agents and CI to enforce `exitGapCount == 0`.

## CoverageGap.Tool

Global .NET tool (`coveragegap`):

```bash
dotnet tool install -g CoverageGap.Tool
coveragegap report project path/YourProject.csproj --search-root src --repo-root .
coveragegap manifest project path/YourProject.csproj -o exits.json
```

## Requirements

- .NET 10 SDK for applications and tools
- Analyzers target `netstandard2.0` (works with `net10.0` consumers)

## License

MIT — see [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
