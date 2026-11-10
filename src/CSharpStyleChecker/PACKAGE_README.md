# CSharpStyleChecker

Roslyn analyzer that enforces the C# style of [CopilotAIWorkflow](https://github.com/DevAM/CopilotAIWorkflow) as **compiler errors** (CSC001–CSC009). Add it as a development dependency:

```xml
<PackageReference Include="CSharpStyleChecker" Version="2.*" />
```

Rebuild. Violations surface as compiler errors. The analyzers load from `analyzers/dotnet/cs` and bundle `ExitPoints.dll`; no second package and no `PrivateAssets` are needed. The checker applies to the referencing project only; it does not flow to downstream projects.

**2.0.0 is breaking:** CSC001 defaults to 130 characters (was 160). Keep the old limit in `.editorconfig`:

```ini
[*.cs]
build_property.csc_max_line_length = 160
```

Rule reference: [README — CSharpStyleChecker](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#csharpstylechecker). Changes: [CHANGELOG](https://github.com/DevAM/CopilotAIWorkflow/blob/main/src/CSharpStyleChecker/CHANGELOG.md).

## Requirements

SDK-style `netstandard2.0`+ or .NET Core App project; .NET SDK 9.0.300+ or Visual Studio 2022 17.14+ (Roslyn 4.14).

## Migration

| Deprecated | Use instead |
|------------|-------------|
| `PackageReference CSharpStyleValidator` | `CSharpStyleChecker` |
| Analyzer IDs `CSV001–CSV008` | `CSC001–CSC008` |

## License

MIT. See [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
