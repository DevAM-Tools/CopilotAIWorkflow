# CSharpStyleChecker Demo

Single `netstandard2.0` sample covering **CSharpStyleChecker** (CSC001–CSC008), a Roslyn source generator, repo wiring, and packed NuGet consumption.

## Compliant build (default)

`Violations/` is excluded. `CompliantExample.cs` includes a manual `IEnumerable<int>` with a private nested enumerator (CSC003 exempt). Analyzer applies via root [`Directory.Build.targets`](../../Directory.Build.targets) when building in this repository.

```bash
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release
```

## Rule violations (CSC001–CSC008)

Enable all files under `Violations/` and rebuild. The build should fail with analyzer errors:

```bash
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release -p:IncludeViolations=true
```

| File | Rule | What it demonstrates |
|------|------|----------------------|
| `Violations/Csc001_LineLength.cs` | CSC001 | Line longer than 160 visible characters |
| `Violations/Csc002_NoVar.cs` | CSC002 | `var` in a local declaration |
| `Violations/Csc003_PrivateNaming.cs` | CSC003 | Private field without `_PascalCase` |
| `Violations/Csc004_TaskBlocking.cs` | CSC004 | `.Result` on `Task<T>` |
| `Violations/Csc005_GlobalUsings.cs` | CSC005 | `using` outside `GlobalUsings.cs` |
| `Violations/Csc006_MultipleExits.cs` | CSC006 | Multiple `return` statements on one line |
| `Violations/Csc007_VolatileAccess.cs` | CSC007 | Non-atomic `++` on a `volatile` field |
| `Violations/Csc008_TargetTypedCreation.cs` | CSC008 | Redundant explicit type (`new List<int>()`) |

To test a single rule, temporarily remove the other `.cs` files from `Violations/` or add a more specific `<Compile Remove="..."/>` in the `.csproj`.

## Source generator

`GreetingGenerator.cs` is an `IIncrementalGenerator` in the same project. It receives the analyzer when built in this repo (or via NuGet in external projects).

## External projects (NuGet)

Add the analyzer package explicitly:

```xml
<PackageReference Include="CSharpStyleChecker" Version="1.*" />
```

Requires SDK-style `netstandard2.0` (or .NET Core App) and **.NET SDK 9.0.300+** (Roslyn 4.14). See the root [README.md](../../README.md#csharpstylechecker).

## Packed NuGet verification (CI)

After `dotnet build CopilotAIWorkflow.slnx -c Release` (creates `artifacts/*.nupkg`):

```bash
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release -p:UseNuGetPackage=true
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release -p:UseNuGetPackage=true -p:IncludeViolations=true
```

Restore uses `nuget.config` to prefer `artifacts/` as a local feed. Expect build failure with `IncludeViolations=true`.

## Custom line length (CSC001)

Uncomment in `.editorconfig`:

```ini
build_property.csc_max_line_length = 80
```

The analyzer project must expose the property via `CompilerVisibleProperty` (already configured in this repo).
