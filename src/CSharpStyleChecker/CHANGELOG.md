# CSharpStyleChecker changelog

All notable changes to the **CSharpStyleChecker** NuGet analyzer are documented here.
Repository-wide and agent-workflow notes live in the root [CHANGELOG.md](../../CHANGELOG.md).

## 1.2.0

### Breaking: `FormattableString.Invariant` is an error on .NET 6 and later

CSC009 now reports `FormattableString.Invariant($"…")` and `Invariant($"…")` imported through `using static System.FormattableString;` in every compilation where `string.Create(IFormatProvider, ref DefaultInterpolatedStringHandler)` binds (.NET 6 and later). 1.1.0 accepted both. The call allocates a `FormattableString` and an `object[]` and boxes value arguments. `netstandard2.0` projects are unaffected: there it stays the only invariant interpolation.

Before (accepted by 1.1.0, error in 1.2.0 on `net6.0`+):

```csharp
global using static System.FormattableString;

string message = Invariant($"count={count}");
```

After (reference the [InvariantStringHelper](../InvariantStringHelper/CHANGELOG.md) package and change one `using`):

```csharp
global using static InvariantStringHelper.InvariantText;

string message = Invariant($"count={count}");
```

The call sites stay as they are. `string.Create(CultureInfo.InvariantCulture, $"…")` is also accepted. A project on `Version="1.*"` picks this release up automatically; to stay on 1.1.0 until you have migrated, pin `Version="[1.1.0]"`.

### CSC009 recommends and accepts the InvariantStringHelper package

- **Update required for projects that use InvariantStringHelper.** CSharpStyleChecker 1.1.0 reports `Invariant($"…")` from the new [InvariantStringHelper](../InvariantStringHelper/CHANGELOG.md) package as a bare interpolated string (CSC009), and its message points at `Invariant($"…")`, which it then rejects. 1.2.0 accepts the call. Projects that do not use the package behave as before.
- The helper is recognized by symbol identity (`InvariantStringHelper.InvariantText.Invariant` with one `ref InvariantInterpolatedStringHandler` parameter). A method that only shares the name `Invariant` is not accepted. If two referenced assemblies define the same type name, the helper is not recognized and the call is reported.
- Messages on .NET 6 and later point at the package: without the package reference, "Add the InvariantStringHelper package and use `Invariant($"…")`"; with it, "Use `Invariant($"…")` from the InvariantStringHelper package". `StringBuilder.Append(CultureInfo.InvariantCulture, …)` is named for builders. `string.Create(CultureInfo.InvariantCulture, …)` is still accepted; it is no longer suggested.
- The advice follows what the compilation can do, not the project's name. The package and `string.Create` are recommended only where interpolated string handlers exist: a framework with `string.Create(IFormatProvider, ref DefaultInterpolatedStringHandler)` (.NET 6 and later) and language version C# 10 or later. `netstandard2.0`, `netstandard2.1`, .NET Framework, .NET 5, and a .NET 6 project pinned to C# 9 keep `FormattableString.Invariant`, and CSC009 names it: "Use `FormattableString.Invariant($"…")` … this target has no allocation-free alternative".
- Fix: an interpolated string in parentheses (`Invariant(($"…"))`, `string.Create(provider, ($"…"))`, `builder.Append(provider, ($"…"))`) was reported although the compiler accepts it as a handler argument. It is now recognized.

## 1.1.0

### New rule

- **CSC009:** bare interpolated strings (`$"…"`, `$@"…"`, raw) and culture-sensitive `ToString` without `CultureInfo.InvariantCulture` are errors. Allowed interpolation wrappers: `FormattableString.Invariant` / `Invariant($"…")`, `string.Create(CultureInfo.InvariantCulture, …)`, `StringBuilder.Append` / `AppendLine(CultureInfo.InvariantCulture, …)`. `bool` / `char` / `string` / `enum` `ToString` are exempt.

Example — before (error):

```csharp
string msg = $"count={count}";
```

Example — after:

```csharp
string msg = Invariant($"count={count}");
```

### Rule updates

- **CSC001:** multi-line verbatim and raw string content no longer counts toward the line-length limit; string state is carried across lines. Comment text (`//` and `/* */`) counts toward the limit. Lines that start with `*` are measured as code when they are not inside a string. Interpolation holes inside `$"..."` / `@$"..."` / `$@"..."` count toward the limit; literal payload and raw-string content (including raw interpolations) still do not.
- **CSC002:** also reports `var` in `for`, classic `using (` / `await using (`, `fixed (`, and `foreach` deconstruction (`foreach (var (a, b) in …)`).
- **CSC004:** `ValueTask<T>.Result` is reported; also `GetAwaiter().GetResult()`, `Task.WaitAll`, and `Task.WaitAny`; task type checks use compilation-cached symbols (no per-node display strings).
- **CSC006:** expression-bodied indexers are covered; ternary diagnostics anchor on `QuestionToken`; lambdas and anonymous methods are documented as out of scope. Collection uses one declaration walk and `readonly record struct` exits; grouping uses a path map. `[ExcludeFromCodeCoverage]` no longer waives CSC006.
- **CSC003:** private-name check uses a character loop (no `Regex`) and guards empty `Locations`.
- **CSC008:** `Memory<T>` / `ReadOnlyMemory<T>` exemptions compare cached symbols.

### Packaging and repo clone

- Analyzer helpers (`LineMasking`, `DiagnosticIds`, `DiagnosticDescriptors`, mask state types) are `internal`.
- Repo clone consumers no longer nest an MSBuild of the checker before every `CoreCompile`; `ExitPoints.dll` is added as an analyzer dependency after project references resolve.
- Production sources use `#region` layout (Fields / Lifecycle / Public API / Private helpers).

## 1.0.1

- Stop **CSharpStyleChecker** from flowing transitively to downstream `ProjectReference` consumers (`PrivateAssets` wiring in repo `Directory.Build.targets`).

## 1.0.0

- Initial rebrand release as **CSharpStyleChecker** (was `CSharpStyleValidator`).
- Rule IDs **CSC001–CSC008** (was CSV*). EditorConfig property: `csc_max_line_length`.
- Bundles `ExitPoints.dll` under `analyzers/dotnet/cs`.
