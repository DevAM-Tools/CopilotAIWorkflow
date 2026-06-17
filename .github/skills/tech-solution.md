# Solution and Build Configuration

Load when `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `GlobalUsings.cs`, or `*.csproj` / `*.props` / `*.targets` are in scope. Extends Sections 4.7 in `copilot-instructions.md` and `tech-csharp.md`. SSOT for build properties, CPM, and New Dependency Protocol.

## File Layout

| File | Location |
|------|----------|
| `Directory.Build.props` | repository root |
| `Directory.Packages.props` | repository root |
| `Directory.Build.targets` | repository root (only when needed) |
| `GlobalUsings.cs` | project root, no namespace |

## Directory.Build.props

### Target Framework and Language

| Property | Value |
|----------|-------|
| `TargetFramework` | `net10.0` |
| `TargetFrameworks` | multi-target only; must include `net10.0` |
| `LangVersion` | `14` |
| `Nullable` | `enable` |
| `ImplicitUsings` | `enable` |

Generator projects only:

| Property | Value |
|----------|-------|
| `TargetFramework` | `netstandard2.0` |

### Analysis and Warnings

| Property | Value |
|----------|-------|
| `TreatWarningsAsErrors` | `true` |
| `EnableNETAnalyzers` | `true` |
| `EnforceCodeStyleInBuild` | `true` |
| `AnalysisLevel` | `latest` |
| `AnalysisMode` | `AllEnabledByDefault` |
| `GenerateDocumentationFile` | `true` |
| `NoWarn` | omit globally |
| `WarningsAsErrors` | optional; specific warning codes only |
| `WarningsNotAsErrors` | omit; documented exception only |

### Build Behavior

| Property | Value |
|----------|-------|
| `Deterministic` | `true` |
| `VersionPrefix` | central in `Directory.Build.props` (e.g. `1.1.0`) |
| `ContinuousIntegrationBuild` | `true` when `CI` is set |
| `DebugType` | `embedded` or `portable` (consistent) |
| `RestoreUseStaticGraphEvaluation` | `true` |

### Versioning & Metadata (on-request)

Central release version: `VersionPrefix` in `Directory.Build.props` (applies to all packable projects with `PackageId`). Per-project `VersionPrefix` overrides only when a package must diverge.
When publishing or packaging is in scope, ask user for: `VersionSuffix`, `Company`, `Authors`, `Copyright`, `Description`, `PackageLicenseExpression`, `PackageProjectUrl`, `RepositoryUrl`.

## Directory.Packages.props

| Property / item | Value |
|---------------|-------|
| `ManagePackageVersionsCentrally` | `true` |
| `CentralPackageTransitivePinningEnabled` | `true` |
| `PackageVersion` | `Include="{package-id}" Version="{version}"` |
| Project `PackageReference` | `Include="{package-id}"` — no `Version` |

## Project File (`.csproj`)

| Item | Value / rule |
|------|----------------|
| `PackageReference` | `Include` only; version from `Directory.Packages.props` |
| `ProjectReference` | relative path |
| `OutputType` | per project |
| `RootNamespace` | per project |
| `AssemblyName` | per project |
| Duplicate `Directory.Build.props` properties | omit |

## CSharpStyleValidator

- Add `CSharpStyleValidator` NuGet to every consumer from `netstandard2.0` or .NET Core App (`net5.0`+), including Roslyn source generators.
- Auto-apply via `Directory.Build.targets`
- Set `ApplyCSharpStyleValidator=false` only for explicit opt-out.

## New Dependency Protocol

- Never add `PackageReference`, `PackageVersion`, or `ProjectReference` without user approval.
- Ask in Grill-Me when plan may need new dependencies.
- Present: package id, purpose, license (`MIT` / `Apache-2.0` / BSD-like), alternatives.
- After approval: add `PackageVersion` first, then `PackageReference` without `Version`.

## Source File Copyright Header

| File type | Value |
|-----------|-------|
| `.cs`, `.razor.cs`, `.css` | `// {copyright}` — exact text from `COPYRIGHT` |
| `.md`, `.html`, `.razor` | `<!-- {copyright} -->` — exact text from `COPYRIGHT` |

## Skeleton

`Directory.Build.props`: `<TargetFramework>net10.0</TargetFramework>`, `<LangVersion>14</LangVersion>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<EnableNETAnalyzers>true</EnableNETAnalyzers>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`, `<AnalysisLevel>latest</AnalysisLevel>`, `<AnalysisMode>AllEnabledByDefault</AnalysisMode>`, `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, `<Deterministic>true</Deterministic>`.

`Directory.Packages.props`: `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`, `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` + `<PackageVersion Include="..." Version="..." />` per package.
