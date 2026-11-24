# TUnit Testing Rules

Load when `*Tests.cs`, `{Project}.Tests` projects, `{App}.UiTest`, C# Playwright UI tests, or `global.json` are in scope. Content and case design: `tech-test.md`. Browser journeys: `tech-playwright.md`.

## Scope

- Unit tests: `{Project}.Tests`.
- Razor/Blazor component logic: bUnit in that `.Tests` project (`tech-blazor.md`).
- C# browser journeys: dedicated `{App}.UiTest` with **TUnit** + `TUnit.Playwright` (`tech-playwright.md`). Do not put journeys in `{Project}.Tests`.

## Framework

❗ **TUnit only** — unit, bUnit, and C# UI tests. Do not add or migrate to xUnit, NUnit, MSTest, FluentAssertions, `Microsoft.Playwright.NUnit`, `.MSTest`, or `.Xunit`.

**Minimal stack (nothing else required for unit tests):**

| Piece | Role |
|-------|------|
| **TUnit** | Test framework (`[Test]`, `await Assert.That(...)`, `[Arguments]`, `[Before]`) |
| **MTP** | Test runner (`global.json` → `Microsoft.Testing.Platform`) |

C# UI tests add `TUnit.Playwright` (`JourneyTest` / `PageTest` in `{App}.UiTest`) — `tech-playwright.md`. Default browser: system `Channel`; see that skill.

Common agent mistakes — **do not** port xUnit/NUnit habits:

| Wrong (xUnit/NUnit) | Use instead (TUnit) |
|---------------------|---------------------|
| `[Fact]` / `[TestMethod]` | `[Test]` on `async Task` method |
| `Assert.Equal(...)` | `await Assert.That(actual).IsEqualTo(...)` |
| `[Theory]` + `[InlineData]` | `[Test]` + `[Arguments(...)]` or `[MethodDataSource]` |
| `IClassFixture<T>` / `[SetUp]` | `[Before(Class)]` / `[Before(Test)]` |
| `PackageReference` xunit/nunit/`Microsoft.Playwright.NUnit` | **Remove** — unit `.csproj`: only `TUnit`. `{App}.UiTest` `.csproj`: `TUnit` + `TUnit.Playwright` |

**`global.json` (repo root):**

- SDK `10.0.100` with `"rollForward": "latestMajor"`: `10.0.100` is the minimum, and every later SDK is accepted, including `11.0.100` and newer majors. Never use `latestFeature` or `latestPatch`: they stay inside `10.0.x` and reject `11.x`. An exact pin fails on machines that only have a later SDK.
- `"test": { "runner": "Microsoft.Testing.Platform" }`

```json
{
  "sdk": { "version": "10.0.100", "rollForward": "latestMajor" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

## Structure

- Test project: `<ProductionProjectName>.Tests` — mirror prod namespace and folders.
- One file per class: `<ClassName>Tests.cs`.
- Section 3 file glob is `*Tests.cs` in `{Project}.Tests/` (not `*.Tests.cs`).
- Shared helpers: `Helpers/`.
- Test method names are **PascalCase without underscores**. Not snake_case, not `Method_Scenario_Expected`, not `_PascalCase`.
- Data source names: PascalCase without underscores (`MethodScenarioData`).

```csharp
[Test]
public async Task ParseEmptyInputReturnsNone()
```

## Authoring

Mechanics only. What to cover, speed, doubles, AAA: `tech-test.md`.

- `await Assert.That(actual).Is...`
- Always await async operations.
- Pass `CancellationToken` to cancellation-aware APIs.
- Assert exceptions with `Throws<T>`:

```csharp
await Assert.That(async () => await sut.PairAsync(path)).Throws<IOException>();
```

- Drive data with `[Arguments]` or `[MethodDataSource]`.

## Fixtures and parallelism

- In Windows projects with deliberate crash tests, verify dialog-free exit in the target runner before unattended runs.
  Check each process the suite deliberately crashes in that runner.

- `[Before(Test)]` / `[After(Test)]` for per-test setup.
- `[Before(Class)]` / `[After(Class)]` for class resources.
- `IAsyncDisposable` on test classes holding resources.
- Parallel-safe by default; no shared mutable statics.
- `[NotInParallel]` only when required. Document the reason in XML.
- ❗ Cap tests that start processes or browsers once per test project (`{App}.UiTest`, process-level integration tests), at assembly level (`tech-test.md` § Resource budget):

```csharp
[assembly: ParallelLimiter<HostProcessLimit>]

/// <summary>
/// Caps how many tests that start processes or browsers run at once, so a test run leaves the machine
/// responsive. Thread safety: stateless.
/// </summary>
internal sealed class HostProcessLimit : IParallelLimit
{
    /// <summary>Half the logical cores, at least one.</summary>
    public int Limit { get; } = Math.Max(1, Environment.ProcessorCount / 2);
}
```

- ❗ Mark an approved Visible, Global, or Heavy test (`tech-test.md` § Host safety) `[Explicit]`: an unfiltered `dotnet test` skips it. Name the effect and the run command in its XML doc:

```csharp
/// <summary>Writes the real system clipboard and restores it in teardown.
/// Run: dotnet test path/App.Tests.csproj -c Release -- --treenode-filter "/*/*/ClipboardAdapterTests/*"</summary>
[Explicit]
public sealed class ClipboardAdapterTests
```

## Coverage

- Use `[ExcludeFromCodeCoverage]` only with an XML reason.

```csharp
/// <summary>Native interop stub. Reason: not exercised from managed tests.</summary>
[ExcludeFromCodeCoverage]
```

## Commands

- Run `dotnet test` with a quoted `--treenode-filter` for the classes or tests that cover the change.
- Run the whole project when the change can break tests you cannot name ([Test runs](tech-test.md#runs)).
- Address all four levels in `/Assembly/Namespace/Class/Test`. Use `*` to match names within one level.
- Group alternative names with `(A|B)` inside one path segment. Never join whole paths with `|`.
- Reserve square brackets for property filters such as `[Category=Fast]`. Never use them to list names.

```text
Wrong: "/*/*/[ClassA|ClassB]/*"
Wrong: "(/*/*/ClassA/*)|(/*/*/ClassB/*)"
Right: "/*/*/(ClassA|ClassB)/*"
```

```bash
# One class
dotnet test --project path/Proj.Tests.csproj -c Release --treenode-filter "/*/*/ClassName/*"
# Multiple classes
dotnet test --project path/Proj.Tests.csproj -c Release --treenode-filter "/*/*/(ClassA|ClassB)/*"
# One test
dotnet test --project path/Proj.Tests.csproj -c Release --treenode-filter "/*/*/ClassName/TestName"
# Multiple tests in one class
dotnet test --project path/Proj.Tests.csproj -c Release --treenode-filter "/*/*/ClassName/(TestA|TestB)"
# Whole project
dotnet test --project path/Proj.Tests.csproj -c Release
```

- ❗ For `Unhandled exception` in `TreeNodeFilter.ParseFilter`, correct the filter syntax before retrying.
  Skip product-code debugging for this error. Never repeat the unchanged command.
- ❗ Treat MTP exit code 8 as a failed `Verify`: zero tests ran. Check the selected project, names and skipped tests.

MTP is the runner (`global.json`). Do not pass VSTest-style `--filter FullyQualifiedName~…`.
