# CSharpStyleValidator Demo

Small console app to try the **CSharpStyleValidator** Roslyn analyzer (CSV001–CSV007) from this repository.

## Compliant build (default)

The `Violations/` folder is excluded from compilation. Build and run:

```bash
dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release
dotnet run --project samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release --no-build
```

## Try rule violations

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

## Custom line length (CSV001)

Uncomment in `.editorconfig`:

```ini
build_property.csv_max_line_length = 80
```

The analyzer project must expose the property via `CompilerVisibleProperty` (already configured in this repo).

## Analyzer reference

The demo references the local analyzer project (equivalent to the NuGet package). Only `CSharpStyleValidator` is required:

```xml
<PackageReference Include="CSharpStyleValidator" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

After `dotnet pack`, use the packaged NuGet as described in the root [README.md](../../README.md).
