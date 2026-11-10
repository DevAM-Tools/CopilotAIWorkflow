# Evolve Workflow

Load on `/evolve`, or when the user dictates what the software must do and asks to build it. Apply `copilot-instructions.md`. Also load now: `tech-spec.md`, `tech-reqtrace.md`, `tech-reporting.md`, `tech-docs.md`.

**Purpose:** Develop one product continuously from requirements. The person states what it must do. The agent specifies, plans, builds, tests, verifies, documents, and repairs. The person acts only at three gates.

Language and product type do not matter. Composes the existing workflows.

## Gates

| Gate | When | Person sees | Budget |
|------|------|-------------|--------|
| A — Spec | After a Spec delta | Spec delta | `tech-reporting.md` |
| E — Escalation | A Decision-class point or blocker | Decision cards | Batched; asked when no selectable work is left, or at the next gate |
| B — Release | After a batch | Briefing | `tech-reporting.md` |

No other human stop exists. Plans and findings are agent handoffs; a person reads them only on request. A project may add a gate in `spec/state.md` `Gates:` (`plan`).

```mermaid
flowchart TD
    I[1 Intake] --> D[2 Spec delta]
    D --> A{Gate A}
    A -->|change| D
    A -->|approve| S[3 Select slice]
    S --> B[4 Build]
    B -->|2 failed remedies| C[Card, next slice]
    C --> S
    B --> H[5 Heal]
    H -->|batch open| S
    H -->|batch full or queue empty| R{Gate B}
    R -->|change| D
    R -->|accept| I
```

## Stage 0 — Resume

- State lives in files. Read `spec/state.md`, then `spec/open.md`. Missing: create `spec/` per `tech-spec.md`.
- Find the `reqtrace` call form (`tech-reqtrace.md` § Call form). No row matches: put the install request in the next Spec delta (`tech-reqtrace.md` § Setup, step 3); until it is approved, run the checks by search.
- Apply answered cards first: patch the spec, re-queue the blocked slice, fill **Applied**.
- Existing code without a spec: specify only what this input touches. Full backfill only on request.
- Resume at **Position**.

`spec/state.md` holds exactly these lines. `Status` is one of `intake` · `gate-spec` · `building` · `gate-release` · `waiting` · `idle`. `Queue` has one line per slice: name — requirement IDs — depends on — blocked by. Update the file at every stage change and before every stop.

```markdown
# State

Status: building
Position: reject-duplicates · Stage 4
Gates: spec, release
Queue:
- reject-duplicates — REQ-EXPORT-NO-DUPLICATE — depends on create-and-list
- rename — REQ-EXPORT-RENAME — depends on create-and-list — blocked by Q-EXPORT-RENAME-HISTORY
Batch: slices 1 · production lines ~180 · since last Gate B
Last dream: 3f2a91c · full: 9be01d4
```

## Stage 1 — Intake

Dictated input is noisy. Read all of it. Lose nothing.

Sort each statement:

| Kind | Goes to |
|------|---------|
| Obligation | Requirement |
| Test idea, edge, “it must not…” | Test card |
| Domain fact, also in passing | `docs/domain/` now (`tech-spec.md`) |
| Preference on how to build | Lasting: propose a line for `custom_instructions.md` (or a skill change) in the Spec delta; never write either unasked. This work only: a plan constraint |
| Open fork | Section 4.16: decide or card |

Copy the depth, not the content:

```text
Dictated: "Exports need names, and, uh, no two the same. Our auditors call a corrected invoice
           a credit note, by the way. It has to be fast. Oh, and use SQLite for it."
Obligation:  The system shall let an operator name an export.
Obligation:  The system shall not accept a second export with an existing name.
Test idea:   Submit the same name twice; one row remains.
Domain fact: "Credit note" = a corrected invoice. Source: user, in passing → docs/domain/ now.
Open fork:   "Fast" has no measure → propose measure and target as an Assumption in the Spec delta.
Preference:  SQLite → a new dependency: New Dependency Protocol in the Spec delta.
Wrong:       Keeping only the two obligations and dropping the other four lines.
```

Then size the work. Pick the smallest fit. A condition of S that is uncertain is not met:

| Size | Fits when | Route in Stage 4 |
|------|-----------|------------------|
| S | ≤ 3 production files; no public API, format, or security change | No plan file. Failing test, fix, `/review` on the change |
| M | One slice | Plan with one step, then implement. One Closing Review; no Step `{n}R` |
| L | Several slices | Full plan per slice |

## Stage 2 — Spec delta

Execute `workflow-requirements.md` in Delta mode. Then write the test cards per `tech-spec.md`: at least one per new requirement and every applicable class in `tech-test.md`. Then write the Spec delta (`tech-reporting.md`).

An S-size fix that changes no obligation skips this stage and Gate A.

## Gate A — Spec

Show the Spec delta. Set `Status: gate-spec`. Stop.

- Approve → set the delta's requirements and cards `active`. Approved install request: run `tech-reqtrace.md` § Setup steps 4–8. Run `reqtrace accept --allow-gap "cards and tests follow in the slices"` (`tech-reqtrace.md` § Accept); refused for anything but L4 or L6 → fix, accept again. Cut slices. Fill **Queue**.
- Change → patch and show only what changed. Stop again.
- ❗ Only explicit words approve. A question, a comment on one entry, or silence is Change or no answer, never approval.
- Approval covers planning and implementing these requirements, the listed public API, agent surface, limits, and dependencies.

## Stage 3 — Select slice

- Cut slices by what a person can try, smallest first, in dependency order. A slice ends in something a person runs or sees; “the data layer” is no slice.
- Pick the first selectable: a re-queued slice, else `Position`, else the next without an open blocker.
- ❗ One slice per session when the context window is ≤ 200k tokens or unknown. Before ending: update `spec/state.md`.
- None selectable: show each open card in full (Gate E), `Status: waiting`, run report.

## Stage 4 — Build

Load the tech skills of the slice (Section 3).

1. **Size S:** write the failing test, fix, run the Sync check (`tech-docs.md`), run `/review` on the change (`workflow-review.md`). Fix and review again until zero Errors and zero High Performance findings. Go to step 5.
2. **Plan:** `workflow-plan.md` in Autonomous mode.
3. **Tests first:** turn the slice’s cards into tests, each with its marker (`tech-spec.md` § Trace). Run them. ❗ Each new test fails for the expected reason before the code exists: its assertion fails, or the member under test does not exist yet. A typo, a setup error, or a missing fixture is not the expected reason; fix the test first. Run `reqtrace check` → no L6 left for these cards.
4. **Implement:** `workflow-implement.md`. It makes these tests pass and never rewrites them; a test changes only through its card. Size M skips Step `{n}R`; size L keeps it. Its Closing Review covers the slice.
5. **Prove as a user:** run each Done-when through the real boundary: the browser for a UI, the process for a CLI, HTTP for an API, a consumer call for a library. A green unit suite is not proof. Tick `Met` only then. A Done-when that fails here is a failed `Verify` (Section 4.15).

```text
Wrong: The tests pass, so REQ-EXPORT-NO-DUPLICATE is `Met ✅`.
Right: Run `export create Report` twice in the built CLI, read the output, see the error and one row, then tick.
```

Two failed remedies of one root cause: write a card, mark the slice blocked, continue with the next (Section 4.15).

Never edit an `active` requirement to make it pass. A wrong requirement is a card.

## Stage 5 — Heal

Run `workflow-dream.md` in `touched` mode on the slice’s areas. Update **Batch**.

Gate B when any holds:

- The queue is empty.
- The batch holds 3 slices or ~1000 production lines.
- A change marked **Judge** (`tech-reporting.md`) is in the batch.

Otherwise go to Stage 3. Run `/dream full` after 10 slices or 20 inbox lines (ask at the next gate; never block on it).

## Gate B — Release

Write one briefing for the batch (`tech-reporting.md` § Briefing). Show its head (**Verdict** to **Attention**) and **Concept** in chat, with the link to the file. Set `Status: gate-release`. Stop.

- Accept → run `reqtrace accept`; it records the batch's new markers, so C2 guards them from now on. Refused → L4 or L6 on an entry of a slice still in **Queue**: accept again with `--allow-gap "<slice> is queued"` or `"<slice> is blocked by Q-…"`. Any other finding is an Error of the batch: fix it, `check`, accept again. Reset **Batch**. `Status: idle`.
- Change → the change is new input: go to Stage 1.

## Completion

Stop conditions, closed set:

| Stop | Meaning |
|------|---------|
| `gate` | Waiting at Gate A or B |
| `waiting` | Only cards remain |
| `budget` | Session limit reached; state saved |
| `idle` | Queue empty, batch accepted |

Chat: the run report (`tech-reporting.md`) on every exit.
