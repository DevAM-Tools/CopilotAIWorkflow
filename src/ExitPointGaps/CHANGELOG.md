# ExitPointGaps changelog

All notable changes to the **ExitPointGaps** local `dotnet` tool are documented here.
Repository-wide and agent-workflow notes live in the root [CHANGELOG.md](../../CHANGELOG.md).
Bundled exit-point collection lives under [`src/ExitPoints`](../ExitPoints/); consumer-visible collection changes that affect this tool are listed below.

## 1.1.0

### CSC009 alignment

- Interpolated strings and culture-sensitive messages use `Invariant($"…")` (`global using static System.FormattableString`) so the tool builds cleanly under **CSharpStyleChecker** 1.1.0 / CSC009.

### Exit-point collection (tool dependency)

- `ExitPointEntry` is a `readonly record struct`.
- `ExitPointCollector.Collect` accepts an optional `CancellationToken`.
- Conditional operator groups use `ConditionalExpressionSyntax.QuestionToken`.

### Internals

- Analysis helpers such as `CoberturaReader` and `PathNormalizer` are `internal` (the product is the CLI tool, not a library package).
- Cobertura branch-condition parsing skips non-branch element bodies and reuses empty condition lists.
- Production sources use `#region` layout; CLI error strings are invariant-formatted.

CLI surface (`run` / `plan`, `--max-parallelism`, `--stream` / `--no-stream`, schema v3 summaries) is unchanged from 1.0.0.

Example:

```bash
dotnet tool run exitpointgaps -- run --repo-root . --configuration Release --format agent
```

## 1.0.0

### Performance

- **Per-project parallelism** (default: all projects in scope concurrently); cap with `--max-parallelism <n>`
- **Solution pre-build** once before multi-project loops (skipped with `--no-build` or single project)
- **Streaming output:** aggregated `summary.json` is **schema v3** with `reportFile` indirection; per-project reports remain schema v1
- **Memory:** drop Roslyn `Compilation` after exit collection; scoped Cobertura parse; incremental per-project writes
- **Pipeline overlap:** Cobertura parse runs in parallel with Roslyn compile per project

### Breaking changes (aggregated JSON only)

| v2 | v3 |
|----|-----|
| `schemaVersion: 2` with embedded `report` objects | `schemaVersion: 3` with `projects[].reportFile` |
| Single-file multi-project `-o out.json` | Multi-project requires directory `-o reports/` or stdout `--stream` |

Migration: read `summary.json` for counts; load each `{reportFile}` for `exitGaps[]`.

### CLI

- `--max-parallelism <n>` — bounded concurrency (`n >= 1`)
- `--stream` / `--no-stream` — NDJSON stdout for multi-project runs without `-o`

### Rebrand

- **ExitPointGaps** replaces `CoverageGap.Tool` + `CoverageGapAnalysis` (analysis integrated into the tool assembly).
- CLI: `dotnet tool run exitpointgaps` (was `coveragegap`).
