# Decision — Machine-checkable links between requirements, test content, and tests

**Status:** accepted. The maintainer decided Go after the pilot on ExitPointGaps (see Verdict).

**Context:** `spec/` connects requirements, test content, tests, and code, but only by hand-written text. Those references rot, and an agent that works sloppily leaves no trace. Agents do better when something outside them can verify the work.

ExitPointGaps is the cautionary example. It measures something real, yet it became tiresome: it demands a test for every exit point including ones that cannot sensibly be tested, it is slow because it builds and runs everything, exceptions exist only as a coarse attribute in production code, and it reports the whole code base regardless of what changed.

**Decision:** A small command-line tool, `reqtrace`, checks structure and links. Verification has six layers, and each handles only what it can decide:

| Layer | Question | Decided by |
|-------|----------|------------|
| 1 Form | Can every line be read? | Tool, strict |
| 2 Links | Does every reference resolve? | Tool, strict |
| 3 Change | What differs from the last accepted state? | Tool, strict |
| 4 Evidence | Did the marked test really run and pass? | Tool, from result files |
| 5 Meaning | Does this test still prove this requirement? | Fresh reviewer, only for the pairs layer 3 named |
| 6 Decision | Is this change of meaning wanted? | A person, from a generated report |

- **Requirement text stays natural language.** Formalize structure, not meaning.
- **Closed-world reading.** Every line in a spec area must match a known form. An unreadable line is a finding, so a malformed entry can never silently disappear.
- **One marker per test, nothing in production code.** Test → code is measured, not written down, in a later stage.
- **`spec/trace.lock` is the accepted snapshot and the baseline.** One line per entry with a hash. Text, sorted, reviewable in git. Gaps recorded at the first accept never block; a new gap needs a stated reason.
- **Requirement changes open a to-do.** Any changed text marks its test cards unconfirmed until someone confirms them with a note.
- **The tool's report comes from the files, not from the agent's summary.**

**What the tool must do better than ExitPointGaps:**

| ExitPointGaps | reqtrace |
|---------------|-----------|
| Everything must be tested | Proof kinds: test, demo, review, waived with a reason |
| Reports the whole code base | Only the current change blocks; old gaps are a count |
| Exceptions only as a production attribute | Reason on the spec entry; ignore file with reasons; all listed in the report |
| Builds and runs tests | Reads files only; at most 200 ms |
| Agent fights the tool | Every finding names location, problem, and fix; two failed attempts become a decision card |

**Rejected:**

- A database as the source of truth: loses diff, merge, and review in git. A generated cache remains possible.
- A Roslyn analyzer: C#-only and no gain over text search for this job. It could later give live IDE feedback.
- Attributes on production code: the bloat that rots first.
- IDs in test code rejected earlier ([requirements-driven-loop](requirements-driven-loop.md)): reversed for test code only, because a marker survives renames while a path in Markdown does not.
- A separate baseline file: the lock already records the accepted state.
- Formalizing requirement meaning: too expensive, and it moves the ambiguity instead of removing it.
- Native AOT: built and measured at about 24 ms instead of 80 ms. It cannot compile for another operating system, so Linux and macOS could not be built from a Windows machine, and it linked a runtime with a known vulnerability until the SDK catches up. A single framework-dependent build avoids both.

**Performance:** every call costs an agent time, so the tool parses with spans instead of regular expressions, rejects files with a vectorized byte search before reading lines, scans in parallel above 128 files, and buffers its output.

**No limit is set on input size.** Every input is a file in the operator's own repository and is read as a stream; no third party supplies it. Class `UNBOUNDED` in the sense of `tech-limits.md`.

**Consequences:** The agent can still mark the wrong test, write a test that asserts nothing, or accept its own change. Layer 5 and a person cover those. A check only counts if something other than the agent runs it; for the pilot, that is the person before committing.

**Verdict of the pilot:**

| Criterion | Target | Measured | Result |
|-----------|--------|----------|--------|
| Sabotages a machine can catch | All | 10 of 10 fail and name the entry; the 2 no tool can catch pass, as designed | Met |
| `check` on this repository | ≤ 200 ms | About 80 ms per run on Windows and 40 ms on Linux, including process start (20 runs each) | Met |
| Blocks from unrelated entries | 0 | 0, while seven other ExitPointGaps files changed concurrently | Met |
| False alarms left open | 0 | 2 found, both fixed: a title line read as a broken ID; attributes counted against the method window | Met |
| Findings fixed first try from the message alone | ≥ 80 % | Not meaningfully measured: too few real findings, and the author judged its own tool | Open |

Go, with the last row watched during real use. Tests pass on Windows x64 and Linux x64; macOS and ARM64 have not run.

**Enforced in:** [`src/ReqTrace/`](../../src/ReqTrace/); format in [`tech-spec.md`](../../.github/skills/tech-spec.md) § Trace; behaviour in [`spec/requirements/trace.md`](../../spec/requirements/trace.md).
