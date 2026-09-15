# Review Workflow

Load on `/review`. Apply `copilot-instructions.md` Sections 2–4.

**Purpose:** Adversarial review of code, associated tests, and how parts interact. Skeptic stance: assume it cannot work; hunt the hair in the soup.

## Modes

### Full review (default)

Unless the user explicitly requests **reduced scope**, run a full review:

- Read in-scope sources, associated tests, docs, and call sites.
- **Analyze** associated tests: can they fail? do they cover the behavior classes? do they match plan `T{n}`?
- **Execute** in optimized/Release per loaded tech skill:
  - build for in-scope projects;
  - unit/integration tests paired with in-scope production code;
  - coverage gate when the loaded tech skill defines one (e.g. ExitPointGaps);
  - Playwright journeys when web UI is in scope.
- Record every command and pass/fail result in the review artifact **Test Execution** section.
- Red build, failing test, or failing gate → **Error** finding with relevant output in `Context`.
- Do not manually run or explore the product outside what the test commands require.

Exam is not a `/review` substitute.

### Static review (reduced scope — explicit only)

Enter **only** when the user explicitly requests reduced scope. Examples: `static review`, `code only`, `code-only`, `reduced scope`, `without running tests`, `ohne Testausführung`, `nur Code-Analyse`.

- Read sources, associated tests, and docs. **Do not** run build, tests, coverage gate, Playwright, debugger, or the product.
- Judge tests by **reading** them only.
- Exit-path coverage and gates are judged from source; do not execute the gate.

Record the chosen mode in the review artifact **Scope** section.

## Stage Order

1. Define Scope
2. Load Rules
3. Gather Context
4. Review
5. Output

## Stage 1 — Define Scope

- Treat scope argument as confirmed when provided.
- Otherwise ask in-scope items, exclusions, and focus.
- **Mode:** default **full review**. Use **static review** only when the user explicitly requests reduced scope (see Modes).
- Do not review before scope and mode are confirmed.

## Stage 2 — Load Rules

- Run Tech Load Protocol per `copilot-instructions.md` Section 3.
- Load Sweep Mode from `workflow-council.md`.
- Load `tech-test.md` when tests or production APIs are in scope.
- Load the technology test skill (`tech-tunit.md`, `tech-pytest.md`, `tech-rust-test.md`, `tech-playwright.md`) when tests are in scope.

## Stage 3 — Gather Context

- Enumerate in-scope files and their associated test projects / spec files.
- If a plan is in scope, **read it in full**. If requirements, architecture, guides, or briefings are linked or attached, **read those in full**.
- If `reviews/briefing_<slug>*.md` exists for this scope, read it first. Also accept legacy `reviews/brief_<slug>*.md`. Follow card order. Open each file link. Treat **How it serves** as claims to verify **in the file by reading**. A slogan, ID-only, or copied **How it serves** is not a reason — read the file anyway.
- Read in-scope files, related tests, direct dependencies, and call sites.
- Map composition: callers, callees, shared state, sequencing, and error paths that only appear when pieces combine.
- Read definition and docs for involved types and items.
- Build coverage checklist: file × criterion.
- Build test-coverage matrix against plan `T{n}` when present **from the test source**; list gaps explicitly.

**Full review:** after reading, run build and scoped tests per Modes. On failure, capture output for findings. Consider concurrent-agent collision before treating a failure as a product defect (Section 4.14).

**Static review:** do not run build, test, or coverage commands.

## Stage 4 — Review

- **Consistency first:** cross-check plan, requirements, request, code, tests, docs, and comments (documented behavior ≠ implementation, claimed `Verify` ≠ observed test result, API contract ≠ call sites, README/guide stale).
- Fix cross-file drift in finding `How` when source-of-truth is clear; cite `C{n}` when plan already chose. Undocumented mismatch → Error.
- Review exhaustively and adversarially. Do not trust tests, docs, or a green path without evidence.
- **Skeptic pass (required):** Assume the in-scope solution cannot work. Do not stop after the first flaw. Cite `Skeptic` in finding `Context`.
  - **Parts:** every in-scope unit — wrong default, off-by-one, silent swallow, copy-paste, tests that cannot fail, missing guard. Dumbest caller/operator error AND worst abuse + STRIDE at trust boundaries.
  - **Whole:** composition — call-graph, shared state, ordering, contracts vs callers, tests that pass but do not prove the claim, requirements that hold per file but fail end-to-end, defects that exist only in the interplay of otherwise-correct pieces.
  - Hair in the soup counts. Isolated nits that violate §4 or become fatal in combination = Error. `none` only after both hunts ran and found nothing (say so in Sweep).
- Evaluate all in-scope files against `copilot-instructions.md` Section 4 plus loaded tech-skill rules.
- Compare requested target vs observed source.
- When a plan is in scope: walk every `R{n}` Done-when and every `T{n}` against code and tests. Unmet on the page = Error. **Full review:** run Done-when checks that are executable here (tests, commands). **Static review:** judge from source only.
- Evaluate test content per `tech-test.md` (behavior classes, speed), not only file names. A test that cannot fail is a gap. **Full review:** a test that passes but cannot fail is still a gap.
- **Full review:** run the loaded tech skill’s coverage gate when one exists; a failing gate = Error. **Static review:** if `tech-tunit.md` is loaded, judge exit-path coverage from source (missing tests = Error); do not run the gate. Other stacks: do not invent an exit-path gate.
- Flag missing misuse/abuse analysis in plan as Error when new public APIs are introduced.
- Flag a public API that drifted from the plan snippet as Error.
- Evaluate interface vs hot-path concrete decisions. Performance is a feature: allocations and (in C#) gen-0 vs gen-1 survival on hot paths (Section 4.4 + tech skill).
- Web UI: markup/CSS look responsive unless the user waived it. **Full review:** run planned Playwright journeys; failures = Error. **Static review:** specs must exist in source and look able to fail; do not launch the browser.
- Never stop after first N findings.
- Flag missing tech-skill load as Error when triggered files are in scope.
- Flag a dependency or script added without user approval as Error.
- Sweep per `workflow-council.md` (same agent, no subagents). Skip none. View defect → finding; cite view in `Context`. Skeptic Sweep row must cover parts and whole and point at finding IDs (or documented `none`).
- Competing goals without recorded preference → Error; user decision before release verdict. High-stakes fork: recommend `/council` in `How`. Do not auto-run Full unless asked.

## Stage 5 — Output

Use the templates below for all findings output.

### Shared Block (every finding)

Field order: `What` → `Why` → `How` → `[Context]` → `[Where]` → `Verify` → `[If it fails]`.
Always require `What`, `Why`, `How`, `Verify`.
Omit `Context` only when neither constraints nor sources exist. Omit `Where` when no file is touched.
Require `If it fails` for schema, state, or external-system risks.
❗Specify the concrete fix. Intent-only `How` is incomplete.
❗Write `How` so another agent can implement the fix without inventing types, items, signatures, algorithms, control flow, or file structure.
Make `How` a standalone, exhaustive fix recipe: types, items, visibility, signatures, parameters, return values, call-site edits, validation, error paths, control flow, data flow, thread-safety/performance/security constraints, prerequisite state, decision rationale, and important edge cases.
❗Include fenced **Problem** and **Fix** code in every finding `How` — current code, then target code with real signatures and key bodies; anchor with path/symbol. Not stubs, not comments-as-code.
❗Cite a concrete source in every finding `Context` when an external reference exists.
Put `Where` as path, approximate line numbers, and searchable symbol anchor.
Put `Verify` as the exact command a later implementer must run (optimized/Release per loaded tech skill) and the expected result. **Full review:** you may have run it already — still record it here for post-fix confirmation. **Static review:** do not run `Verify` during `/review`.

```markdown
## {ID} - {Title}
Status: ⬜ {Initial} · {Depends on / Severity}
### What
### Why
### How
### Context
### Where
### Verify
### If it fails
```

### Findings Overview Table

```markdown
| ID | Bucket | Title | Summary |
|----|--------|-------|---------|
| E1 | E | {title} | {one sentence with location} |
```

### Output Modes

**File mode** (default under `/complex-task`): write to `reviews/review_<slug>_<iteration>.md`. Put Findings Overview at top. Every finding as full Shared Block under its bucket section. In chat: bucket counts, release verdict, artifact path, prioritized action list. Do not repost Shared Block contents in chat.

**Chat-only mode** (no review file): output Findings Overview table, Perspective Sweep table, and every finding as full Shared Block in chat. Overview, then Sweep, then bucket sections; Priority Action List after all findings.

### Review File Sections

1. Findings Overview (top)
2. Summary
3. Scope (include **Mode:** full review | static review)
4. Test Execution (**full review:** commands run, pass/fail, gate summary; **static review:** `Not run — static review`)
5. Perspective Sweep (table; every view filled; finding IDs or `none`)
6. Errors
7. Cosmetic Issues
8. Refactoring Opportunities
9. Performance and Allocations
10. Closing Assessment
11. Priority Action List

## Finding Buckets

- Error
- Cosmetic
- Refactoring Opportunity
- Performance

Assign exactly one bucket per finding.

## Category Rules

- **Error:** Severity (High/Medium/Low) in status line; Problem/Fix in `How`; OWASP category for security; missing boundary and guard for validation gaps.
- **Cosmetic:** exact style rule in `How`; Problem/Fix in `How`.
- **Refactoring:** unchanged behavior in `Why`; extract/move/split in `How`; Problem/Fix in `How`.
- **Performance:** Section 4.4 + tech skill in `How`; Problem/Fix in `How`; frequency, allocation pressure, cache behavior, and throw/panic / error-return paths in `Context`.

## Closing Assessment

- Include architecture quality, composition (parts vs whole), dominant error themes, thread-safety posture, allocation profile, docs consistency.
- **Full review:** state build/test/gate outcome. **Static review:** state that execution was skipped by user request.
- Confirm Sweep covered all five views; unused view = `none`.
- Confirm Skeptic pass ran on parts and on composition; cite finding IDs or `none`.
- State explicit release verdict: `Ready for public release` or prioritized blocker IDs.
- List top 3 priority actions by finding ID.

## Completion

- Report counts by bucket and prioritized action list.
