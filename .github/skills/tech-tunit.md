# TUnit Testing Rules

Load when test files or test projects are in scope. Implements Section 4.5 enforcement in `copilot-instructions.md`.

## Scope

- Unit tests in `.Tests` projects; bUnit component tests for Razor/Blazor → `tech-blazor.md`. Exit-point gate on testable `.razor.cs` and service logic.

## Framework

- Use TUnit
- Make every test method `async Task`.
- Use NSubstitute for doubles.

## Test Quality

- Never use `Thread.Sleep` in tests.
- Cover happy path, errors, edges, boundaries, corners.
- Cover `null`, empty, min/max, off-by-one, max-length strings, collections 0/1/2, concurrency, branch-straddling values.
- Use data-driven tests (`[Arguments(...)]`, `[MethodDataSource(...)]`).
- Keep tests deterministic, independent, order-insensitive.
- Run tests on Windows/Linux/macOS and x64/ARM64.
- Assert one logical outcome per test.

## CoverageGap.Tool

SSOT for exit-point coverage workflow. Release gate: `summary.exitGapCount == 0`. Branch fields in the report are informational only.

**Run** (each production `.csproj` in scope):

```bash
dotnet test <Solution> -c Release -- --coverage --coverage-output-format cobertura
dotnet run --project src/CoverageGap.Tool/CoverageGap.Tool.csproj -c Release -- report project <Prod.csproj> --search-root src --repo-root . --format agent --no-fail
```

Consumer repo: `coveragegap report project <Prod.csproj> --search-root <src> --repo-root . --format agent --no-fail` (after `dotnet tool install -g CoverageGap.Tool`).

**Work results:** read JSON `summary.exitGapCount` / `summary.gatePassed`; fix every item in `exitGaps[]` (file, line, `exitPointId`, `kind`) with a test; add `--include-snippet` when needed. Loop test → report until `exitGapCount == 0`. Do not block release on `branchGapCount` or `branchGatePassed`.

**Plan tests:** `manifest project <Prod.csproj> -o exits.json` (same `dotnet run` / global `coveragegap` prefix as `report`).

## Structure

- Name test project `<ProductionProjectName>.Tests`.
- Mirror production namespace and folder structure.
- Use one test file per production class: `<ClassName>Tests.cs`.
- Put shared helpers in `Helpers/`.
- Name test methods `<Method>_<Scenario>_<ExpectedResult>`.
- Name method data sources `<Method>_<Scenario>_Data`.

## Authoring

- Separate Arrange, Act, Assert with blank lines.
- Use `await Assert.That(actual).Is...`.
- Use builders for non-trivial setup.
- Use `[Arguments(...)]` for corner cases.
- Use `[MethodDataSource(...)]` for reusable data sets.
- Always await async operations.
- Assert exceptions: `await Assert.That(async () => await sut.Method()).Throws<ExceptionType>()`.
- Pass `CancellationToken` to cancellation-aware APIs; test cancellation.

## Fixtures and Parallelism

- Use `[Before(Test)]` / `[After(Test)]` for per-test setup.
- Use `[Before(Class)]` / `[After(Class)]` for class resources.
- Implement `IAsyncDisposable` on test classes holding resources.
- Keep tests parallel-safe by default.
- Avoid shared mutable statics.
- Use `[NotInParallel]` only when required; document reason in XML.
- Run coordinated concurrent tasks in concurrency tests; verify no corruption, deadlock, or data loss.

## Doubles and Coverage

- Prefer real implementations when deterministic.
- Substitute only external or non-deterministic dependencies.
- Prefer outcome assertions over interaction counts.
- Never relax access modifiers for tests only.
- Document `[ExcludeFromCodeCoverage]` with XML reason; excluded exits are omitted from the exit-coverage gate.
