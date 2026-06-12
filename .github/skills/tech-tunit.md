# TUnit Testing Rules

Load when test files or test projects are in scope. Extends Section 4.5 in `copilot-instructions.md`.

## Framework

- Use TUnit
- Make every test method `async Task`.
- Use NSubstitute for doubles.
- Enforce Coverlet branch coverage: `--threshold 100 --threshold-type branch`.

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
- Substitute only uncontrollable dependencies.
- Prefer outcome assertions over interaction counts.
- Never relax access modifiers for tests only.
- Document `[ExcludeFromCodeCoverage]` with XML reason.
