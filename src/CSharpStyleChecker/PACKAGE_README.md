# CSharpStyleChecker

Roslyn analyzer that enforces the C# style of [CopilotAIWorkflow](https://github.com/DevAM/CopilotAIWorkflow) as **compiler errors** (CSC001–CSC009). Add it as a development dependency:

```xml
<PackageReference Include="CSharpStyleChecker" Version="3.*" />
```

Rebuild. Violations surface as compiler errors. The analyzers load from `analyzers/dotnet/cs` and bundle `ExitPoints.dll`; no second package and no `PrivateAssets` are needed. The checker applies to the referencing project only; it does not flow to downstream projects.

**2.0.1** supports top-level statements under CSC003, including .NET 10 file-based apps.

With **3.0.1**, CSC009 accepts long messages split across lines inside a single `InvariantStringHelper` call.
Prefix each segment with `$` so the compiler formats them with one invariant handler.

```csharp
string message = Invariant(
    $"Order {orderId}. "
    + $"Total {total:F2}.");
```

CSC001 allows 120 visible characters by default. To keep the 2.x limit, set the width in `.editorconfig`.

```ini
[*.cs]
build_property.csc_max_line_length = 130
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
