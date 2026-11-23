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

No other human stop exists. Between the gates the person sees a progress note after each step (§ Progress note); it asks nothing and waits for nothing. Plans and findings are agent handoffs; a person reads them only on request. A project may add a gate in `spec/state.md` `Gates:` (`plan`).

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
- Read **Builder**. Unset: take the model the person named in the request; else ask with the **Builder** line of the next Spec delta (§ Builder). Until it is set, the Lead builds.
- Apply answered cards first: patch the spec, re-queue the blocked slice, fill **Applied**.
- Existing code without a spec: specify only what this input touches. Full backfill only on request.
- A **Queue** line without a status mark: read it as `⬜` and write the mark on the next update. No `Run` line: read it as `slice`.
- Resume at **Position**. Print a progress note (§ Progress note) before the first edit.

`spec/state.md` holds exactly these lines. `Status` is one of `intake` · `gate-spec` · `building` · `gate-release` · `waiting` · `idle`. `Position` names the slice and the stage; in Stage 4 also the plan step as `Step {n} of {total}`. `Queue` has one line per slice since the last accepted Gate B: status — name — requirement IDs — depends on — blocked by. The status uses the Section 5 legend: `⬜` queued · `🚧` building · `✅` built, proved, and healed · `⚠️` blocked by a card. ❗ A `✅` line stays in **Queue** until Gate B accept removes it: the progress note and the run report read what is done from these lines. `Builder` is the model the Builder subagent runs on, written as the tool accepts it, or `none — {reason}` (§ Builder). `Run` is `slice` (default) or `all` (§ Run all). Update the file at every stage change, after every plan step, and before every stop.

```markdown
# State

Status: building
Position: reject-duplicates · Stage 4 · Step 2 of 3
Gates: spec, release
Builder: {the cheaper model, as the person named it}
Run: slice
Queue:
- ✅ create-and-list — REQ-EXPORT-CREATE, REQ-EXPORT-LIST
- 🚧 reject-duplicates — REQ-EXPORT-NO-DUPLICATE — depends on create-and-list
- ⚠️ rename — REQ-EXPORT-RENAME — depends on create-and-list — blocked by Q-EXPORT-RENAME-HISTORY
- ⬜ import — REQ-EXPORT-IMPORT — depends on create-and-list
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
| Structure: components, layers, who may call whom | The Spec delta **Architecture** line (Stage 2) |
| Preference on how to build | Which model builds: the `Builder` line (§ Builder). All slices in one run: `Run: all` (§ Run all). Other lasting preference: propose a line for `custom_instructions.md` (or a skill change) in the Spec delta; never write either unasked. This work only: a plan constraint |
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
Structure:   "keep the export apart from the store" → Architecture line: component Export, may depend on Store.
Preference:  "let {model} build it" → Builder: {model} in spec/state.md, the name exactly as said.
Preference:  "build all of it in one go" → Run: all in spec/state.md.
Wrong:       Keeping only the two obligations and dropping the other four lines.
```

Then size the work. Pick the smallest fit. A condition of S that is uncertain is not met:

| Size | Fits when | Route in Stage 4 |
|------|-----------|------------------|
| S | ≤ 3 production files; no public API, format, security, or architecture change (new component or **May depend on** entry) | No plan file. Failing test, fix, `/review` on the change |
| M | One slice | Plan with one step, then implement. One Closing Review; no Step `{n}R` |
| L | Several slices | Full plan per slice |

## Stage 2 — Spec delta

Execute `workflow-requirements.md` in Delta mode. Then write the test cards per `tech-spec.md`: at least one per new requirement and every applicable class in `tech-test.md`. Read `tech-test.md` § Host safety now and classify each card. Each card that would be Visible, Global, or Heavy goes into the Spec delta **Host effects** line with its effect, the options of that section, and your recommendation; write the Contained alternative as the card until Gate A answers.

**Architecture:** read `tech-docs-templates.md` § docs/architecture.md now. Name the component that will own each new requirement. Fill the Spec delta **Architecture** line:

| Situation | The line holds |
|-----------|----------------|
| No `docs/architecture.md` yet | The full Overview: every component, its responsibility, its **May depend on**, and the component diagram |
| A new component or a new **May depend on** entry | Those rows and the component diagram after the change |
| A choice between real alternatives: one process or two, a shared store or one per component | The choice and each rejected option with its reason |
| Every new requirement fits an existing component without a new dependency | `unchanged` and the owning components |

Copy the depth, not the content:

```text
Wrong: **Architecture:** unchanged. (The delta adds an XLSX export, and no component writes files yet.)
Right: **Architecture:** new component Export (src/Export/, writes CSV and XLSX files) — may depend on Store.
       Cli may now depend on Export. Rejected: export inside Store — Store would gain file I/O.
       Diagram: [delta_export_components.svg](delta_export_components.svg) — arrows Cli → Export, Cli → Store,
       Export → Store, in three rows.
```

**Council:** ❗ read `workflow-council.md` now and run it in Spec mode on the whole draft: requirements, test cards, **Host effects**, **Architecture**, and Coverage. It replaces `workflow-requirements.md` Stage 8 under `/evolve`. Apply each gap:

- `add` or `fix`: edit the entry, the card, or the **Architecture** line; run `reqtrace check`.
- `ask`: a card in `spec/open.md`; Gate A shows it under **Needs an answer**.
- `out`: the proposed exclusion goes into the Spec delta.

Then write the Spec delta (`tech-reporting.md`). ❗ No Spec delta reaches Gate A without its **Council** line.

An S-size fix that changes no obligation skips this stage and Gate A.

## Gate A — Spec

Show the Spec delta. Set `Status: gate-spec`. Stop.

- Approve → set the delta's requirements and cards `active`. Approved install request: run `tech-reqtrace.md` § Setup steps 4–8. Run `reqtrace accept --allow-gap "cards and tests follow in the slices"` (`tech-reqtrace.md` § Accept); refused for anything but L4 or L6 → fix, accept again. Cut slices. Fill **Queue** with `⬜` lines. Write **Builder** from the delta's **Builder** line. Print a progress note.
- Change → patch. A change that adds a requirement, changes the scope, or changes the **Architecture** line runs the Spec council again (`_<n>`). Show only what changed. Stop again.
- ❗ Only explicit words approve. A question, a comment on one entry, or silence is Change or no answer, never approval.
- Approval covers planning and implementing these requirements, the listed public API, agent surface, limits, dependencies, host effects, and the **Architecture** line. A Visible, Global, or Heavy test not on the approved **Host effects** line stays unwritten: card it at the next gate (`tech-test.md` § Host safety).

## Stage 3 — Select slice

- Cut slices by what a person can try, smallest first, in dependency order. A slice ends in something a person runs or sees; “the data layer” is no slice.
- Pick the first selectable: a re-queued slice, else `Position`, else the next `⬜` without an open blocker. Mark it `🚧` and set **Position**.
- ❗ `Run: slice`: one slice per session when the context window is ≤ 200k tokens or unknown. Before ending: update `spec/state.md`. `Run: all`: § Run all.
- None selectable: show each open card in full (Gate E), `Status: waiting`, run report.

## Stage 4 — Build

Load the tech skills of the slice (Section 3).

1. **Size S:** write the failing test, fix, add the code tests of every member the fix changed with their ledger line (`tech-test.md` § Code tests), run the Sync check (`tech-docs.md`), run `/review` on the change (`workflow-review.md`). Fix and review again until zero Errors and zero High Performance findings. Go to step 5.
2. **Plan:** `workflow-plan.md` in Autonomous mode. Set **Position** to `Step 1 of {total}`. ❗ The approved **Architecture** line binds the plan:
   - The step that creates a component or a dependency lists in its `Docs` field the Overview row, the component section, the component-diagram arrows (the approved delta SVG updates `docs/images/components.svg`), and a decision record (`docs/decisions/<slug>.md`, `accepted`) per approved choice.
   - The first slice creates `README.md` and `docs/architecture.md` when they are missing (`tech-docs.md` § Doc set).
   - A component or dependency the approved line lacks is Decision class: card it (Gate E). Never build it unasked.
3. **Card tests first:** the Builder writes them when one is set (§ Builder); the Lead checks each against its card. Turn the slice’s cards into tests, each with its marker (`tech-spec.md` § Trace). Run them. ❗ Each new test fails for the expected reason before the code exists: its assertion fails, or the member under test does not exist yet. A typo, a setup error, or a missing fixture is not the expected reason; fix the test first. Run `reqtrace check` → no L6 left for these cards. Print a progress note.
4. **Implement:** `workflow-implement.md`. It makes these tests pass and never rewrites them; a card test changes only through its card. After each Edit it adds the code tests of what it wrote (Stage 2 item 3, `tech-test.md` § Code tests): the cards are the minimum, not the suite. Size M skips Step `{n}R`; size L keeps it. Its Closing Review covers the slice. With a Builder, each step goes through § Builder. ❗ After each step closes or stops: update **Position** before its progress note (§ Progress note).
5. **Prove as a user:** run each Done-when through the real boundary: the browser for a UI, the process for a CLI, HTTP for an API, a consumer call for a library. A green unit suite is not proof. Tick `Met` only then. A Done-when that fails here is a failed `Verify` (Section 4.15). A Done-when whose run opens a window outside the integrated browser or changes global state runs only as approved on the **Host effects** line; otherwise leave `Met ⬜` and ask the person to run it at Gate B (`Proof: demo`).

```text
Wrong: The tests pass, so REQ-EXPORT-NO-DUPLICATE is `Met ✅`.
Right: Run `export create Report` twice in the built CLI, read the output, see the error and one row, then tick.
```

Two failed remedies of one root cause: write a card, mark the slice `⚠️` with `blocked by`, print a progress note, continue with the next (Section 4.15).

Never edit an `active` requirement to make it pass. A wrong requirement is a card.

## Builder

The session that runs `/evolve` is the **Lead**; start it with the strongest model you have. A **Builder** is a subagent on a cheaper model that turns one finished plan step into tests, code, and docs. The Lead decides; the Builder types.

**When:** size M or L, the tool can start a subagent on another model, and **Builder** is not `none`. Use the tool's own way to start it and pick its model: a model parameter, a named agent, or a setting. A condition fails, or the subagent can only inherit the Lead's model (it saves nothing): the Lead builds every step, and the run report says once: `Builder: none — {reason}`. ❗ Never a subagent for the plan, test cards, a review, or a gate (Section 4.14).

**Choose once, before the autonomous part:** the person names the model in the `/evolve` request, or the first Spec delta asks with its **Builder** line. Gate A writes it to `spec/state.md`. Never ask again; the person changes it by saying so. ❗ Write the name exactly as the person gives it. Never fill in a model, vendor, or version from an example or from memory: model names change weekly and differ per tool. The person's name does not resolve in the tool → `Builder: none — {name} not available here`, and say so in the run report.

| Work | Who | Why |
|------|-----|-----|
| Intake, Spec delta, gates, test cards with **Content**, plan with locked `How` | Lead | Judgment and the conversation with the person |
| Test code from the cards, production code from `How`, code tests from the written code, docs from `Docs`, `Verify` | Builder | Most tokens; the plan leaves no choice |
| Handoff check, Step `{n}R`, Closing Review, Prove as a user, Heal | Lead | The author never reviews its own work |

**Handoff:** one step at a time, one Builder at a time (one working tree). The brief holds only this; copy the depth, not the content:

```text
Build Step 3 of plans/plans_export.md. Read .github/copilot-instructions.md, then the whole plan.
Load: tech-csharp.md, tech-test.md, tech-tunit.md, tech-docs.md (the plan's Loaded skills line).
Do: workflow-implement.md Stage 2 items Edit, Tests, Docs, Verify, for this step only.
Write only: the step's Where files, the test files of its Tests cards and of its code tests, its Docs files.
Never: spec/**, the plan, another step, a test that differs from its card's Content, a git write,
a test that opens a window, touches the clipboard, settings, or environment variables, or floods processes
(tech-test.md § Host safety) unless its card names the approved effect.
Card tests first: write them, run them, and keep each failure line before writing code.
Code tests after the code: walk each member you added or changed (tech-test.md § Code tests).
Decide nothing of Decision class (Section 4.16): stop and report it.
Report: done | blocked — {reason} · the model you ran on · files changed · the Verify command and its
last lines · the red-run lines of the card tests · the Code tests ledger line · the Sync check line · each difference from How ·
user-visible behavior a code test showed that no requirement names · domain facts learned · open questions.
```

**Check every handoff.** Never trust the report:

1. `git diff --stat` shows only files the brief allows. Any other file: undo that edit, then treat it as a failed handoff.
2. Each new marked test, next to its card **Content**: the setup, action, and observable are there, and the red-run line failed for the expected reason (Stage 4 step 3). A test that cannot fail fails the handoff.
3. Each member the diff adds or changes is in the Code tests ledger line; each booked test exists and asserts an exact value, error code, or message (`tech-test.md` § Code tests). A member without a booking fails the handoff. A new test that is Visible, Global, or Heavy without an approved card, or a product branch on an environment variable, fails the handoff (`tech-test.md` § Host safety).
4. Each file of the step's `Docs` field is in the diff, and the Sync check line names it. A new component without its Overview row and component-diagram edges fails the handoff.
5. Run `Verify` yourself.
6. The model in the report differs from **Builder**: say so in the run report. Some tools fall back to another model without telling.
7. Write the reported domain facts (`workflow-implement.md` Stage 2 item 5). Put each reported user-visible behavior that no requirement names into the next Spec delta as a proposed card. Then continue that Stage 2 from item 7.

**A failed handoff:** sharpen the step's `How` with what was missing and hand it off once more. Fails again: the Lead builds the step. Two failed remedies of one root cause → card (Section 4.15).

```text
Wrong: The Builder reports "done, all green". The Lead ticks the step.
Right: The diff lists src/Store.cs and spec/tests/export.md. The brief did not allow the spec file: the Lead
       undoes that edit, quotes the "Never: spec/**" line, and hands the step off again.
```

## Stage 5 — Heal

Run `workflow-dream.md` in `touched` mode on the slice’s areas. Update **Batch**. Mark the slice `✅` in **Queue**. Print a progress note.

`Run: all`: Gate B per § Run all; the list below does not apply. `Run: slice`: Gate B when any holds:

- Every **Queue** line is `✅`.
- The batch holds 3 slices or ~1000 production lines.
- A change marked **Judge** (`tech-reporting.md`) is in the batch.

Otherwise go to Stage 3. Run `/dream full` after 10 slices or 20 inbox lines (ask at the next gate; never block on it).

## Run all

On request, build every slice of the queue in one run, with no stop between slices.

- ❗ Only a request in words sets it: `/evolve all`, "build all slices in one go", "alle Slices am Stück". Write `Run: all` in `spec/state.md` when the request comes. A question about the mode is no request: answer it.
- Gate A still stops. `Run: all` takes effect after its approval.
- After Stage 5, go to Stage 3 without stopping. Stage 3's one-slice-per-session rule does not apply.
- Gate B comes once: when no **Queue** line is `⬜` or `🚧`. Batch size and a **Judge** change do not trigger it. List each **Judge** change first in the briefing's **Attention**.
- A blocked slice: write its card, continue with the next `⬜`. No `⬜` left: Gate B when **Batch** holds a slice, else Stage 3 (Gate E). Show every open card in full at that Gate B (Gate E with Gate B).
- Hand each M and L step to the Builder (§ Builder): its work stays out of the Lead's context.
- Context: `spec/state.md` is current after every step. The tool summarizes the context → read this skill and `spec/state.md` again, continue at **Position** (Stage 0). The tool cannot summarize and reports under 20 % context left → finish the current step, save, stop `budget`. The run report says: continue with `/evolve resume`. `Run: all` stays.
- Gate B accept sets `Run: slice`.

```text
Wrong: "Build all slices in one go." After 3 slices the Lead stops at Gate B, per Stage 5.
Wrong: "Can evolve build all slices at once?" The Lead writes `Run: all`.
Right: "Build all slices in one go." The Lead writes `Run: all`, builds create-and-list, reject-duplicates,
       and import; rename waits on a card. Then one Gate B: one briefing for three slices, the rename card in full.
```

## Gate B — Release

Write one briefing for the batch (`tech-reporting.md` § Briefing). Show its head (**Verdict** to **Attention**), **Architecture**, and **Concept** in chat, with the link to the file. Set `Status: gate-release`. Stop.

- Accept → run `reqtrace accept`; it records the batch's new markers, so C2 guards them from now on. Refused → L4 or L6 on an entry of a slice not `✅` in **Queue**: accept again with `--allow-gap "<slice> is queued"` or `"<slice> is blocked by Q-…"`. Any other finding is an Error of the batch: fix it, `check`, accept again. Remove the `✅` lines from **Queue**. Reset **Batch**. Set `Run: slice`. `Status: idle`.
- Change → the change is new input: go to Stage 1.

## Progress note

Form, fields, and example: `tech-reporting.md` § Progress note. The Lead prints every note; a Builder never does.

❗ Print one at each point:

| Point | **Done now** names |
|-------|--------------------|
| Stage 0, after reading the state | `Resumed` and the last `✅` step or slice from the files |
| Gate A approved | The requirements set `active` and the slices cut |
| Stage 4 step 3, card tests red | The number of card tests, the command, and that each failed for the expected reason |
| Stage 4 step 4, each plan step closed or stopped | `workflow-implement.md` Stage 2 prints it with these fields |
| A slice blocked | The card title and which slice continues |
| Stage 5, slice `✅` | Each Done-when run as a user and what was observed |

Fill the fields from **Queue** and **Position**:

- Headline: `Evolve`, the slice, **Position**; `{met} of {total}` over the requirements of all **Queue** lines.
- `{Items}` is **Slices**: every **Queue** line in order; the `🚧` slice also shows `Step {n} of {total}`.
- **Waiting on you:** each card of `spec/open.md` by title. Next stop: Gate A, Gate B with its trigger (Stage 5 or § Run all), or `none`.

## Completion

Stop conditions, closed set:

| Stop | Meaning |
|------|---------|
| `gate` | Waiting at Gate A or B |
| `waiting` | Only cards remain |
| `budget` | Session limit reached; state saved |
| `idle` | Queue empty, batch accepted |

Chat: the run report (`tech-reporting.md`) on every exit. ❗ Its **Outcome** carries the **Slices** line and the `{met} of {total} requirements Met` count of § Progress note, `✅` slices included; **Not done** lists each slice not `✅`.
