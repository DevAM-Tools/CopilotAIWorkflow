# CSharpStyleChecker changelog

All notable changes to the **CSharpStyleChecker** NuGet analyzer are documented here.
Repository-wide and agent-workflow notes live in the root [CHANGELOG.md](../../CHANGELOG.md).

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
