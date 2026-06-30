# Review: CSV008 Release 2.1.0 (staged changes)

| ID | Bucket | Title | Summary |
|----|--------|-------|---------|
| E1 | E | Exit-point gate fails | `exitGapCount == 2` in `TargetTypedCreationAnalyzer.cs` lines 35 and 81 |
| E2 | E | Untested contextual paths | Return/assignment/argument resolution in `GetContextualTargetType` has no behavioral tests |
| E3 | E | Plan vs skill drift | `tech-csharp.md` bullet omits exemptions documented in plan and CHANGELOG |
| E4 | E | Instruction typo | `copilot-instructions.md` contains `Recude` instead of `Reduce` |
| C1 | C | Missing trailing newlines | `AnalyzerReleases.Shipped.md`, `SolutionParser.cs`, `ProjectReferenceScanner.cs` |
| C2 | C | Demo cross-rule noise | `Csv005_GlobalUsings.cs` also triggers CSV008 when all violations are built |
| C3 | C | CompliantExample FQN | Demo uses fully qualified `System.Collections.Generic` / `System.Text` types |
| R1 | R | Redundant null guards | `creation.Type is null` / `typeSyntax is null` branches appear unreachable |
| P1 | P | `ToString()` in Report | `typeSyntax.ToString()` allocates per diagnostic — deferred per review |

## Summary

Staged work delivers **CSharpStyleValidator 2.1.0** with new rule **CSV008** (target-typed `new()` / `[]`), 17 analyzer tests, demo sample, docs, and repo-wide mechanical style alignment (`new()` / `[]`) across CoverageGap and ExitPoints projects. Release build, 273 unit tests, and exit-point gate all pass (`exitGapCount: 0`).

**Loaded skills:** `tech-csharp.md`, `tech-tunit.md`, `tech-solution.md`

## Scope

**In scope:** All 30 staged files — CSV008 analyzer/tests/docs, version `2.1.0`, `tech-csharp` / `copilot-instructions` tweaks, CoverageGap/ExitPoints `new()`/`[]` style pass, plan artifact.

**Out of scope:** Unstaged files, NuGet publish, `dotnet tool` manifest setup.

**Verification run:**
- `dotnet build -c Release` — 0 warnings/errors
- `dotnet test -c Release` — 270/270 pass
- `dotnet run --project src/CoverageGap.Tool -c Release -- run --repo-root . --configuration Release --format agent` — `gatePassed: false`, `exitGapCount: 2`

## Errors

## E1 - Exit-point coverage gate not satisfied
Status: ✅ Fixed · Severity: High
### What
`CSharpStyleValidator` production project reports `exitGapCount: 2` after CSV008 addition. Uncovered exits: `AnalyzeObjectCreation` line 35 (`Return`) and `AnalyzeArrayCreation` line 81 (`Return`) — defensive `type is null` early-return branches.
### Why
Section 4.5 requires `exitGapCount == 0` before public release. Plan checklist marks Step 2R complete despite gate failure.
### How
- **Option A (preferred):** Remove unreachable `|| creation.Type is null` / `typeSyntax is null` guards if Roslyn syntax guarantees non-null; re-run gate.
- **Option B:** Add `[ExcludeFromCodeCoverage]` with XML reason on the null-guard branches only (match `PrivateNamingAnalyzer` pattern for unreachable defensive paths).
- **Option C:** Add analyzer tests that synthesize null-type syntax nodes (likely impractical).
- Re-run: `dotnet run --project src/CoverageGap.Tool -c Release -- run project src/CSharpStyleValidator/CSharpStyleValidator.csproj --repo-root . --configuration Release --format agent`
### Where
`src/CSharpStyleValidator/Analyzers/TargetTypedCreationAnalyzer.cs` — lines 33–35, 78–81
### Verify
`dotnet run --project src/CoverageGap.Tool -c Release -- run --repo-root . --configuration Release --format agent` → `summary.exitGapCount == 0` and `summary.gatePassed == true`
### If it fails
Inspect `exitGaps[]` for remaining IDs; ensure `ExcludeFromCodeCoverage` is on methods, not whole analyzer class.

## E2 - Contextual target-type paths lack behavioral tests
Status: ✅ Fixed · Severity: Medium
### What
`GetContextualTargetType`, `GetTypeFromArgument`, `GetEnclosingMethodReturnType`, and assignment handling are marked `[ExcludeFromCodeCoverage]` but have **no** analyzer tests exercising return, assignment, or argument contexts (e.g. `return new TypeA();`, `x = new TypeA();`, `M(new TypeA())`).
### Why
Excluded helpers bypass the exit gate; without tests, regressions in semantic resolution would ship silently. Plan vision includes return/argument contexts in `GetContextualTargetType` remarks.
### How
Add tests in `TargetTypedCreationAnalyzerTests.cs`:
- `TargetTypedCreation_ExplicitTypeInReturn_ReportsCsv008` — `return new TypeA();` with return type `TypeA`
- `TargetTypedCreation_ExplicitTypeInAssignment_ReportsCsv008` — `x = new TypeA();`
- Optional: argument context with typed parameter
- Keep exemption tests for polymorphic/interface/throw paths
### Where
`src/CSharpStyleValidator.Tests/Analyzers/TargetTypedCreationAnalyzerTests.cs`
### Verify
`dotnet test src/CSharpStyleValidator.Tests/CSharpStyleValidator.Tests.csproj -c Release` — all pass; manual review that new tests fail if `GetContextualTargetType` return branch is removed.

## E3 - tech-csharp skill incomplete vs plan and CHANGELOG
Status: ✅ Fixed · Severity: Low
### What
Plan Step S4 and CHANGELOG list CSV008 exemptions (constructor args, object initializers with members, polymorphic targets, etc.). Staged `tech-csharp.md` only says: *"Use `new()` / `[]` instead of repeating the type"*.
### Why
Agent instructions must match shipped analyzer behavior; undocumented exemptions cause agents to "fix" exempt code or miss valid violations.
### How
**Before:** `❗Never var. Use new() / [] instead of repeating the type;`
**After:** `❗Never var. Use new() / [] instead of repeating the type; constructor args may keep explicit type; object initializers with members are exempt; polymorphic/interface/throw/rank-sized arrays exempt per CSV008.`
### Where
`.github/skills/tech-csharp.md` — Naming section (~L10)
### Verify
Manual diff against `CHANGELOG.md` §2.1.0 exemptions and `TargetTypedCreationAnalyzer` logic.

## E4 - Typo in always-on copilot instructions
Status: ✅ Fixed · Severity: Low
### What
Section 4.4 Performance bullet reads `Recude GC runs` instead of `Reduce`.
### Why
Always-on instructions are SSOT for agents; typos reduce trust and may propagate.
### How
**Before:** `❗ Minimize allocations where ever possible. Recude GC runs to a minimum.`
**After:** `❗ Minimize allocations wherever possible. Reduce GC runs to a minimum.`
### Where
`.github/copilot-instructions.md` — Section 4.4 (~L103)
### Verify
`rg "Recude|where ever" .github/` — no matches.

## Cosmetic Issues

## C1 - Missing final newlines
Status: ✅ Fixed
### What
Files end without POSIX trailing newline.
### Why
Repository consistency; some diff tools flag `\ No newline at end of file`.
### How
Add single trailing newline to each file.
### Where
`src/CSharpStyleValidator/AnalyzerReleases.Shipped.md`, `src/CoverageGap.Tool/SolutionParser.cs`, `src/CoverageGap.Tool/ProjectReferenceScanner.cs`
### Verify
`git diff --check` on staged files — no `new blank line at EOF` warnings.

## C2 - Demo Csv005 cross-triggers CSV008
Status: ✅ Fixed
### What
`Csv005_GlobalUsings.cs` uses `new List<int> { 1 }`, which now also reports CSV008 when `IncludeViolations=true`. Full demo build shows 9 errors (CSV005 + CSV008 on same file).
### Why
Demo README lists one rule per violation file; cross-hits may confuse users validating a single rule.
### How
Change Csv005 sample to a pattern that does not violate CSV008 (e.g. `List<int> values = [1];` still demonstrates CSV005 via file-level `using`).
### Where
`samples/CSharpStyleValidator.Demo/Violations/Csv005_GlobalUsings.cs` (~L12)
### Verify
`dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release -p:IncludeViolations=true` — Csv005 file triggers only CSV005.

## C3 - CompliantExample uses fully qualified types
Status: ✅ Fixed
### What
`CompliantExample.cs` uses `System.Collections.Generic.List<int>` and `System.Text.StringBuilder` instead of types available via expanded `GlobalUsings.cs`.
### Why
Demo compliant sample is less readable; inconsistent with CSV005 message about usings in `GlobalUsings.cs`.
### How
Add `global using System.Collections.Generic;` and `global using System.Text;` to demo `GlobalUsings.cs`; shorten field/local types to `List<int>` and `StringBuilder`.
### Where
`samples/CSharpStyleValidator.Demo/CompliantExample.cs`, `samples/CSharpStyleValidator.Demo/GlobalUsings.cs`
### Verify
`dotnet build samples/CSharpStyleValidator.Demo/CSharpStyleValidator.Demo.csproj -c Release` — success, no CSV005/CSV008.

## Refactoring Opportunities

## R1 - Simplify unreachable null-type guards
Status: ✅ Fixed
### What
`AnalyzeObjectCreation` and `AnalyzeArrayCreation` check `creation.Type is null` / `typeSyntax is null` after pattern matching.
### Why
Valid C# parse trees always include type syntax for these node kinds; guards add dead branches (see E1) without clarity benefit.
### How
Narrow pattern to `is ObjectCreationExpressionSyntax { Type: { } type }` or drop null check after `is not` binding; behavior unchanged for valid code.
### Where
`src/CSharpStyleValidator/Analyzers/TargetTypedCreationAnalyzer.cs` — `AnalyzeObjectCreation`, `AnalyzeArrayCreation`
### Verify
Same as E1 — gate passes; existing 14 CSV008 tests still pass.

## Performance and Allocations

## P1 - Type name via ToString()
Status: ⬜ Deferred · acceptable per review Context
### What
`Report` uses `typeSyntax.ToString()` for the diagnostic message argument.
### Why
Section 4.4 prefers minimizing allocations; analyzers run on every compilation but this is cold relative to hot paths.
### Context
Fires once per violation site, not per syntax node visit; acceptable for style analyzer unless profiling shows impact.
### How
If measured: use `semanticModel.GetSymbolInfo(typeSyntax).Symbol?.ToDisplayString()` or a span-based formatter; otherwise defer.
### Where
`src/CSharpStyleValidator/Analyzers/TargetTypedCreationAnalyzer.cs` — `Report` (~L87–93)
### Verify
No action required unless benchmark shows regression; optional `dotnet test` unchanged.

## Closing Assessment

**Architecture:** CSV008 follows established analyzer patterns (`AnalyzerGuard`, descriptor registry, Roslyn syntax/semantic split, `[ExcludeFromCodeCoverage]` on helpers). Version bump to `2.1.0` is coherent for a new compiler-error rule. Mechanical `new()`/`[]` pass across CoverageGap/ExitPoints is behavior-neutral.

**Dominant themes:** Release gate (exit gaps), test coverage gaps for excluded semantic paths, minor doc/skill drift.

**Thread safety:** Analyzer uses `EnableConcurrentExecution()`; static methods are stateless — appropriate.

**Allocation profile:** No hot-path concerns; single `ToString()` per reported diagnostic.

**Release verdict:** **Ready for public release** — all Error and Cosmetic findings resolved; P1 deferred.

## Priority Action List

1. ~~**E1**~~ — Done: direct cast per registered syntax kind; gate passes.
2. ~~**E2**~~ — Done: return, assignment, argument tests added.
3. ~~**E3**~~ — Done: `tech-csharp.md` and `CHANGELOG.md` aligned.
