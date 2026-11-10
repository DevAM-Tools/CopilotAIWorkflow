# Using reqtrace as an agent

`reqtrace` checks the links between requirements (`spec/requirements/`), test cards (`spec/tests/`), and tests. It reads files only; it builds nothing and runs nothing. Run it from anywhere inside the repository. Format of the spec: [`tech-spec.md`](../.github/skills/tech-spec.md). Setup, which command runs at which workflow moment, and what to do per finding: [`tech-reqtrace.md`](../.github/skills/tech-reqtrace.md).

Sync: [`tech-reqtrace.md`](../.github/skills/tech-reqtrace.md) restates the commands, exit codes, rules, and output for repositories that copy the kit without this file.

## Install

The NuGet package `ReqTrace` is a dotnet tool. It needs the .NET 10 SDK, also in a Rust or Python project.

```bash
dotnet new tool-manifest            # once per repository
dotnet tool install ReqTrace --version 0.*
dotnet tool run reqtrace check      # or: dotnet reqtrace check
```

In this repository, run the source instead: `dotnet run --project src/ReqTrace -c Release -- check`.

## Commands

```text
reqtrace [check] [--results <file-or-folder>] [--json]
reqtrace show <ID>
reqtrace report
reqtrace accept [--allow-gap "<reason>"]
reqtrace confirm <TEST-ID> --note "<what was checked>"
```

Exit codes: `0` no error · `1` at least one error · `2` usage or unreadable input. `--results` is relative to the repository root.

| Command | Use it |
|---------|--------|
| `check` | After every change to the spec or to tests |
| `show` | Before changing a requirement or test: what depends on it |
| `report` | At a gate: paste its output, never a summary of it |
| `accept` | Only after a person approved the state |
| `confirm` | After checking that a card still proves a changed requirement |

## Three worked calls

```text
> reqtrace show REQ-GAPS-EXIT-GAP
REQ-GAPS-EXIT-GAP — The tool shall exit with code 1 when an exit-point gap remains.
  Card: TEST-GAPS-EXIT-GAP
    Test: src/ExitPointGaps.Tests/Cli/GapRunCliIntegrationTests.cs:38 RunProject_PartialCoverage_FailsGate

> reqtrace check
spec/requirements/export.md:14  F1  This line matches no known form.
  Fix: Write a field such as "  - **Done when:** …", or move this text above the first ### heading.
Result: 1 error · 0 open gaps

> dotnet test src/App.Tests -c Release -- --report-trx --results-directory artifacts/trace-results
> reqtrace check --results artifacts/trace-results
Result: 0 errors · 3 open gaps
```

## Test markers

Each test that proves a card carries its ID, once. Production code never does.

```text
C#:     [Test, Property("Card", "TEST-AREA-TOPIC")]
Python: @pytest.mark.card("TEST-AREA-TOPIC")
Rust:   // card: TEST-AREA-TOPIC
```

## JSON output

`check --json` prints one object: `schemaVersion` (1), `evidenceChecked`, `hasAcceptedState`, `counts` (`requirements`, `testCards`, `markers`, `ignoredPaths`), and `findings`, each with `rule`, `severity` (`error` or `openGap`), `file`, `line`, `message`, `fix`.

## Rules

| Rule | Meaning |
|------|---------|
| F1–F5 | Form: unreadable line, malformed ID, missing field, bad value, file with no entry |
| L1–L7 | Links: duplicate, unknown requirement, unknown card, no card, marker without method, no marker, bad ignore entry |
| C1–C3 | Change: vanished entry, removed marker, card not confirmed |
| E1–E4 | Evidence: not run, failed, stale result, ambiguous method |

Only the current change blocks: a gap in an unchanged entry is an open gap. Two failed attempts on one finding become a decision card.

## Accepted state

`spec/trace.lock` is the state a person accepted, as sorted text. Never edit it by hand.

## Ignoring a path

`spec/trace.ignore`, one per line, reason required: `path/inside/repo — reason`.
