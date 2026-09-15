# Implement Workflow

Load on `/implement`. Apply `copilot-instructions.md` for all quality, tech, git, and communication rules.

**Purpose:** Implement approved plan steps or accepted review findings **exactly**. Complete every item in scope. Close with an **extensive briefing**, then a council Exam of the built result.

## Stage Order

1. Prepare
2. Execute Steps
3. Final Verification
4. Briefing
5. Closing Exam

## Stage 1 — Prepare

- Require approved plan or accepted review findings per `copilot-instructions.md` Section 6; stop otherwise.
- ❗ Read the **entire** plan (or finding file) **before the first edit**. Do not skim Step Overview and skip `How`. Read the requirements artifact and any linked architecture / guide / doc **in full**.
- Run Tech Load Protocol per Section 3.
- Resume at first `⚠️`, else first `⬜`.

## Stage 2 — Execute Steps

- **Checklist status:** `⚠️` before first edit · `✅` after Verify, alignment, requirement/test ticks, Step NR clean.
- Tick **all three** plan surfaces together: Step Overview, Shared Block, Task Checklist.
- Process **every** checklist step, dedicated test step, and finding in scope; skip none.
- Follow topological order per plan dependencies.
- Implement **only** current step or finding scope — match `What` and `How` exactly, including Before/After.
- Do not substitute, simplify, or extend beyond scope without user approval.
- Implement named `T{n}` cases listed on the step (add or run). Mark those rows `✅` when they exist and can fail.
- Run `Verify` from the plan step or finding; require pass. On failure follow Section 4.15 and the step `Debug` block. Consider concurrent-agent collision (Section 4.14).
- **Alignment check:** plan/finding target vs actual; no silent deviation.
- **Requirements check (step close):** re-read Requirements (User View). Run Done-when for rows still `⬜` that this step could have made true. Tick `Met` only when the check **ran**. Do not tick because `How` alluded to an outcome. Do not write requirement IDs into code or comments.
- Confirm step `Acceptance` checkboxes.
- Confirm misuse/abuse checklist from the plan when new public APIs are in scope.
- Confirm docs (README, guides, architecture, package docs) still match behavior and commands (Section 1).
- Run `/review` at each Step NR; zero Errors before next step.
- Persist review file in complex-task mode per `workflow-complex-task.md`.
- After two failed remediations for the same Error root cause: exponential backoff then retry, or stop with blocker.

## Stage 3 — Final Verification

- Confirm **every** scoped step, `T{n}`, and finding is `✅`; leftover `⬜` / `⚠️` = blocker.
- Re-run alignment: every `R{n}` Met ✅, every `T{n}` ✅, or all accepted findings resolved.
- Run full build and all tests in optimized/Release. Commands from the loaded tech skill.
- For web UI: run the planned Playwright journeys.
- Output Implementation Status Table (every step / finding / `T{n}` listed).
- Do not enter Stage 4 until this stage is green.

## Stage 4 — Briefing

Write the briefing before Stage 5. Exam skipped → still write it. No complete without it. Chat: path only.

Call this artifact a **briefing**, never a “review brief”. `/review` is a different workflow (adversarial review; default includes test execution).

The briefing is the human packet for what shipped. It must be **extensive**: a reader who did not watch implementation should know what shipped, why it had to, how to try it, what to inspect in each file, which acceptance checks now hold, and what is out of scope. English (Section 4.6). ❗ Incomplete without contribution, how to try the result, and what to inspect. Do not compress **How it serves** / **Why needed** / header **Why**.

- Path: `reviews/briefing_<slug>.md`. Single item: `reviews/briefing_<slug>_<item>.md` (`step{n}` / finding ID). Before Exam: full-scope file.
- List every created, edited, or deleted path. Omit none. Rewrite on remediation.
- Built result. Name symbols, behavior, contracts. Include small illustrative snippets where they help (signatures, usage). Not a raw diff dump.
- Header fields below are required. Expand until a reader can act without reconstructing the argument.
- `R{n}` / `T{n}` / `E{n}` links belong in the briefing (reader aid). Do not copy those IDs into product code.
- Existing heading: linked backtick path. Deleted: backtick path + `(deleted)`. Repo-root path, forward slashes. Sibling files may share one card.
- Field order per card: **Changed** → **Why needed** → **How it serves** → **Look at** → **Depends on** → **Serves**.
- **How it serves:** per linked `R{n}` / `T{n}` / `E{n}`, full sentences: which symbols, what exists or is gone, what a caller can or cannot do, which Done-when this file owns. Unique to this file. Ban slogans, ID-only, “as planned”, empty purpose.
- **Look at:** where in the file a reader should spend time (type, method, invariant).
- Order for reading: contracts/types → implementations → cutover → tests → docs.

```markdown
# Briefing — {scope}

**Expect:** {outcome}
**Done when:** {checks the reader can run}
**Why:** {problem without this work; what stays wrong if it does not ship}
**Out:** {exclusions}

**Try this:** {exact commands, Playwright spec, or usage snippet; expected observable}

**Acceptance evidence:**
- R1 ({short outcome}): {command/UI/API and what was observed} · [R1](../requirements/req_{slug}.md)
- T1 ({case}): {test filter or spec and result}

**Public API:** {short snippet or "unchanged"}

**Performance:** {hot-path allocation notes, or "not in hot path"}

**Requirements:** [R1](../plans/plans_{slug}.md#requirements-user-view) {short outcome} · [R2](../plans/plans_{slug}.md#requirements-user-view) {short outcome}

**Test cases:** [T1](../plans/plans_{slug}.md#test-cases) {case} · [T2](../plans/plans_{slug}.md#test-cases) {case}

## Walkthrough

1. {Open this file; confirm this symbol / AC}
2. {Then this}

## Files

1. [`{path}`](../{path})
   - **Changed:** {symbols/behavior}
   - **Why needed:** {what fails without this file — several sentences if needed}
   - **How it serves:** [R1](../plans/plans_{slug}.md#requirements-user-view): {causal contribution unique to this file}. [T1](../plans/plans_{slug}.md#test-cases): {how this file makes the case possible}
   - **Look at:** {symbol and what to verify}
   - **Depends on:** —
   - **Serves:** [R1](../plans/plans_{slug}.md#requirements-user-view), [T1](../plans/plans_{slug}.md#test-cases)

2. `{path}` (deleted)
   - **Changed:** {what went away}
   - **Why needed:** {what the deleted type blocked or enabled wrongly}
   - **How it serves:** {type is gone; callers must use …; Done-when this deletion closes}
   - **Look at:** callers listed in Depends on / Serves
   - **Depends on:** 1
   - **Serves:** [R1](../plans/plans_{slug}.md#requirements-user-view)
```

## Stage 5 — Closing Exam

- Load `workflow-council.md`. Run **Exam** on the built result (plan `R{n}`, `T{n}`, briefing, touched files, tests, latest Step NR reviews). Same agent; no subagents.
- Skip Exam when parent is `workflow-complex-task.md` Stage 3 per-item or `workflow-review-loop.md` Stage 2. Still run Stage 4.
- User `quick`/`lite` → Lite Exam (Skeptic addendum still required). Else Full Exam.
- Chairman **Holds** → implement complete. Cite the briefing and the exam artifact.
- Chairman **Does not hold** → remediate kill shots and §4 violations (Verify, alignment, two-attempt cap). Re-run Stage 3 for touched scope, rewrite Stage 4, then re-Exam `_<n>`.
- Grill Me follow-ups → ask user; do not mark implement complete.
- Do not treat Exam as `/review`. Do not skip Step NR because Exam will run.

## Implementation Status Table

```markdown
| Step / Finding | Status |
|----------------|--------|
| Step 1 - {title} | ✅ Complete |
| Step 1R - Review Step 1 | ✅ Clean - 0 Errors |
| T1 - {case} | ✅ |
| Step {N} - Requirements fit | ✅ Every R{n} Met |
| R1 - {outcome} | ✅ Met |
| E1 - {title} | ✅ Fixed |
| Briefing | ✅ reviews/briefing_<slug>.md |
| Closing Exam | ✅ Holds · councils/council_<slug>-exam.md |
```
