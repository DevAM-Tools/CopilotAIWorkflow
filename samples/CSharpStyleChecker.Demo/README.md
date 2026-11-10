# CSharpStyleChecker Demo

Single `netstandard2.0` sample covering **CSharpStyleChecker** (CSC001–CSC009), a Roslyn source generator, repo wiring, and packed NuGet consumption.

## Compliant build (default)

`Violations/` is excluded. `CompliantExample.cs` includes a manual `IEnumerable<int>` with a private nested enumerator (CSC003 exempt). Analyzer applies via root [`Directory.Build.targets`](../../Directory.Build.targets) when building in this repository.

```bash
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release
```

## Rule violations (CSC001–CSC009)

Enable all files under `Violations/` and rebuild. The build should fail with analyzer errors:

```bash
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release -p:IncludeViolations=true
```

| File | Rule | What it demonstrates |
|------|------|----------------------|
| `Violations/Csc001_LineLength.cs` | CSC001 | Line longer than 130 visible characters (string/character literal and raw-string content masked; comments and `$"..."` / `@$"..."` holes count) |
| `Violations/Csc002_NoVar.cs` | CSC002 | `var` in a local declaration |
| `Violations/Csc003_PrivateNaming.cs` | CSC003 | Private field without `_PascalCase` |
| `Violations/Csc004_TaskBlocking.cs` | CSC004 | Blocking `.Result` / `.Wait()` / `GetAwaiter().GetResult()` / `WaitAll` / `WaitAny` on task types |
| `Violations/Csc005_GlobalUsings.cs` | CSC005 | `using` outside `GlobalUsings.cs` |
| `Violations/Csc006_MultipleExits.cs` | CSC006 | Multiple `return` statements on one line |
| `Violations/Csc007_VolatileAccess.cs` | CSC007 | Non-atomic `++` on a `volatile` field |
| `Violations/Csc008_TargetTypedCreation.cs` | CSC008 | Redundant explicit type (`new List<int>()`) |
| `Violations/Csc009_InvariantCulture.cs` | CSC009 | Bare `$"…"` interpolation or culture-sensitive `ToString()` without `CultureInfo.InvariantCulture`. This sample targets `netstandard2.0`, so `FormattableString.Invariant` (`using static System.FormattableString;`) is the accepted fix here; on .NET 6+ use `Invariant($"…")` from the InvariantStringHelper package |

To test a single rule, temporarily remove the other `.cs` files from `Violations/` or add a more specific `<Compile Remove="..."/>` in the `.csproj`.

## Source generator

`GreetingGenerator.cs` is an `IIncrementalGenerator` in the same project. It receives the analyzer when built in this repo (or via NuGet in external projects).

## External projects (NuGet)

Add the analyzer package explicitly:

```xml
<PackageReference Include="CSharpStyleChecker" Version="2.*" />
```

Requires SDK-style `netstandard2.0` (or .NET Core App) and **.NET SDK 9.0.300+** (Roslyn 4.14). See the root [README.md](../../README.md#csharpstylechecker).

## Packed NuGet verification (CI)

Pack the analyzer, then build the sample against that package:

```bash
dotnet pack src/CSharpStyleChecker/CSharpStyleChecker.csproj -c Release
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release -p:UseNuGetPackage=true
dotnet build samples/CSharpStyleChecker.Demo/CSharpStyleChecker.Demo.csproj -c Release -p:UseNuGetPackage=true -p:IncludeViolations=true
```

With `UseNuGetPackage=true`, restore adds `artifacts/release/` as a package source and uses the private packages folder `obj/packages/`, so an older build of the same version in `~/.nuget/packages` cannot stand in. `obj/packages/csharpstylechecker/{version}/.nupkg.metadata` names the source that was used. The first build succeeds; the second fails with CSC001–CSC009.

## Custom line length (CSC001)

Uncomment in `.editorconfig`:

```ini
build_property.csc_max_line_length = 80
```

The analyzer project must expose the property via `CompilerVisibleProperty` (already configured in this repo).
