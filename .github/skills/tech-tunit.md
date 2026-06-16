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

## Exit-Point Coverage

- After `dotnet test` with Cobertura output, run CoverageGap.Tool `report project` on each production project in scope.
- Pass when report `exitGapCount == 0`. Branch metrics (`branchGapCount`, `branchRate`, `branchGatePassed`) are informational only.
- Use `--no-fail` on `report` when verifying exit coverage only; still require `exitGapCount == 0`.
- Workflow: test → report → add or update test for each exit gap → repeat until `exitGapCount == 0`.

```bash
dotnet test <Solution> -c Release -- --coverage --coverage-output-format cobertura
dotnet run --project <CoverageGapToolProject> -c Release -- report project <ProductionProject> --search-root <SearchRoot> --repo-root <RepoRoot> --no-fail
```

- Use `manifest project` to list exit-point IDs when planning tests:

```bash
dotnet run --project <CoverageGapToolProject> -c Release -- manifest project <ProductionProject> -o exits.json
```

- Use `--format agent` for structured gap JSON; `--include-snippet` when source context helps.

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
