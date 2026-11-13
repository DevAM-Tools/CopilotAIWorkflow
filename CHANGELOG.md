# Changelog

All notable changes to this project are documented in this file.

## Unreleased

Package release notes (full detail):

| Package | Version | Changelog |
|---------|---------|-----------|
| CSharpStyleChecker | 2.0.0 (breaking) | [src/CSharpStyleChecker/CHANGELOG.md](src/CSharpStyleChecker/CHANGELOG.md) |
| ExitPointGaps | 1.1.2 | [src/ExitPointGaps/CHANGELOG.md](src/ExitPointGaps/CHANGELOG.md) |
| ReqTrace | 0.2.0 | [src/ReqTrace/CHANGELOG.md](src/ReqTrace/CHANGELOG.md) |
| InvariantStringHelper | 1.0.0 (unchanged) | [src/InvariantStringHelper/CHANGELOG.md](src/InvariantStringHelper/CHANGELOG.md) |

### Packages

- Each package releases on its own version line and packs alone: `dotnet pack src/{Project}/{Project}.csproj -c Release`. `Directory.Build.props` sets no shared version; each package ships its own `PACKAGE_README.md` instead of the shared one. Steps: [docs/guides/release-a-package.md](docs/guides/release-a-package.md).
- A dotnet tool package holds exactly what its publish produced: the publish folder is emptied before each publish of a tool. ExitPointGaps 1.1.1 on nuget.org carried seven files of the pre-rename `CoverageGap.Tool`; 1.1.2 ships without them.
- **Rule change** one release path for every package: `dotnet pack src/{Project}/{Project}.csproj -c Release` writes `artifacts/release/{PackageId}.{version}.nupkg`. A build no longer packs, so a Release build leaves released packages alone (before: libraries and the analyzer were packed into `artifacts/` on every Release build). Before the pack builds, each project, project references included, empties its `bin/` output. A Debug pack, `--no-build`, and a pack from an IDE or `dotnet msbuild -t:Pack` stop with an error that names the command. The sample's packed-package check restores from `artifacts/release/` into a private packages folder. `tech-solution.md` § Release packages; [decision record](docs/decisions/release-pack-path.md).
- **Rule change** `tech-csharp.md` and `tech-solution.md`: reference CSharpStyleChecker `2.*` (was `1.*`). A `1.*` reference stays on 1.2.0 and its 160-character default.

### ReqTrace 0.2.0

- `reqtrace syntax [requirement|card|marker|ignore]` prints the spec form the tool reads, with every allowed value; it runs outside a repository too.
- Form fixes quote the line to add or every allowed value and name the `syntax` call; none points to `tech-spec.md`. `check --json` fields are unchanged.
- `tech-reqtrace.md` and `tech-spec.md` name `syntax`; `tech-reqtrace.md` says what to do on 0.1.0. `tech-spec.md` § Layout now names `## User stories` and `## Retired`.

### ReqTrace 0.1.0 (new)

- The dotnet tool `ReqTrace` (`dotnet tool install ReqTrace --version 0.*`, command `reqtrace`). The name `SpecTrace` belongs to an unrelated package on nuget.org.
- `reqtrace` checks the links between requirements, test cards, and tests: closed-world reading of `spec/`, markers in C#, Python, and Rust tests, comparison with the accepted state in `spec/trace.lock`, and test evidence from TRX files. About 80 ms per run on this repository on Windows, 40 ms on Linux. See [`docs/agents.md`](docs/agents.md).
- `/requirements`, `/plan`, `/implement`, `/review`, `/dream`, `/evolve`, the Spec delta, and the briefing use it; `tech-spec.md` § Integrity checks says what it covers, `tech-reqtrace.md` how to set it up, call it, and proceed without it.
- Pilot on ExitPointGaps: four exit-code behaviors are traced end to end.
- New `tech-reqtrace.md`, loaded with `tech-spec.md`: how to call the tool in this repository and in others, one-time setup as a local dotnet tool (install only after approval, exact version, `dotnet tool restore` on a fresh clone), which command runs at which workflow moment, exit codes, what to do and never do per rule, test evidence from TRX (.NET only), and the ignore list. `docs/agents.md` stays the product reference.
- **Rule change** every person's approval records the accepted state: `/requirements` approval of the Spec delta and `/plan` approval now set their entries `active` and run `reqtrace accept --allow-gap "<where the proof follows>"`; Gate A passes `--allow-gap` (before: plain `accept`, which refused new entries without tests); Gate B and an `accept` reply to the `/implement` run report record the new test markers, so a later deleted test is an error instead of an open gap. Entries are `proposed` until that approval (before: until Gate A only).
- `/plan` runs `reqtrace check` after writing cards and `show` before changing one; `/implement` step 9 and Stage 3 check with test evidence; `/review` treats exit `2` as a check that did not run and never accepts; `/dream` never accepts and leaves C3 of its own rewording to the next review.

### Workflow kit

- `/evolve` hands M and L plan steps to a Builder subagent on a cheaper model where the tool allows it; the session (Lead) plans, writes the test cards, checks every handoff (diff against the brief, each test against its card, `Verify` again, the model the Builder ran on), and reviews. The model is named in the request or asked once in the first Spec delta, and kept as `Builder:` in `spec/state.md`. New terms Lead and Builder in §5.1; [decision record](docs/decisions/evolve-builder.md).
- **Rule change** Section 4.1 and `tech-spec.md`: a test may carry the one marker of its test card. Production code still may not hold IDs. The card field `Code` is replaced by the marker. Requirements gain a required `Proof` field (`test`, `demo`, `review`, `waived`).

- New `/evolve` (`workflow-evolve.md`): continuous requirements-driven development with three human gates (Spec, Escalation, Release), work sized S, M, or L, tests first, proof as a user, and state in `spec/state.md`.
- New `/dream` (`workflow-dream.md`): consolidates domain knowledge, requirements, and test content and repairs the trace; never changes production code.
- New `tech-spec.md`: tracked `spec/` per area, stable IDs, requirement entries, test cards, one-home trace to code, integrity checks I1–I11, rules for editing shared knowledge, domain knowledge in `docs/domain/` with index and inbox.
- New `tech-reporting.md`: reader-first rules, attention marks, length budgets, decision cards, blocker form, Spec delta, briefing, run report. Blocker and run report forms move here from Section 4.16.
- New `tech-agent-access.md` and Section 4.18: every application has an agent surface unless a requirement waives it.
- New `tech-sweep.md`: views and Sweep move out of `workflow-council.md`, so `/plan` and `/review` no longer load the council skill.
- New `workflow-plan-templates.md` and `workflow-review-templates.md`: templates load at the writing stage.
- **Rule change** Section 4.16: every open point is Covered, Detail, Assumption, or Decision. Only Decision reaches a person, and other work continues. Grill Me asks Decision-class points only.
- **Rule change** Section 1: out-of-scope discoveries are recorded and the run continues, unless the scope depends on them.
- **Rule change** Section 3: print `Loaded: <skills>` before the first edit; read a skill again when it cannot be quoted.
- New Section 4.17 Context budget. New rule in Section 4.5: never delete or weaken a test to get green. Section 6: `/implement` also runs inside `/evolve` after Gate A.
- **Rule change** `/requirements`: requirements live per area in `spec/`, not per task in `requirements/req_<slug>.md`; Full and Delta mode; each entry is checked against the existing spec; test cards move from the plan to `spec/tests/`.
- **Rule change** the briefing is a reviewer’s guide: a one-screen head, a **Concept** section (the idea, a type-and-data-flow diagram, one row per new or changed type with its role, why it exists, and which similar type it must not be confused with), and a structured card with a reduced code snippet per file marked Judge or Sample. Routine files get one table row. Attention marks are the words Judge, Sample, and Routine; red, yellow, and green symbols are not used, because red reads as a failure.
- **Rule change** `tech-web.md`: breakpoints are 744, 1280, 1920, 2560 (were 768, 1024, 1280), floor test at 320 × 568; width policy per page type; tables become card lists and never scroll horizontally; overflow rules; icon language with tooltips, including keyboard and touch. `tech-playwright.md` asserts overflow at each viewport.
- `copilot-instructions.md` falls from 30 KB to about 22 KB: limit details move to `tech-limits.md` § Principles, and UI and doc details to their skills. `workflow-plan.md` falls from 28 KB to 11 KB plus 14 KB of templates, and `workflow-review.md` from 30 KB to 16 KB plus 8 KB of templates.
- **Rule change** `tech-instructions.md`: a workflow skill loads once for its task, so precision wins over size there. Budgets rise to 20 KB per workflow skill and 16 KB per templates companion. Every step leaves a checkable trace; every demanded artifact section has a template or a filled example; a term that draws a line gets a decision rule with one example per side; a table cell that only names a topic is not a rule.
- **Rule change** `/plan`: Grill Me records one line per topic in the Context Anchor **Topics** (`n/a — why` included); a topic without a line was not walked. The topic table says what to settle per topic. Test cards cover each applicable class per behavior or name an `Out`. `workflow-plan-templates.md` gains a filled step as worked example, a Wrong `How`, and templates for the Context Anchor, Slices, Edge Cases and Risks, `C{n}`, and the Task Checklist.
- **Rule change** `/review`: Category Rules decide the bucket by four ordered questions with an example each, and define High, Medium, and Low for Errors and Performance findings. Cosmetic means the fix changes no content (form only: order, layout, wording); which rule was missed does not decide the bucket. Templates for **Scope** and **Test Execution**; a bucket section without findings says `none`.
- `/implement`: each Stage 2 item names its trace (status surfaces, Deviation record, ticked Acceptance boxes, domain page); an `After` that cannot work as written is a Deviation or needs approval; four Wrong/Right pairs for the common shortcuts.
- `/requirements`: examples for splitting and keeping entries, for conflict versus narrower entry, and for Decision versus Assumption.
- `/evolve`: a filled `spec/state.md` with one **Queue** line per slice, a worked intake example, only explicit words approve at Gate A, the expected reason of a failing first test, size S reviews until clean, proof as a user also for libraries, and what Gate B shows in chat.
- `tech-instructions.md`: size budgets, a named load in the stage that needs it, a rule for when an example earns its place, and new coupling rows. A compaction keeps sentences that exclude a known shortcut, the examples inside a definition, and the check keywords of a check table; a pointer never replaces a checklist.
- Compaction check of `/plan`, `/review`, `/implement`, `/requirements`, and `copilot-instructions.md` against their previous text: the review hunts Security, Races, Correctness, Repo, Debug, Consistency, and Docs project name their check points again; `/implement` again forbids ticking `Met` because `How` alluded to the outcome and stopping “Review until clean” at zero Errors; Section 4.6 lists which documents are English, workflow folders included.
- **Rule change** Section 4.16: a question to a person states what it is about in plain words, what happens today with a concrete example, why it matters, and what each option gives the reader; four checks run before sending, with a Wrong/Right pair. `tech-reporting.md`: **Needs you**, the Spec delta, and Gate E show each decision in full; a link or ID alone is incomplete.
- **Rule change** `/illustrate`: a teaching design and storyboard come first; every motion shows a state change of the subject; forms per subject; interaction changes the outcome; the illustration simulates real logic; visual rules; teaching checks (Mute, Freeze, Replace, Insight, Real data) and an assert that stepping changes displayed values.
- `tech-csharp.md`, `tech-rust.md`, `tech-python.md` compacted: examples that repeated their rule removed or shortened. No rule removed. The limit comment example moves to `tech-limits.md`.
- Fix: `tech-rust.md` and `tech-python.md` no longer recommend host-derived limit defaults, which `tech-limits.md` forbids.
- New rule in `tech-csharp.md`: classes and structs whose construction only stores its parameters use a primary constructor; construction that must run a statement, such as a guard, keeps an explicit constructor.
- **Removed** `/complex-task` and `/review-loop`. Use `/evolve`, or `/plan` then `/implement`. `/implement` keeps its step reviews and Closing Review; `/docs-repair` and the Rust port workflows use the new `workflow-implement.md` § Review until clean.
- **Rule change** the maximum line length falls from 160 to 130 for C#, Rust, and Python. CSC001 defaults to 130 ([CSharpStyleChecker changelog](src/CSharpStyleChecker/CHANGELOG.md)).
- New `tech-solution.md` § Vulnerable packages: how to fix NU1901–NU1904 restore failures for direct and transitive packages, and which audit switches never to touch.
- **Rule change** `tech-csharp.md`: SIMD code branches widest first, `Vector512` → `Vector256` → `Vector128` → scalar, each width on its `IsHardwareAccelerated`.

## 1.2.0

Package release notes (full detail):

| Package | Version | Changelog |
|---------|---------|-----------|
| CSharpStyleChecker | 1.2.0 | [src/CSharpStyleChecker/CHANGELOG.md](src/CSharpStyleChecker/CHANGELOG.md) |
| ExitPointGaps | 1.1.1 | [src/ExitPointGaps/CHANGELOG.md](src/ExitPointGaps/CHANGELOG.md) |
| InvariantStringHelper | 1.0.0 (new) | [src/InvariantStringHelper/CHANGELOG.md](src/InvariantStringHelper/CHANGELOG.md) |

### Workflow kit

- `tech-solution.md` § Raising a diagnostic to error: raise single diagnostics by ID in the root `.editorconfig`, never by category or analysis level. `IDE0005` (unnecessary using) becomes an error; it can miss unused `global using` entries, so `/review` still checks `GlobalUsings.cs` for unused entries and order.
- **Rule change** `tech-solution.md` § Raising a diagnostic to error: every project raises `IDE0051` (private member never used), `IDE0052` (private member never read), `IDE0059` (unnecessary assignment), and `IDE0060` (unused parameter, also on public and protected methods through `dotnet_code_quality_unused_parameters = all`) to error. Unused locals and unused or never assigned fields already fail through `TreatWarningsAsErrors`. The fix is to delete the unused item with its call-site arguments, docs, and tests, never to suppress it (before: unused members and parameters passed the build).
- New `tech-limits.md` (loads on a trust-boundary limit in scope, Section 3 row): the limits mechanism for every technology, the decisions each project makes (no numbers in the kit), the audit as part of `/review`, and the reference implementations. The Limits bullet of `tech-csharp.md` names the packages `NetworkInspector.Limits` and `NetworkInspector.Settings`; `tech-rust.md` and `tech-python.md` say that none exists yet. New term `Limit definition`.
- `copilot-instructions.md` Section 4.2: when a project chose no posture for a limit, the default is secure by default (active, finite, never tighter than the host handles); "Neither wins by rule" is removed. The bullet that keeps a ceiling out of end-user reach stays: it is a decision of the application.
- `tech-test.md`: the limit test cases include `off` when the mechanism offers it.
- `workflow-review.md`: a Limits hunt. A bare bound at a trust boundary is an Error; a review whose scope names the audit classifies every bound and writes the register in a tracked document. The Security bullet points to `tech-limits.md` and `copilot-instructions.md` Section 4.2.
- `workflow-plan.md` Stage 1: check every existing member the plan names against the code by name; a snippet that calls a missing member is a gap. Stage 3: the limits topic names the register and the policy slots of `tech-limits.md`.
- `tech-test.md` § Allocation tests: measure in several rounds and assert on the smallest round.
- `tech-solution.md` § CSharpStyleChecker: build every touched project with the checker enabled and read all checker output for the touched files.
- `tech-limits.md`: fix each default as one number for the reference machine of the project; never derive a default from the memory or the processors at run time. The slot "Memory budget of all derived shares" and the exact-integer share rule are removed; the limit tests and the catalog test no longer name a machine or a budget. The C# reference implementation lists the live holder, the unit parts, and the duplicate check.- `tech-csharp.md` Limits bullet: one fixed default per limit for the reference machine instead of defaults derived from `GC.GetGCMemoryInfo()` and `Environment.ProcessorCount`; the example comment no longer calls a fixed constant the rejected option.- `docs/decisions/limits-mechanism.md`: the memory budget leaves the slot list; rejected option added: defaults derived from the machine.
- `tech-csharp.md` Limits bullet: one fixed default per limit for the reference machine instead of defaults derived from `GC.GetGCMemoryInfo()` and `Environment.ProcessorCount`; the example comment no longer calls a fixed constant the rejected option.
- `docs/decisions/limits-mechanism.md`: the memory budget leaves the slot list; rejected option added: defaults derived from the machine.

### InvariantStringHelper (new, 1.0.0)

- `Invariant($"…")` formats an interpolated string under the invariant culture and allocates only the result string. Targets `net6.0` and `net10.0`; no dependencies; `netstandard2.0` is not supported.

### CSharpStyleChecker

- **Breaking:** on .NET 6 and later CSC009 now reports `FormattableString.Invariant($"…")` and `Invariant($"…")` imported through `using static System.FormattableString;`; 1.1.0 accepted both. Reference InvariantStringHelper and change the one `using` to `global using static InvariantStringHelper.InvariantText;`; the call sites stay. `string.Create(CultureInfo.InvariantCulture, $"…")` is also accepted. `netstandard2.0` projects are unaffected.
- **Update required for projects that use InvariantStringHelper:** CSharpStyleChecker 1.1.0 reports the helper's `Invariant($"…")` as CSC009 and recommends the very call it rejects. 1.2.0 accepts the helper (recognized by symbol, not by name) and its CSC009 messages on .NET 6 and later recommend the package; `string.Create` stays accepted but is no longer suggested.
- The advice follows what the compilation can do, not the project's name: the package is recommended only on .NET 6 and later with C# 10 or later. `netstandard2.0`, `netstandard2.1`, .NET Framework, .NET 5, and a .NET 6 project pinned to C# 9 keep `FormattableString.Invariant`, and CSC009 names it there.
- Fix: an interpolated string in parentheses as a handler argument is no longer reported.

### ExitPointGaps

- Uses `Invariant($"…")` from InvariantStringHelper (was `FormattableString.Invariant`); the tool package ships the helper assembly. CLI surface and schema unchanged.

## 1.1.0

Package release notes (full detail):

| Package | Changelog |
|---------|-----------|
| CSharpStyleChecker | [src/CSharpStyleChecker/CHANGELOG.md](src/CSharpStyleChecker/CHANGELOG.md) |
| ExitPointGaps | [src/ExitPointGaps/CHANGELOG.md](src/ExitPointGaps/CHANGELOG.md) |

### CSharpStyleChecker

- **CSC009** (new): bare interpolated strings and culture-sensitive `ToString` require invariant culture (`Invariant($"…")`, `string.Create` / `StringBuilder.Append` with `CultureInfo.InvariantCulture`).
- **CSC001–CSC008** coverage and masking fixes (verbatim/raw string line length, more `var` sites, `ValueTask` / `WaitAll` / `WaitAny`, indexers, `[ExcludeFromCodeCoverage]` no longer waives CSC006).
- Repo clone: no nested MSBuild of the checker per `CoreCompile`; `ExitPoints.dll` wired as analyzer dependency after project references resolve.

### ExitPointGaps

- Builds clean under CSC009 (`Invariant($"…")`).
- Exit-point collection: `ExitPointEntry` is a `readonly record struct`; `Collect` takes optional `CancellationToken`; ternary groups use `QuestionToken`.
- CLI surface unchanged (`run` / `plan`, schema v3).

### Agent workflow

- Section 4.6 holds the documentation rules itself; `tech-docs.md` adds details and checks:
  - **Doc set:** `README.md` and `docs/architecture.md` are the minimum. `docs/concepts.md` (ideas of the solution across components), `docs/domain.md` (problem world: glossary, rules with source, external facts), and decision records in `docs/decisions/` are created with their first fact. Each has a one-line test for what belongs there.
  - **Reading tiers:** agents always read the README and the one-screen architecture **Overview**, then only the component sections in scope and the concept, domain, and decision entries for the task’s terms. Context stays small.
  - **Architecture as a contract:** the Overview lists allowed dependencies. A broken boundary, dependency, flow, or invariant needs user agreement, an architecture update, and a decision record; otherwise it is a defect.
  - **Docs move with code:** knowledge learned in a run lands in its home in the same run. **Impact search** before an edit, **Sync check** before ✅.
  - **Doc comments:** on every type and member, private included, stating the contract.
  - **References:** **Related** links only items bound in meaning, items this one depends on, and items easy to forget; never call sites. Couplings the compiler cannot check get `Sync:` at both ends, and the architecture component section names them under **Easy to miss**. No separate navigation file.
  - **Inline comments:** why and flow, never obvious syntax; tags `Invariant:`, `Perf:`, `Security:`, `Sync:`; no history; no file header; named sections in large files.
  - **Language contract:** every language skill defines its doc form, checked link, section marker, and doc lint.
  - **Templates:** `tech-docs-templates.md` holds README, architecture (Overview plus component sections), concepts, domain, decision record, and guide.
- The C#, Rust, and Python skills add the mechanism:
  - C#: XML docs with `<seealso cref>` on every member.
  - Rust: `# Related` sections, `missing_docs_in_private_items`, and `rustdoc::broken_intra_doc_links`.
  - Python: `Related:` docstring sections and private docstrings.
  - Section markers: `#region` / `// region:` / `# region`. Sections group members by content (one responsibility or flow stage with its state and helpers), never by member kind; C# drops the fixed `Fields` / `Lifecycle` / `Public API` / `Private helpers` regions.
  - Skill examples use a neutral invoice-import domain instead of the repository’s own tools.
- `.gitignore` adds `/illustrations/` and `/docs-repair/` to the ignored workflow history (`/plans/`, `/councils/`, `/reviews/`).
- New `/docs-repair` (`workflow-docs-repair.md`) for existing projects. It inventories docs and code units and audits each unit’s claims against code and tests. Drift is classified (`doc-stale`, `doc-missing`, `doc-misplaced`, `doc-orphan`, `comment-stale`, `code-drift`, `conflict`, `why-unknown`). Doc-side drift is fixed when the evidence decides. Self-contained `DRIFT-<AREA>-<TOPIC>` decision cards go to a person when it does not. Code-side fixes go to `/implement` as findings. The workflow backfills doc comments with Related links, the architecture Overview and component sections, concepts, and the domain doc. It keeps a resumable ledger in `docs-repair/<slug>.md`; durable answers move to `docs/`.
- Section 4.16 becomes **Run reports, blockers, and questions**:
  - **Run report:** every run of an editing workflow (`/implement`, `/review-loop`, `/complex-task`, `/docs-repair`, `/port-dotnet-rust-loop`) ends with the same report on every exit: Outcome, **Not done** (first, never in passing), Needs you, Problems, Deviations. Empty sections say `none`. It replaces the implement Open-Items Report.
  - **Blocker kinds:** a requirement not met, a conflict, and a failed check each state what a person needs to judge them (Done-when and observation; both sides quoted and the side followed; command and key output).
  - **Question form:** context starts with the task and where the run stands; options are suggestions, and a free-text answer is always possible. A choice tool without free text is not used; the question goes to chat. The `/plan` and `/requirements` Grill Me templates follow it.
- Status legend (Section 5) adds `🚧` **Started / In progress**. `⚠️` now means only at risk or blocked. `/implement`, `/review-loop`, and `/complex-task` set `🚧` when work on an item starts, `⚠️` only when a blocker stops it, and resume at `⚠️`, then `🚧`, then `⬜`.
- `/plan`: Stage 1 reads the docs in tiers and runs the Impact search. A design that breaks the architecture is a Grill Me question. Every step has a required `Docs` field. Target Solution has a **Doc delta**. Coverage adds a doc check.
- `/implement`: per step: edit → docs → Sync check → `Verify` → Step `{n}R`, scoped to the step’s change, which may reuse the step’s `Verify` run. The automatic Closing Exam is replaced by one full-scope **Closing Review** (`reviews/review_<slug>-closing_<n>.md`). It adds the Closing hunts: requirements end to end, “Verify proves”, leftovers, and briefing claims. Exam runs only on request after the Closing Review. `/complex-task` and `/council` follow.
- `/review`: a **Docs** hunt runs the Sync check and checks architecture conformance (a silent break is an Error even when the code works); Summary Checks splits `Docs` into `Docs code` and `Docs project` and adds `Closing requirements` and `Closing verify`.
- New `tech-instructions.md`: authoring rules for instructions and skills (imperative one-rule bullets, one home per rule, compaction without lost rules) and the workflow-kit change-coupling table. `copilot-instructions.md` is compacted: duplicate rules are merged, and no rule was dropped.
- This repository has an [architecture doc](docs/architecture.md) with an Overview and **Easy to miss** couplings per component.
- Plan test content IDs are `TEST-<AREA>-<TOPIC>`. The ID names the case.
  A case that proves a requirement reuses that requirement’s area and, when
  it is that one claim, its topic (`REQ-EXPORT-NO-DUPLICATE` →
  `TEST-EXPORT-NO-DUPLICATE`).
- `/plan` writes **API and interfaces** immediately before the steps: schematic snippets of the public API and of interfaces, traits, or equivalent contract types.
- `/review` Summary is structured: release line, counts, a findings table, a checks table (one row per hunt), and a priority list.
- `/review` Summary **Findings** has a Status column (Section 5 legend). A new
  review starts each row at `⬜`. `/implement` sets that cell and the finding
  Status line together (`🚧` before the edit, `⚠️` when a blocker stops it, `✅` after Verify). **Release**
  and **Counts** stay the review-time verdict. A deferred finding stays `⬜`.
- `/requirements` IDs are `REQ-<AREA>-<TOPIC>` (the ID names the subject). The artifact is an atomic, contradiction-free list. Headings, context, and notes do not get IDs.
- Plan and review artifacts welcome extra illustrative code. Plans prefer snippets over a prose-only `How` or Target Solution. Review findings keep fenced Problem/Fix (before/after) as the primary handoff.
- Section 4.6: file references in plans, briefings, reviews (and other non-temp docs) are clickable Markdown links with a relative target that opens. Bare backticks and absolute paths are not enough.
- `/implement` ends every run, complete or not, with the run report in chat (Stage 6, Section 4.16), each item linked. The briefing mirrors it in **Open**. `/complex-task` and `/review-loop` report once at their end or first stop.
- `/plan` writes **Expected Experience** directly after Step Overview: one `Result` block, or one block per phase that aggregates the step Experience (`Steps`, `After this phase a person can`, `They should see`, `How to try`, `Not yet`). `/implement` runs the phase `How to try` at phase close.
- Plan Step Overview, Task Checklist, and Test cases index link to the step and test blocks. `/review` Findings Overview, Summary **Findings**, and **Priority** link each finding ID to its block.
- Section 4.2 **Trust-boundary limits**: each limit names its threat; host-dependent values are configurable with a validated range and owner (code, operator, end user); each limit is labeled **secure by default** or **open by default** with a reason; end-user settings explain what, why, unit, range, default, and risk; a hit limit is a named error. Every security decision is commented where the code enforces it (threat, mitigation, rejected alternative, residual risk). `/requirements`, `/plan` (limits table in Target Solution), `/review`, council Skeptic, `tech-test.md`, `tech-web.md` (limit settings), and the C#, Rust, and Python skills follow it.
- Section 4.12 becomes **API Design and Misuse Prevention** and covers public and internal APIs. Goal: the caller cannot use an API wrongly, and the whole API set gives one clear way to do each task.
  - An API that can be misused is an Error. Preferred order: misuse does not compile; misuse is obviously wrong at the call site; misuse fails at run time with a named error. Named misuse sources: arguments that compile in the wrong order, flags that do not read at the call site, an unenforced call order, hidden effect or cost behind a property or getter-shaped name, a name with two meanings or look-alike names with different behavior, a surprising default, ownership the signature does not show.
  - **State-dependent use is the last resort.** Prefer stateless operations or types that encode the state. When it is unavoidable, the plan says why, each member documents its valid states, and a call in the wrong state fails with a named error.
  - **Coherent API:** the API set has no contradictions. Before adding an API, the existing ones for the same or a close task are extended or generalized. A parallel path (overload, wrapper, alias, sync/async pair, old and new shape) needs a caller need the existing path cannot meet and is weighed in Grill Me. Siblings share naming, parameter order, defaults, and error form; one word means one thing.
  - **Narrow surface:** no public member, parameter, or extension point without a named caller need.
  - **No type explosion:** misuse is fixed with the cheapest means first (clear name, named argument, parameter order, keyword-only). A new type needs an invariant, a third value or mutually exclusive choice, or use across several APIs; never one type per parameter or per `bool`. A single `bool` named for what `true` does is fine.
  - **Call-site test:** planning and review write the intended call and the most likely wrong call; the wrong call must not compile or must fail by name.
  - `/review` adds an **API** hunt that also covers pre-existing code in the reviewed files: call-site test, misuse sources, state dependence (avoidable = Error), parallel paths (different behavior or a new unweighed path is an Error, pre-existing redundancy a Refactoring), drift between siblings, surface. The Fix names the path that stays, how the others map onto it, and whether public callers break.
  - `/plan` asks whether a new API extends an existing one or adds a parallel path, and **API and interfaces** lists each likely wrong call with how it is rejected and the valid states of state-dependent members. `/implement` Align checks those lines and adds no unlisted API. `tech-test.md` requires a test per documented wrong call.
  - The C#, Rust, and Python skills add the mechanism (options type over overload ladders, clearly named `bool` parameters stay allowed, enums only for mutually exclusive choices, distinct types for same-typed parameters only for domain values with an invariant, typestate or factory instead of state-guarded members, cheap getters). The council Outsider flags two ways to do one task and calls that are easy to get wrong.
- Chat handover is not global. Each workflow skill’s Completion (or Output Modes) defines the ending.
- `/review-loop` default is full clean: remediate every bucket, then stop only when the latest review has zero open findings (or explicit deferrals). `/complex-task` still defers Cosmetic, Refactoring, and Performance so the plan can finish and Exam can run.
- `/review` file mode is the default for standalone `/review` as well as
  `/review-loop` and `/complex-task`. Chat-only only when the user says
  `chat only` / `ohne Review-Datei`.
- `/illustrate`: if the user allows libraries, download pinned allowlist files
  into `vendor/` at create time. Shipped HTML uses relative paths only and works
  offline (no CDN). Playwright verifies during create; it is not part of the
  illustration. Illustrations are not product web UIs: not responsive-by-default,
  and `tech-web.md` does not apply.
- README does not publish instruction line counts (they go stale). Terms (Grill Me,
  Step `{n}R`, Shared Block, MTP, …) live in `copilot-instructions.md` §5.1.
- `/review` Perspective Sweep is two views (Skeptic, Outsider). Plan Sweep stays
  five views. Do not paste the plan fence into a review file.
- Tool commands that set env vars show Unix (`NAME=value`) and PowerShell
  (`$env:NAME = "value"`). Illustration Verify uses the stack CLI in
  `tech-playwright.md`, not a `pnpm`-only fence.
- `tech-rust.md` UTF-8 console sample has `// SAFETY:` on the `unsafe` block.
- `tech-blazor.md` `[EditorRequired]` sample uses `required string` with no
  empty-string default.
- `/commit-message` writes a complete English message to `commit_message.md` and stops. Git commit/push after that file is out of scope.
- `/review` is a skeptical auditor new to the project: trust nothing, hunt the hair in the soup, check with absolute precision. Findings lock `How` plus Problem/Fix for a weaker agent. Ignore existing `reviews/review_*.md` unless the user names one. Load skills from the Section 3 table for the confirmed scope. The review **must** answer whether that scope is ready for public release (`Ready for public release` or `Blocked by {IDs}`). Leading criteria: `custom_instructions.md` when present, loaded skills, Section 4. Compact always-on distillate (security, races, docs, orphans). No second closer.
- C# UI tests live in `{App}.UiTest` (TUnit + `TUnit.Playwright`), not `{App}.E2E`. Playwright asserts named UI requirements, not smoke-only.
- Design apps and libraries so an agent can debug them (smallest command, filterable tests, traces).
- C# tests (unit, bUnit, Playwright UI) are TUnit only (`TUnit.Playwright` in `{App}.UiTest`). Playwright, web, Blazor, and test skills no longer duplicate journey/viewport/runner rules.
- Try* APIs surface failure as code, message, or type (`out` preferred; documented TLS last-error only when `out` cannot be added). Prefer auto-properties; explicit `volatile` fields; least visibility and least setter.
- Profile C# hot paths with `dotnet-trace` (Release only) when the user asks or the step is hot-path optimization. GC counts belong in that session, not production.
- Section 6 is the workflow router (load skill; `/implement` after plan approval). Stages stay in the skill.
- 100% exit-path coverage is a C# / TUnit gate (`tech-csharp.md`, `tech-tunit.md`), not an always-on rule.
- Trust-boundary limits (size, count, range, time, rate) must be agreed with the user; record constant vs configurable.
- `/requirements` records required behavior and expected properties (shall / shall not / shall exhibit). Tests in plans are `TEST-<AREA>-<TOPIC>`. Implementation stays in `/plan`. `/plan` links an existing requirements file; otherwise it runs requirements first.
- Plans lock implementation (`How` + Before/After) for a weaker executing agent, with first-class test **content** (`TEST-<AREA>-<TOPIC>` cards: scenarios, edges, constellations, contradictions, gaps), step experience, and public-API snippets. Class and Layer may be named. Test code in the plan is allowed; it does not replace Content. Extra tests at implement time are allowed.
- Plans **link** the requirements file (real Markdown path) and do not copy the requirement list. Tick `Met` in that file.
- Requirements are verified at step close (Done-when runs). IDs stay out of product code.
- Council runs in the same agent. No subagent advisors or peer-review round.
- Section 3 Tech Load maps `GlobalUsings.cs` to `tech-csharp.md` + `tech-solution.md`, `.editorconfig` to `tech-solution.md`, `global.json` to `tech-tunit.md`, and C# tests as `*Tests.cs` in `{Project}.Tests/` (not `*.Tests.cs`). C# copyright syntax lives in `tech-csharp.md` so it applies when only `.cs` files are in scope.
- `/commit-message` writes a specific message (named outcomes, before/after, examples), not a slogan. Updates `CHANGELOG.md` when the scope is a release.
- Briefings (not “review briefs”) are the post-implement packet (try-this, acceptance evidence, walkthrough, per-file Look at).
- Shared product web UI rules live in `tech-web.md` (phone-first layout, locked breakpoints, CSS/JS placement, a11y). Blazor keeps stack mechanics in `tech-blazor.md`. Playwright stays journeys and debug.
- Read and write files with stream/reader APIs. Do not slurp unbounded files into a single string or byte array.
- Performance is a feature: general rules in always-on instructions; GC, SIMD, and pools in the language skills. Gen-1 survival costs more than gen-0 on .NET hot paths. `ref struct` is a special-case stack tool, not a default.
- Dependencies need approval (what, why, license, alternatives). No shell/PowerShell/Python scripts in repo or chat except last resort with explanation.
- Test strategy lives in `tech-test.md`; TUnit, pytest, Rust, and Playwright skills hold runner mechanics only (no duplicated case-design rules). C# test methods are PascalCase without underscores (`PairEmptyRepoReturnsNone`).
- Warning suppressions always need user approval and a tech-skill template (rule id, reason, scope). Global C# `NoWarn` needs one comment per id, including why it is global (`tech-csharp.md`). `IDE1006` naming is EditorConfig-only (`tech-solution.md`); `NoWarn` does not clear the IDE squiggle.
- Non-temporary documents are English. Chat may follow the user.
- After writing a plan, walk it twice: requirements coverage and conversation coverage.

### Documentation

- `README.md` and `AGENTS.md` list the new workflows and tech skills.
- `tech-web.md` locks phone-first responsive layout (320px floor; 768 / 1024 / 1280 bands), CSS layers, and JS-vs-CSS rules. Playwright product journeys use those viewports.

---

## 1.0.0

### ExitPointGaps performance

- **Per-project parallelism** (default: all projects in scope concurrently); cap with `--max-parallelism <n>`
- **Solution pre-build** once before multi-project loops (skipped with `--no-build` or single project)
- **Streaming output:** aggregated `summary.json` is **schema v3** with `reportFile` indirection; per-project reports remain schema v1
- **Memory:** drop Roslyn `Compilation` after exit collection; scoped Cobertura parse; incremental per-project writes
- **Pipeline overlap:** Cobertura parse runs in parallel with Roslyn compile per project
- **Benchmark (CopilotAIWorkflow.slnx, Release, `--no-build`):** serial ~22.8s vs parallel ~12.0s (~47% faster)

### Breaking changes (aggregated JSON only)

| v2 | v3 |
|----|-----|
| `schemaVersion: 2` with embedded `report` objects | `schemaVersion: 3` with `projects[].reportFile` |
| Single-file multi-project `-o out.json` | Multi-project requires directory `-o reports/` or stdout `--stream` |

Migration: read `summary.json` for counts; load each `{reportFile}` for `exitGaps[]`.

### CLI

- `--max-parallelism <n>` — bounded concurrency (`n >= 1`)
- `--stream` / `--no-stream` — NDJSON stdout for multi-project runs without `-o`

### Rebrand (at this point)

- **ExitPointGaps** replaces `CoverageGap.Tool` + `CoverageGapAnalysis` (analysis integrated into tool assembly)
- **CSharpStyleChecker** replaces `CSharpStyleValidator`
- Version line restarts at 1.0.0 (not 3.0 continuation)
- CLI: `dotnet tool run exitpointgaps` (was `coveragegap`)
- Diagnostic IDs renamed **CSV001–CSV008** → **CSC001–CSC008** (`csc_max_line_length` editorconfig property)
- See migration table below

### Migration

| Old | New |
|-----|-----|
| `dotnet tool install CoverageGap.Tool` | `dotnet tool install ExitPointGaps` |
| `dotnet add package CoverageGapAnalysis` | use `ExitPointGaps` CLI (no library package) |
| `PackageReference CSharpStyleValidator` | `CSharpStyleChecker` |
| Analyzer rule IDs `CSV001–CSV008` | `CSC001–CSC008` |

---

> **Historical (pre-rebrand):** Entries below describe packages and names before 1.0.0.

## 2.1.2

### CSharpStyleValidator

- **CSV003:** Clarified exemptions — private members inside **private nested types**, **explicit interface implementations**, and **local functions** are no longer flagged; outer-type private members still require `_PascalCase`
- **CSV008:** Confirmed exempt when created type differs from contextual target (e.g. `IA a = new A()`, `IList<int> x = new List<int>()`); regression test added
- **CSV008:** Exempt array and collection initializers when the contextual target cannot use a collection expression (`Memory<T>`, `ReadOnlyMemory<T>`) or differs from the created type (e.g. `ReadOnlyMemory<byte> m = new byte[] { … }`)
- **CSV006:** Count `??` and `??=` as dual completion arms; multi-arm `?:`, `??`, `??=`, and `switch` exits share an operator group so line breaks cannot hide multiple completion paths
- **CSV007:** Plain `volatile` read/write allowed; forbid non-atomic `++`, `--`, and compound assignments (use `Interlocked`)

### Documentation

- Demo `CompliantExample` includes manual `IEnumerable<int>` with private nested enumerator
- `tech-csharp.md` documents CSV003 and CSV008 exemptions

## 2.1.0

### CSharpStyleValidator

- **CSV008:** Require target-typed `new()` and collection expressions `[]` instead of redundant explicit type names when the type is known from context (declaration, assignment, return, or argument)
- **Violations:** parameterless `new Type()`, empty object initializer `new Type() { }`, `new List<T>()`, collection initializer `new List<T> { … }`, array initializer `new T[] { … }`
- **Exemptions (unchanged behavior):**
  - Constructor calls with arguments: `new Foo(1)`
  - Object initializers with members: `new Foo() { Prop = value }`
  - Polymorphic construction: `Base b = new Derived()`
  - Interface or base-type targets: `IList<int> x = new List<int>()`
  - Generic type parameters: `T t = new T()`
  - `throw new ArgumentException()` and similar throw contexts
  - Rank-sized arrays without element initializer: `new int[n]`

### Documentation

- `tech-csharp.md` and `copilot-instructions.md` aligned with CSV008 exemptions and allocation guidance
- Demo sample: isolated CSV008 violation file; Csv005 no longer cross-triggers CSV008

## 2.0.0

### CoverageGap.Tool

- **Single-call gate:** `dotnet tool run coveragegap` / `… run` runs tests, collects Cobertura, and reports exit gaps
- **Zero-arg default:** from repo root, gates all production projects in the auto-discovered solution
- **Scoped targets:** `run project <csproj>...`, `run solution [path]`, `plan project <csproj>`
- **Parallel-safe:** unique `--work-dir` per invocation; relative `-o` under work dir; default stdout
- **Solution gate:** projects run sequentially; parallel per-project runs planned for a future release
- **Gate scope:** `run` gates class-library production projects; executable hosts (`OutputType` `Exe`) are excluded (including the tool itself)
- **Cancellation:** Ctrl+C and Unix SIGINT/SIGTERM cancel in-flight `dotnet` subprocesses
- **Isolated Cobertura:** results read only from the invocation work directory, not a global `TestResults/` scan

### Documentation

- README, `PACKAGE_README.md`, and `tech-tunit.md` document the CLI workflow

## 1.1.0

### CSharpStyleValidator

- Repo clone: auto-apply via root `Directory.Build.targets` for `netstandard2.0`+ and .NET Core App projects (including Roslyn source generators)
- NuGet: analyzers load from `analyzers/dotnet/cs` (bundled `ExitPoints.dll`); no MSBuild targets in the package
- Sample: `samples/CSharpStyleValidator.Demo/` — CSV* violations, source generator, packed NuGet verification

### Documentation

- README Quickstart with NuGet shields, consumer TFM matrix, and minimum SDK (Roslyn 4.14 / SDK 9.0.300+)
- `PACKAGE_README.md` requirements per package
- CoverageGap.Tool usage in `tech-tunit.md`
- Central package version in `Directory.Build.props` (`VersionPrefix`)

### Packaging

- Symbol package (`.snupkg`) for `CoverageGapAnalysis`

## 1.0.0

Initial public release.

### CSharpStyleValidator (NuGet)

Roslyn analyzer package enforcing CopilotAIWorkflow C# style as compiler errors:

| ID | Rule |
|----|------|
| CSV001 | Line length ≤ 160 (strings/comments masked) |
| CSV002 | No `var` |
| CSV003 | Private `_PascalCase` |
| CSV004 | No `.Result` / `.Wait()` on `Task` |
| CSV005 | `using` only in `GlobalUsings.cs` |
| CSV006 | At most one exit point per source line per callable |
| CSV007 | `volatile` fields: plain read/write allowed; non-atomic ops require `Interlocked` |

### CoverageGap.Tool (global tool)

- `coveragegap run` — test, collect Cobertura, and gate exit-point coverage
- `coveragegap plan` — export exit-point manifest without running tests

### CoverageGapAnalysis (library)

Cobertura parsing, branch-gap analysis, and exit-point comparison for agent-oriented coverage workflows.

### Agent workflow

- Exit-point coverage gate in `copilot-instructions.md` §4.5 and `tech-tunit.md`
- Blazor/Razor: exhaustive bUnit component tests per `tech-blazor.md`

### Requirements

- .NET SDK 10.0 (`global.json` pins 10.0.100)
- Target framework `net10.0` for applications in this repository; analyzer packages target `netstandard2.0` and apply to consumers from `netstandard2.0` / `net5.0`+
