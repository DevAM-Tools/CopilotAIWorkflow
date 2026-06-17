# CSharpStyleValidator Demo

Single `netstandard2.0` sample covering **CSharpStyleValidator** (CSV001–CSV007), a Roslyn source generator, repo wiring, and packed NuGet consumption (CI).

## Compliant build (default)

`Violations/` is excluded. Analyzer applies via root [`Directory.Build.targets`](../../Directory.Build.targets) when building in this repository.

```bash
dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release
```

## Rule violations (CSV001–CSV007)

Enable all files under `Violations/` and rebuild. The build should fail with analyzer errors:

```bash
dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release -p:IncludeViolations=true
```

| File | Rule | What it demonstrates |
|------|------|----------------------|
| `Violations/Csv001_LineLength.cs` | CSV001 | Line longer than 160 visible characters |
| `Violations/Csv002_NoVar.cs` | CSV002 | `var` in a local declaration |
| `Violations/Csv003_PrivateNaming.cs` | CSV003 | Private field without `_PascalCase` |
| `Violations/Csv004_TaskBlocking.cs` | CSV004 | `.Result` on `Task<T>` |
| `Violations/Csv005_GlobalUsings.cs` | CSV005 | `using` outside `GlobalUsings.cs` |
| `Violations/Csv006_MultipleExits.cs` | CSV006 | Multiple `return` statements on one line |
| `Violations/Csv007_VolatileAccess.cs` | CSV007 | Plain read of a `volatile` field |

To test a single rule, temporarily remove the other `.cs` files from `Violations/` or add a more specific `<Compile Remove="..."/>` in the `.csproj`.

## Source generator

`GreetingGenerator.cs` is an `IIncrementalGenerator` in the same project. It receives the analyzer when built in this repo (or via NuGet in external projects).

## External projects (NuGet)

Add the analyzer package explicitly:

```xml
<PackageReference Include="CSharpStyleValidator" Version="1.*" />
```

Requires SDK-style `netstandard2.0` (or .NET Core App) and **.NET SDK 9.0.300+** (Roslyn 4.14). See the root [README.md](../../README.md#csharpstylevalidator).

## Packed NuGet verification (CI)

After `dotnet build CopilotAIWorkflow.slnx -c Release` (creates `artifacts/*.nupkg`):

```bash
dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release -p:UseNuGetPackage=true
dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release -p:UseNuGetPackage=true -p:IncludeViolations=true
```

Restore uses `nuget.config` to prefer `artifacts/` as a local feed. Expect build failure with `IncludeViolations=true`.

## Custom line length (CSV001)

Uncomment in `.editorconfig`:

```ini
build_property.csv_max_line_length = 80
```

The analyzer project must expose the property via `CompilerVisibleProperty` (already configured in this repo).
