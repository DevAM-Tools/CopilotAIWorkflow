# Requirements Workflow

Load on `/requirements`, or from `/plan` / `/complex-task` when no usable requirements exist yet. Apply `copilot-instructions.md` Sections 2–4. Do not implement. Do not edit production code.

**Purpose:** Capture intention and break it into observable requirements with acceptance criteria that later agents can **verify at task close** — not by sprinkling IDs through `How`.

## When

- User runs `/requirements` (or natural-language equivalent).
- `/plan` finds no requirements artifact, no attached requirements doc, and no usable list in the request → run this skill first, then plan.
- User already supplied requirements (artifact, attached doc, or explicit list) → **use them**. Do not reinvent. Fill gaps with Grill Me. Re-read the supplied file **in full**.

## Stage Order

1. Intention
2. Breakdown
3. Acceptance criteria
4. Grill Me
5. Write artifact
6. Coverage

## Stage 1 — Intention

Answer, in the artifact: who, job-to-be-done, why now, success picture, non-goals, hard constraints. Read attached architecture, guides, and docs **in full**.

## Stage 2 — Breakdown

- Write user-observable outcomes, not implementation tasks.
- Write few high-level `R{n}`. Split to mid/low only when otherwise the acceptance criteria would be vague.
- One outcome per `R{n}`. Ban slogans (“make it robust”). Ban “the project builds” / “tests exist” / “docs updated” — those are Section 4.

```markdown
| ID | Level | Requirement | Done when | Met |
|----|-------|-------------|-----------|-----|
| R1 | high | {observable outcome} | {check a later agent can run} | ⬜ |
```

Level is `high` / `mid` / `low`. Keep the table to these columns so it stays readable in a narrow pane.

## Stage 3 — Acceptance criteria

Every `R{n}` needs Done-when that names an **actor**, an **action**, and an **observable**. Depth may be shallower at R-level than at plan-step level.

```text
Reject: Export works · API is robust · solution is performant
Accept: Given a solution with 2 projects, `exitpointgaps run solution …` writes
        summary.json with projects.length==2 and exitGapCount equal to the fixture
```

Done-when must be executable by a later agent or visible to a human. Partial, hidden, or “works if you know the code” is not Done-when.

## Stage 4 — Grill Me

Ask all unresolved questions in one round. Do not re-ask answered questions. Cover:

- intention and non-goals
- each AC until it is concrete (force an example, not an adjective)
- test content: which edges are in; what is out (no cartesian integer sweeps); time budget
- public API (libraries): short snippet + usage — `workflow-plan.md`
- Playwright journeys (web UI)
- performance / allocation constraints
- security boundaries
- trust-boundary limits: proposed defaults; constant vs configurable

```markdown
## Q{n} — {topic}
**Source:** Requirements
**Context:** {1-3 sentences}
**Question:** {single-part question}
**Options:** 1) {option} · 2) {option} · 3) {option} · or free-text
```

Do not proceed while blocking ambiguities remain.

## Stage 5 — Write artifact

- User path when provided; else `requirements/req_<slug>.md`.
- English (Section 4.6).
- Slug: lowercase, punctuation/whitespace → `-`, collapse `-`, trim, fallback `task`.

```markdown
# Requirements — {title}

## Intention
{who, job, why now, success, non-goals, constraints}

## Requirements

| ID | Level | Requirement | Done when | Met |
|----|-------|-------------|-----------|-----|
| R1 | high | {outcome} | {check} | ⬜ |

## Notes
{preferences, out of scope, links to attached docs}
```

- `Met` starts `⬜`. Fill only when a later step **runs** Done-when (implement step close or Requirements-fit). Do not mark Met because a `How` mentioned the outcome.
- Do not put requirement IDs in code or comments.

## Stage 6 — Coverage

Re-read the conversation and attached docs. Every user ask, constraint, and non-goal is a row. Unmapped = gap. Patch until clean.

```markdown
**Coverage (conversation → requirements):**

| Item | Source | Lands in |
|------|--------|----------|
| {one-line item} | User · Q{n} · Doc | R{n} · Notes · Out of scope ({reason}) |
```

## Completion

Return the artifact path. Status table, goal verdict, risks ≤5. Chat: path only — do not recap the table.
