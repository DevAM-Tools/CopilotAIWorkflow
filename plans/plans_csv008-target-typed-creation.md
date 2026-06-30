# Plan: CSV008 Target-Typed Creation (Release 2.1.0)

| Step | Delivers |
|------|----------|
| Step 1 — CSV008 analyzer core | `TargetTypedCreationAnalyzer` flags redundant explicit type in parameterless `new Type()` and non-`[]` collections/arrays |
| Step 1R — Review Step 1 | Zero Error findings; iterate until clean |
| Step 2 — CSV008 tests | TUnit coverage for violations, exemptions, and edge contexts |
| Step 2R — Review Step 2 | Zero Error findings; iterate until clean |
| Step 3 — Demo + release docs | Violation sample, README/CHANGELOG/AnalyzerReleases, `VersionPrefix` 2.1.0 |
| Step 3R — Review Step 3 | Zero Error findings; iterate until clean |
| Step 4 — tech-csharp skill update | Concise `new()` / `[]` rule aligned with CSV008 |
| Step 4R — Review Step 4 | Zero Error findings; iterate until clean |

## Summary / Context Anchor

**Goal:** Release 2.1.0 adds CSV008 — enforce target-typed `new()` and collection expressions `[]`. Primary target: parameterless `TypeA a = new TypeA()`. Constructor arguments stay allowed with explicit type. Object initializers with members stay exempt; empty `{}` does not. Collections and array literals must use `[]`, not `new List<T>()` / `new T[] { … }` / collection initializers. Namespace≠type naming: no skill change — covered by existing .NET built-in warning (Q5).

**Grill-Me resolved:** Q1 args exempt · Q2 empty `{}` report · Q3 only `[]` · Q4 arrays → `[]` where applicable · Q5 no skill namespace rule.

**Loaded skills:** `tech-csharp.md`, `tech-tunit.md`, `tech-solution.md`

**In-scope files (implementation):**

| Area | Files |
|------|-------|
| Analyzer | `src/CSharpStyleValidator/Analyzers/TargetTypedCreationAnalyzer.cs` (new) |
| Diagnostics | `DiagnosticIds.cs`, `DiagnosticDescriptors.cs` |
| Release tracking | `AnalyzerReleases.Unshipped.md`, `AnalyzerReleases.Shipped.md` |
| Tests | `TargetTypedCreationAnalyzerTests.cs` (new), `DiagnosticDescriptorsTests.cs` |
| Demo | `samples/CSharpStyleValidator.Demo/Violations/Csv008_TargetTypedCreation.cs` (new), `CompliantExample.cs`, demo `README.md` |
| Docs | `README.md`, `CHANGELOG.md`, `PACKAGE_README.md` (if rule table present) |
| Version | `Directory.Build.props` (`VersionPrefix` → `2.1.0`) |
| Skill | `.github/skills/tech-csharp.md` |

**Dependency order:** Step 1 → Step 2 → Step 3 → Step 4.

**Test matrix (behavior × tests):**

| Behavior | Expected | Test |
|----------|----------|------|
| `TypeA a = new TypeA()` | CSV008 | Yes |
| `TypeA a = new()` | clean | Yes |
| `Foo x = new Foo(1)` | clean (args exempt) | Yes |
| `new TypeA() { Prop = v }` | clean (object initializer exempt) | Yes |
| `new TypeA() { }` | CSV008 | Yes |
| `List<int> x = new List<int>()` | CSV008 | Yes |
| `List<int> x = []` | clean | Yes |
| `List<int> x = new List<int> { 1 }` | CSV008 → use `[1]` | Yes |
| `int[] a = new int[] { 1, 2 }` | CSV008 → use `[1, 2]` | Yes |
| `new int[10]` | clean (rank-sized; no `[]` equivalent) | Yes |
| `Base b = new Derived()` | clean | Yes |
| `IList<int> x = new List<int>()` | clean | Yes |
| `T t = new T()` | clean | Yes |
| `throw new ArgumentException()` | clean | Yes |
| Descriptor count | 8 unique Error descriptors | Yes |

## Target Solution (Vision)

```mermaid
graph TD
    A[ObjectCreationExpressionSyntax] --> B{Explicit type syntax?}
    B -->|No| Z[OK]
    B -->|Yes| C{Arguments present?}
    C -->|Yes| Z
    C -->|No| D{Object initializer with members?}
    D -->|Yes| Z
    D -->|No| E{Collection/array initializer?}
    E -->|Yes| F[CSV008 use []]
    E -->|No| G{Created type equals target?}
    G -->|No| Z
    G -->|Yes| H{Target-typed context?}
    H -->|No e.g. throw| Z
    H -->|Yes| I[CSV008 use new]
    J[ArrayCreationExpressionSyntax] --> K{Rank specifiers only?}
    K -->|Yes new int n| Z
    K -->|No| L{Array initializer?}
    L -->|Yes| F
```

CSV008 message (draft): *"Use target-typed `new()` or `[]` instead of explicit type `{0}`"* — Style, Error, `CSV008`.

`tech-csharp.md` (Step 4 only): strengthen existing bullet — ❗Use `new()` and `[]`; object initializers with members are exempt; constructor args may keep explicit type.

No new NuGet dependencies. Breaking change on 2.1.0 upgrade within `2.*`.

## Vertical Slices

Single slice: analyzer + tests + demo + docs + skill.

## Steps

## S1 - TargetTypedCreationAnalyzer (CSV008)

Status: ✅ Complete · Blocks S2–S3

### What
Roslyn analyzer CSV008 for redundant explicit types in parameterless `new Type()`, empty `{}`, and non-`[]` collection/array creations.

### Why
Closes gap after CSV002; matches Grill-Me decisions and original property-initializer exception.

### How
- Add `DiagnosticIds.TargetTypedCreation = "CSV008"`, descriptor, append to `All`.
- `TargetTypedCreationAnalyzer`:
  - Register `ObjectCreationExpression` and `ArrayCreationExpression`.
  - **Args exempt (Q1):** if `ArgumentList?.Arguments.Count > 0`, return.
  - **Object initializer exempt:** `ObjectInitializerExpressionSyntax` with `Expressions.Count > 0` → return.
  - **Empty `{}` (Q2):** `ObjectInitializerExpressionSyntax` with zero expressions → report.
  - **Collection initializer (Q3):** `CollectionInitializerExpressionSyntax` on object creation → report (use `[]`).
  - **Arrays (Q4):** on `ArrayCreationExpressionSyntax`, if initializer present (no rank-only `new int[n]`) → report; rank-only exempt.
  - **Type-mismatch exempt:** `Derived`/`Base`, `List<int>`/`IList<int>`, type parameter `T`.
  - **No-context exempt:** `throw new X()`, etc.
  - Report on explicit type syntax span.
- `AnalyzerReleases.Unshipped.md` entry.

### Where
- `src/CSharpStyleValidator/Analyzers/TargetTypedCreationAnalyzer.cs` (new)
- `src/CSharpStyleValidator/Diagnostics/DiagnosticIds.cs` (~L28)
- `src/CSharpStyleValidator/Diagnostics/DiagnosticDescriptors.cs` (~L81)
- `src/CSharpStyleValidator/AnalyzerReleases.Unshipped.md`

### Verify
`dotnet build src/CSharpStyleValidator/CSharpStyleValidator.csproj -c Release` — 0 warnings/errors.

### If it fails
Narrow to declaration/assignment contexts first; expand after tests pass.

---

## S2 - CSV008 analyzer tests

Status: ✅ Complete · Depends on S1

### What
TUnit tests per test matrix.

### Why
Release gate; follows existing analyzer test patterns.

### How
- `TargetTypedCreationAnalyzerTests.cs` with all matrix cases.
- `DiagnosticDescriptorsTests` → 8 descriptors.

### Where
- `src/CSharpStyleValidator.Tests/Analyzers/TargetTypedCreationAnalyzerTests.cs` (new)
- `src/CSharpStyleValidator.Tests/Diagnostics/DiagnosticDescriptorsTests.cs` (~L15)

### Verify
`dotnet test src/CSharpStyleValidator.Tests/CSharpStyleValidator.Tests.csproj -c Release` — all pass.

---

## S3 - Demo, docs, version 2.1.0

Status: ✅ Complete · Depends on S1, S2

### What
Demo violation, public docs, `VersionPrefix` 2.1.0.

### How
- `Violations/Csv008_TargetTypedCreation.cs` — e.g. `List<int> items = new List<int>();`
- Update demo README, root README (CSV001–CSV008), CHANGELOG `## 2.1.0`
- Ship CSV008 in `AnalyzerReleases.Shipped.md` under `## Release 2.1.0`
- `Directory.Build.props` → `2.1.0`

### Where
- `samples/CSharpStyleValidator.Demo/Violations/Csv008_TargetTypedCreation.cs`
- `samples/CSharpStyleValidator.Demo/README.md`, `README.md`, `CHANGELOG.md`, `Directory.Build.props`

### Verify
```bash
dotnet build CopilotAIWorkflow.slnx -c Release
dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release -p:IncludeViolations=true
```

---

## S4 - tech-csharp skill instructions

Status: ✅ Complete · Parallel S3

### What
One concise skill bullet for `new()` / `[]` aligned with CSV008.

### Why
Document rule for agents; namespace≠type omitted — built-in .NET warning (Q5).

### How
- Strengthen **Naming** or **Style** bullet near existing `new()` / `[]` line:
  - ❗Use `new()` and `[]` instead of repeating the type; object initializers with members exempt; constructor args may use explicit type.
- No namespace bullet.

### Where
`.github/skills/tech-csharp.md` (~L10)

### Verify
Manual review — one strengthened bullet, no bloat.

---

## Edge Cases and Risks

| Risk | Mitigation |
|------|------------|
| `throw new X()` false positive | No-context exemption |
| Polymorphic construction | Type-mismatch exemption |
| `new int[n]` | Rank-only exempt (Q4) |
| `new Foo(1, 2)` with explicit type | Args exempt (Q1) |
| Consumer break on 2.* upgrade | CHANGELOG note |
| C# 12 `[]` required | Repo C# 14 — OK |

## Decisions & Trade-offs

| ID | Choice | Source |
|----|--------|--------|
| C1 | `new Foo(args)` exempt when args present | Q1 |
| C2 | `new Foo() { }` → CSV008 | Q2 |
| C3 | `new List<int> { 1 }` → CSV008; use `[1]` | Q3 |
| C4 | `new int[] { … }` → `[]`; `new int[n]` exempt | Q4 |
| C5 | `new Type() { Prop = v }` exempt | Original + Q2 |
| C6 | No namespace skill rule | Q5 — built-in warning |
| C7 | `IList` / `Derived` / `throw` exemptions | Semantic |

## Open Questions

None — Grill-Me complete.

## Closing Summary

Release 2.1.0: CSV008 analyzer + one `tech-csharp.md` bullet. No namespace skill rule. ~16 tests, demo, docs, version bump.

## Task Checklist

- [x] Step 1 — CSV008 analyzer core
- [x] Step 1R — Review Step 1
- [x] Step 2 — CSV008 tests
- [x] Step 2R — Review Step 2
- [x] Step 3 — Demo + release docs
- [x] Step 3R — Review Step 3
- [x] Step 4 — tech-csharp skill update
- [x] Step 4R — Review Step 4
