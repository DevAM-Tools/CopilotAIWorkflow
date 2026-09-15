# Plan Workflow

Load on `/plan`. Apply `copilot-instructions.md` Sections 2–4.

Plans are for **human acceptance** and for a **weaker executing agent**. Before/After, locked `How`, named test cases, and step acceptance criteria are the handoff. Do not treat existing files under `plans/` as style examples unless the user points at one. Do not compress step `How`, Requirements, or Requirements-fit.

## Stage Order

0. Requirements
1. Gather Context
2. Perspective Sweep
3. Grill Me
4. Reconcile
5. Decision Loop
6. Write Plan
7. Coverage Check

## Stage 0 — Requirements

- If a requirements artifact, attached doc, or explicit list already exists: **read it in full** and use it. Do not rewrite outcomes that are already clear.
- Else execute `workflow-requirements.md` (write `requirements/req_<slug>.md`), then continue.
- Carry `R{n}` into this plan. Steps do **not** cite requirement IDs in `How`. Tracking happens by **running Done-when at step close** and at Requirements-fit.

## Stage 1 — Gather Context

- Read relevant code, tests, docs, interfaces, build config.
- Read every user-attached architecture, guide, requirement, or brief **in full**.
- Enumerate all affected files before planning.
- Run Tech Load Protocol per `copilot-instructions.md` Section 3.
- Identify interface candidates, hot paths, and (for web UI) Playwright journeys.
- Build a test-case list (Stage 6 Test cases). Apply `tech-test.md`.
- Identify public API surface when this is a library or other public contract.

## Stage 2 — Perspective Sweep

- Sweep per `workflow-council.md`. Same agent. No subagents. No `councils/` file.
- Promote Grill Me → Stage 3, Council candidates → Stage 5, Act → plan constraints.
- Record `workflow-council.md` in Context Anchor `Loaded skills:`.

## Stage 3 — Grill Me

Use this template. Ask all unresolved questions in one round. Do not re-ask answered questions. Follow-ups only for new ambiguities; cite the prior answer.

Include unresolved Sweep/Council follow-ups. Tag `Source`. Council candidates → Stage 5.

Cover every topic before finalizing scope:

- functional outcomes and **concrete** acceptance criteria (not “it builds”)
- named test cases: in / out; edges; time budget (`tech-test.md`)
- public API snippet + usage when a public surface exists
- web UI: load per Section 3 (`tech-web.md`, `tech-playwright.md`, `tech-test.md`, stack UI skill); dark mode (other themes out of scope); responsive behavior; journeys; debug story
- performance and allocations (hot paths, budgets) — performance is a feature
- edge cases and error handling
- security boundaries and STRIDE
- trust-boundary limits: proposed defaults; constant vs configurable (where, who, unset default)
- concurrency, TOCTOU, async interleaving
- compatibility, migration, breaking change
- new dependencies: id, what it does, why needed, license, alternatives
- architecture boundaries
- agentic debug path (smallest command, filterable tests, traces)
- API misuse / abuse vectors
- automation when the same edit hits more than ten call sites (approved script or codemod)

```markdown
## Q{n} — {topic}
**Source:** Sweep | Council | Plan | Requirements
**Context:** {1-3 sentences}
**Question:** {single-part question}
**Options:** 1) {option} · 2) {option} · 3) {option} · or free-text
```

Do not proceed while ambiguities remain. New blocking fork → Stage 5 before Write Plan.

## Stage 4 — Reconcile

- Cross-check request, requirements artifact, Grill-Me answers, Sweep, council verdicts, code, docs, ADRs, and Section 4.
- Record a **preference** when both sides cannot hold (`C{n}`).
- Apply Rule Priority (§4.13), then explicit user choice, then scope split.
- Align docs ↔ code ↔ tests when source-of-truth is unclear; ask.
- Gate: Write Plan when no undecided preference blocks scope. Else Stage 5.

## Stage 5 — Decision Loop

Grill Me ↔ Council per `workflow-council.md` until no blocking fork. Lite default; Full if `/council` or security / public-API / irreversible. Verdict → `C{n}`. Cap 3. Do not Write Plan while open.

## Stage 6 — Write Plan

- User path when provided; else `plans/plans_<slug>.md`.
- English (Section 4.6).
- Slug: lowercase, punctuation/whitespace → `-`, collapse `-`, trim, fallback `task`.
- Step Overview at top. Status starts `⬜`.
- Link or copy Requirements (User View) next. Link to `requirements/req_<slug>.md`. Copy a short table only if the plan must stand alone.
- Map every `R{n}` in Target Solution (design completeness). Unmapped `R{n}` = incomplete. Extra design with no `R{n}`: justify or cut. **Do not** repeat those IDs inside step `How`.
- Test cases are first-class (section + checklist rows). Name behaviors, error paths, boundaries, concurrency, and security — not only test file names. An executing agent must be able to implement them as steps.
- Write Requirements as user-observable outcomes with a Done-when check. Not slogans. Not implementation tasks. A green build is not an acceptance criterion.
- End with Requirements fit, then its Step NR.
- Record `Loaded skills:` (include `workflow-council.md` and `tech-test.md` when tests exist), Sweep table, council paths, Decision Loop count, step dependencies. Leave Coverage for Stage 7.
- Every step needs a fully specified `How` and Before/After (Shared Block below).
- Do not present the plan for approval. Run Stage 7.

### Step Overview

Narrow table. Experience lives in the step block, not as an extra column.

```markdown
| Step | Status | Delivers |
|------|--------|----------|
| Step 1 — {title} | ⬜ | {one sentence} |
| Step 1R — Review Step 1 | ⬜ | Zero Error findings; iterate until clean |
| Step {N} — Requirements fit | ⬜ | Every R{n} met from the user view |
| Step {N}R — Review Step {N} | ⬜ | Zero Error findings; iterate until clean |
```

### Shared Block (plan steps)

Field order: `What` → `Why` → `How` → `Experience` → `Acceptance` → `Tests` → `[Public API]` → `[Size]` → `[Context]` → `[Where]` → `Verify` → `Debug` → `[If it fails]`.

Always require `What`, `Why`, `How`, `Experience`, `Acceptance`, `Verify`.
Require `Tests` when the step ships behavior (name `T{n}` cases this step adds or runs).
Require `Public API` when the step ships or changes a public surface.
Require `Size` when the step may exceed the soft budget (see Step Rules).
Omit `Context` only when neither constraints nor sources exist. Omit `Where` when no file is touched.
Require `If it fails` for schema, state, or external-system risks.
Require `Debug` so the executing agent can probe a red `Verify` (command, Playwright, collision).

❗Specify the concrete implementation. Intent-only `How` is incomplete.
❗Write `How` so another agent can implement without inventing types, items, signatures, algorithms, control flow, or file structure.
❗Write `How` exhaustively: types, items, visibility, signatures, parameters, return values, call-site edits, validation, error paths, control flow, data flow, thread-safety / performance / security constraints, prerequisite state, decision rationale, and important edge cases.
❗Include fenced **Before** and **After** in every step `How` — current code, then Target Solution shape (real signatures and key bodies); anchor with path/symbol. Not stubs, not pseudocode-only, not an intermediate shape later steps will replace. New file: After only. Requirements-fit: skip unless a gap needs a fix.
❗Cite a concrete source in `Context` when an external reference exists.

`Where`: path, approximate lines, symbol. Mark `primary` (create/rewrite) or `call-site` or `additive` (append-only).
`Verify`: exact command in optimized/Release per loaded tech skill, plus expected result.
`Experience`: what a person can try after this step, what they should see, how to try it, and what is **not yet** true.

```markdown
## {ID} - {Title}
Status: ⬜ {Initial} · {Depends on}
### What
### Why
### How
### Experience
After this step a person can: …
They should see: …
How to try: {command | Playwright spec | usage snippet}
Not yet: …
### Acceptance
- [ ] {observable check for this step — may be deeper than R-level}
### Tests
- T{n} {case} — {in this step: add | run}
### Public API
### Size
Prod files: {n} · ~LOC: {n} (tests excluded) · over budget because: {or n/a}
### Context
### Where
### Verify
### Debug
Reproduce: …
First probe if red: …
UI: {Playwright trace / headed / n/a}
Collision: other agents locking build/test?
### If it fails
```

## Plan Structure

1. Step Overview
2. Requirements (User View) — table or link to `requirements/req_<slug>.md`
3. Test cases (first-class)
4. Summary / Context Anchor (include Coverage table)
5. Target Solution (Vision) — include Public API snippet when applicable
6. Phases (optional; >10 steps or multiple areas)
7. Slices
8. Steps (Shared Block; last = Requirements fit + NR)
9. Edge Cases and Risks
10. Decisions & Trade-offs (`C{n}`; omit when none)
11. Open Questions
12. Closing Summary
13. Task Checklist (Step N, tests that are their own steps, Step NR; include Requirements fit)

## Requirements (User View)

```markdown
## Requirements (User View)

Source: {path or "inline"}

| ID | Requirement | Done when | Met |
|----|-------------|-----------|-----|
| R1 | {observable user outcome} | {check a later agent can execute} | ⬜ |
```

- Imperative, testable. One outcome per `R{n}`.
- Fill `Met` only when Done-when **ran** (step close or Requirements-fit). Start `⬜`.
- Reject the plan when an `R{n}` cannot be observed by a user.
- Step `How` does not say “implements R3”. At step close the agent re-reads this table and ticks rows whose Done-when now holds.

## Test cases

First-class. The implementing agent can add or run each row as part of a step (or a dedicated test step).

```markdown
## Test cases

| ID | Case | Kind | Step | Status |
|----|------|------|------|--------|
| T1 | {behavior, including edge} | unit · error · boundary · concurrency · UI | Step 2 | ⬜ |
```

Kind stays short so the table remains narrow. Details belong in the step `Tests` / `How`.
Exhaustive = the **classes** in `tech-test.md`, not every integer. Keep the suite fast.

## Target Solution (Vision)

- Concrete end-state: types, files, APIs, data flow, invariants, algorithms. Not a slogan.
- Map every `R{n}` to a design element here (completeness). Steps then apply this shape.
- SSOT for final file shape. A primary-file `After` that differs from this section is an incomplete plan.
- Do not use step order here.

### Public API (when a public surface exists)

Short snippet + usage. This is what a human reviews. Changing it after approval → new Grill Me.

````markdown
### Public API

```csharp
public sealed class GapRunOrchestrator
{
    public static Task<int> RunAsync(CliOptions options, CancellationToken ct);
}
```

Usage:

```csharp
int code = await GapRunOrchestrator.RunAsync(opts, ct);
```
````

## Slices

- Group steps by what becomes **tryable** (Experience), not by stub-then-fill.
- Cut a real thin slice. Do not fake end-to-end. `Not yet:` must be honest.
- Ban scaffolding that later steps delete (`NotImplemented`, dummy types, temporary wrappers).
- Build each cross-cutting concern in target shape. Later slices call it.
- Extract a repeated pattern before a second slice copies it.
- Preserve layer boundaries.

## Step Rules

Soft budgets — justify when exceeded; do not split an atomic cutover just to hit a number:

- Cap a step at ≤12 production files and ≤~3000 production LOC. Tests do not count.
- Make edits additive (`Where` = `additive`) when the file shape can stay. Rewrite when the shape must change.
- Do not list the same file as `primary` in many steps unless each increment is a complete Experience and splitting would be worse.
- Leave each step as something a person can see or try (`Experience`).

Still required:

- Freeze file shape in Target Solution. `After` is that shape, not an intermediate.
- Write new types complete (signature, body, errors, tests) before consumers call them.
- Keep rename/move separate from behavior change.
- Ship tests with the production behavior they prove. Dedicated test steps are welcome when the cases are large; do not dump all tests at the end by default.
- Size a step so a human can accept the Before/After.
- Analyze dependencies. Order topologically. State depends-on.
- Reject a `How` that allows more than one implementation, or whose `After` is not Target Solution for primary files.
- Close with Requirements fit.

## Stage 7 — Coverage Check

Run after the plan file is written. **Walk the plan twice.** Patch until both walks are clean. Do not enter Completion with gaps.

1. **Requirements walk** — Re-read the requirements artifact (or User View table) **in full**. Every `R{n}` and every `T{n}` must land in Target Solution and in a step `How` / `Tests` / `Acceptance`. Unmapped = gap.
2. **Conversation walk** — Re-read the conversation, Grill Me Q/A, Sweep, council, and attached docs **in full**, then the **entire** written plan. Every relevant user ask, constraint, non-goal, named type/path/command, accepted proposal, and rejected option with leftover constraint must land somewhere. Unmapped = gap. “Implied” without a citation = gap.

Include every `R{n}`, every `T{n}`, and every Grill Me answer as a row. Record dropped items with reason. Write both tables into Context Anchor. Re-run this stage after any patch.

```markdown
**Coverage (requirements → plan):**

| ID | Lands in |
|----|----------|
| R1 | Target Solution · Step 2 Acceptance |
| T1 | Step 2 Tests |

**Coverage (conversation → plan):**

| Item | Source | Lands in |
|------|--------|----------|
| {one-line item} | User · Q{n} · Sweep · Council · Doc | R{n} · T{n} · Step {n} · C{n} · Out of scope ({reason}) |
```

## Requirements Fit (last step)

```markdown
## Step {N} - Requirements fit
Status: ⬜ Depends on all prior steps
### What
Walk the built solution as a user. Check every R{n} and every T{n}.
### Why
A green build can still miss the user outcome.
### How
- Re-read Requirements (User View) and Test cases. Ignore implementer intent.
- For each R{n}: run Done-when. Cite evidence (command, UI, API, file, output).
- For each T{n}: confirm the case exists and can fail.
- Mark Met / Status ✅ only when the check holds with no caveats.
- Any ❌ or leftover ⬜ = blocker.
- Skip Before/After unless a gap needs a code fix; then stop and file the gap.
### Experience
A person can execute every Done-when without reading the source.
### Acceptance
- [ ] Every R{n} Met = ✅
- [ ] Every T{n} Status = ✅
### Verify
Every R{n} Met = ✅. Every T{n} ✅. Zero leftover ⬜.
### Debug
Re-run the failing Done-when in isolation. Check agent collision before redesign.
```

## Checklist Rules

- Flat ordered list: Step N, any dedicated test steps, Step NR. Include Requirements fit.
- Tick Status on every transition on **all three** surfaces: Step Overview, Shared Block, Task Checklist.
- Zero Error findings at each review gate before the next step.

## Completion

- Stage 7 clean: both coverage walks green (requirements **and** conversation).
- Return plan path. Cite requirements path and council paths if any.
- Chat: path only. Do not recap Coverage.
- Wait for explicit user approval per Section 6 before implementation.
