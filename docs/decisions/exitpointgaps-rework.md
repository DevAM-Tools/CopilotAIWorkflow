# Decision — Make ExitPointGaps a ratchet on changed code, not a wall

**Status:** proposed

**Context:** ExitPointGaps was built to raise test coverage: `run` builds, tests with coverage, and fails while any exit point (`return`, `throw`, `yield break`, implicit end, expression arm) of a method was not reached (`summary.exitGapCount == 0`). In daily use it turned into a negative example:

- **Rigid.** Every exit of every method gates, including exits no test can reach in a sensible way: a `throw` that guards a broken invariant, an out-of-memory path, a platform branch, `Main`. The tool kept failing, and agents spent runs fighting it instead of testing behavior.
- **No way to exclude.** The only exclusion is `[ExcludeFromCodeCoverage]` on a type, method, accessor, or local function (`ExitPointExclusion`). It sits in production code, excludes the whole member instead of one exit, records no reason, and no report shows what is excluded. The other escape hatches (`--no-fail`, `--skip-no-tests`) switch the gate off as a whole.
- **Slow.** A run builds and tests every project again, with coverage: about 12 s in parallel on this small repository (README § Performance), on top of the test run the workflow already did.
- **Awkward.** Fifteen options, a work directory per run, and a summary (schema v3) whose findings sit in per-project report files the caller must load one by one.
- **Not wired.** No skill of the kit names the tool any more, but [README.md](../../README.md#exitpointgaps) still points to `tech-tunit.md` for its workflow.

[ReqTrace](spec-trace.md) was designed against these failures, and the same ideas carry over: only new or changed things block, old gaps are counted; exclusions live in a file with a reason; every finding says where and what to do; the tool reads results that already exist instead of producing them.

**Decision:** Rework ExitPointGaps from an all-or-nothing gate into a **ratchet on changed code**:

1. **Accepted state.** `exitpointgaps accept` writes `exitpoints.lock`: one line per uncovered exit, keyed by member and exit kind plus a text fingerprint, not by line number, so moving code does not reopen a gap. A gap in the lock is an open gap: counted and shown, never failing. Only an uncovered exit that is not in the lock fails the run. Accepting needs a person, like `reqtrace accept`.
2. **Scope to the change.** `--changed-since <ref>` gates only members whose text changed since a git ref (read-only `git diff`); everything else is reported as open. Default for agents in `/implement`: the working-tree change.
3. **Exclusions with a reason, outside production code.** `exitpoints.ignore`, one entry per line, like `spec/trace.ignore`:

   ```text
   src/App/Program.cs — process entry; covered by the CLI smoke test
   App.Parsing.Lexer.Next throw UnreachableException — guards a broken invariant
   ```

   Every exclusion is counted in the report. An entry that matches nothing is a finding, so the list cannot rot. `[ExcludeFromCodeCoverage]` keeps working.
4. **Exit classes with a policy.** Not every exit is a behavior. Default policy: a `throw` of a bug-guard type (`ArgumentNullException`, `ArgumentOutOfRangeException`, `UnreachableException`, `InvalidOperationException` built from a constant message), a `Debug.Fail` arm, and an implicit end after a `throw` are informational; returns, expected-failure exits (`Try…` returning `false`, result-type errors), and `yield break` gate. A project can tighten or loosen the policy per class in one settings file.
5. **Read, do not rerun.** By default, read the Cobertura files of the test run that already happened (`--cobertura` today, as a folder like `reqtrace check --results`); build and test only with `--run`. Cache the exit collection per source-file hash, so an unchanged file is not parsed again.
6. **Findings an agent can act on.** One line per finding: file and line, member, exit kind, the test class that is missing (error path, boundary, empty collection), and the fix: write the test, add an ignore entry with a reason, or accept. `--json` carries the same with `schemaVersion`. The summary holds the findings; per-project files become opt-in.
7. **Advisory in the kit.** The `/review` Tests hunt reads the report as a hint for missing behavior classes in changed code. It does not replace judging test content. `/implement` runs it on the changed scope. A person decides whether the project gates on it at all.

**Rejected:**

- Keep the gate as is — it punishes untestable exits and teaches agents to write tests for the tool instead of for behavior.
- Delete the tool — an uncovered `return` in changed code is still a cheap, useful hint for a missing test, and the exit collection is shared with CSC006 of CSharpStyleChecker.
- A coverage percentage threshold — a number says nothing about which behavior is untested, and it rewards trivial tests.
- Attributes in production code as the only exclusion — noisy, member-wide, without a reason, invisible in reports.
- Merge into ReqTrace — a different question ("is each exit of the code reached?" against "does each requirement reach a passing test?"); different inputs (Roslyn and coverage against Markdown and markers) and a different start-up cost.

**Consequences:**

- Easier: an agent fixes only what it touched; old debt is visible but does not block; untestable exits get a reason once and stop nagging; a run costs a read of existing coverage instead of a second test run.
- Harder: the lock file and the ignore file must be maintained; a fingerprint that is too loose hides a changed exit, one that is too strict reopens accepted gaps after formatting.
- Breaking for callers of 1.x: the default changes from "every exit gates" to "new and changed exits gate". That is a 2.0.0, with `--strict` keeping the old behavior.
- Watch: whether the default class policy hides real gaps. The review hunt still judges test content.
- Before the rework starts: the published ExitPointGaps 1.1.1 package carries seven stale files from before the rename (`CoverageGap.Tool.*`, `CoverageGapAnalysis.*`) that a dirty `bin/` leaked into it. [Directory.Build.targets](../../Directory.Build.targets) no longer packs dotnet tools during build, so a clean `dotnet pack` excludes them; a republish needs a new patch version.

**Next step:** `/requirements` in Delta mode on the area `GAPS` ([spec/requirements/gaps.md](../../spec/requirements/gaps.md) holds the exit-code pilot), one requirement per numbered point above, then `/plan`. Open for that round: the fingerprint of an exit; whether `--changed-since` defaults to the working tree or to the merge base; whether the class policy is a file or options.

**Enforced in:** nothing yet; this record is a proposal.
