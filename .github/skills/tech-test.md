# Test Strategy

Load whenever tests or production APIs are in scope. Content and case design live here. Runner and asserts live in the technology test skill (`tech-tunit.md`, `tech-pytest.md`, `tech-rust-test.md`, `tech-playwright.md`).

Implements Section 4.5 in `copilot-instructions.md`.

## What exhaustive means

Exhaustive is **behavior classes**, not value enumeration.

Cover:

- happy path
- expected errors / `Err` / false
- boundaries: empty, min, max, off-by-one
- collections of size 0, 1, and 2
- `null` / absence when the type allows it
- concurrency when shared state exists
- trust-boundary / misuse cases for public APIs
- each limit (Section 4.2): at the limit, one above, a configured value out of range, `off` when the mechanism offers it, and the default when unset

```text
Wrong: [Arguments(0), (1), (2), … (int.MaxValue)] for Clamp
Right: empty, one element, two elements, min, max, min-1, overflow-relevant, null if possible
```

Do not iterate every integer, every string, or every permutation “to be sure”.

## Speed

- Each unit test finishes in ≤ 100 ms (Section 4.5). Slower tests carry the stack’s slow or integration marker. Do not `Sleep` / `time.sleep` / `thread::sleep`.
- Do not use network, real clock waits, or an extra process when a deterministic fake will do. Host effects: § Host safety.
- Assert one behavior per test. Parametrize related inputs; do not copy-paste.

## Host safety

Tests run on the machine a person works on, often while an agent runs them. ❗ A test run leaves the machine usable and unchanged: no window appears, no global state changes, the machine keeps responding.

Classify each test by its effect on the host:

| Effect | Examples | Default run |
|--------|----------|-------------|
| Contained | In-process code, fakes, the test’s own temp directory, a loopback port the OS picks, a headless browser, a child process the test owns | Allowed |
| Visible | A window, dialog, tray icon, notification, or sound; focus or cursor taken; a headed browser; desktop UI automation | Never |
| Global | Clipboard, registry, user or machine settings, user or machine environment variables, writes outside the test’s temp directory, system time, certificates, services, printers, network configuration, other processes | Never |
| Heavy | Over § Resource budget; load or stress runs; a run longer than the agreed time budget | Never |

Count the agent tool’s integrated browser as Contained: it runs inside the editor (Section 4.8).

- ❗ Design for Contained. Put each OS service (clipboard, window, dialog, file picker, registry, clock, process start) behind a narrow interface. Test the logic through a fake; keep the real adapter a pass-through with no branch.
- ❗ A card or test that needs Visible, Global, or Heavy is Decision class (Section 4.16). Ask before writing it: in `/plan` Stage 3, under `/evolve` in the Spec delta. Never write one on your own judgment “to be safe”.
- Offer these options. A requirement that is expensive to show needs no automated test when the person agrees:

| Option | The person gets |
|--------|-----------------|
| A) Fake only | Logic tested against a fake; the adapter shown by review (`Proof: review — {reason}` when nothing else proves the requirement) |
| B) Opt-in host test | A test that runs only when a person starts it by name (§ Opt-in); never in the default run or an agent `Verify` |
| C) Demo | The person tries it once (`Proof: demo — {reason}`) |
| D) No proof | `Proof: waived — {reason}` |

- Record the answer: plan Context Anchor **Topics** line `Host`, the card **Content** or **Out**, the requirement **Proof**. The test’s doc comment names the effect, never the question ID (Section 4.1).
- An unapproved Visible, Global, or Heavy test is an Error in review. Rewrite it to Contained, or ask.

```text
Wrong: ClipboardTests writes the real clipboard. The text the person copied is gone after each run.
Right: CopyRowCommand takes IClipboard. The test asserts the fake received "Report;3".
       Card Out: real clipboard write — adapter is one call, shown by review (Host answer, Topics).
```

### Environment variables

- ❗ Never switch product behavior for a test through an environment variable, a registry key, or a global flag. Inject it: constructor, options object, command-line argument, the test host’s configuration.
- ❗ Never set an environment variable of the test process: parallel tests share it and child processes inherit it. Pass a child process its variables in its start info.
- Allowed: runner configuration of the machine (browser channel, cache folder: `tech-playwright.md` § Browser selection), read once, never product behavior.

```text
Wrong: if (Environment.GetEnvironmentVariable("APP_TEST_MODE") == "1") { skipDialog = true; }
Right: new ExportCommand(fileDialog); the test passes a fake dialog that returns a temp path.
```

### Resource budget

Apply these defaults to the default run. Change them only with the person’s answer (Heavy).

- ❗ Run at most `max(1, logical cores / 2)` test-started processes at once: app instances, browsers, CLIs. Run at most 4 browser instances; open a new context per test, never a new browser.
- ❗ Start the app under test once per run or per class, never per test.
- ❗ Give every started process a timeout. Kill its whole process tree in teardown, also when the test fails. Leave no test-started process after the run.
- Keep generated test data ≤ 100 MB per test, on disk and in memory. Never busy-wait.
- Cap with the runner: `tech-tunit.md` § Fixtures and parallelism · `tech-pytest.md` § Markers · `tech-rust-test.md` § Host tests · `tech-playwright.md` § Host safety.

### Opt-in

- Mark an approved Visible, Global, or Heavy test so it runs only when a person starts it by name: `[Explicit]` (`tech-tunit.md`), the `host` marker (`tech-pytest.md`), `#[ignore]` (`tech-rust-test.md`).
- Restore in teardown what it changed (clipboard content, setting). Name the effect and the run command in its doc comment.

## Allocation tests

- ❗ Measure allocation in several rounds and assert on the smallest round. A real per-item allocation shows in every round; one measurement fails under load.

## Runs

Minimize how long a run takes. Every stack.

- Run .NET tests with `dotnet test`. Filter syntax: `tech-tunit.md`.
- When a full run is not required, run only the tests that cover the change. Use that stack’s filter (`tech-tunit.md`, `tech-pytest.md`, `tech-rust-test.md`, `tech-playwright.md`).
- Run the whole test project or solution when the change can break tests you cannot name, when the user asks for a full run, or at final verification (`workflow-implement.md` Stage 3).

## Web UI (Playwright)

When a product web UI is in scope, **use Playwright intensively** to prove what the user can see and do. Do not treat a green unit/component suite as UI coverage.

When requirements or the plan name **concrete UI behavior** (requirement ID / `TEST-<AREA>-<TOPIC>`: visible text, validation, navigation, viewport, keyboard, enabled/disabled), each named behavior needs an observable Playwright assert. A generic “page loads” journey is not enough.

Run journeys headless and within § Resource budget (§ Host safety). Cover the same **behavior classes** as above, in the browser: happy path, expected errors, empty/min/max, viewports in `tech-web.md`, and trust-boundary messages the user can read. Plan those cases as `TEST-<AREA>-<TOPIC>` **Content**.

Do **not** skip journeys to keep the suite small. Do **not** assert every pixel, screenshot, or CSS rule. Assert observable UI: heading, row, error text, URL, focus, enabled/disabled, layout that would break the task (clipped primary action, required horizontal scroll).

Trace on failure. No `waitForTimeout` as sync. Mechanics: `tech-playwright.md`. Component logic stays in the component runner (`tech-blazor.md`), not Playwright. **C# project:** `{App}.UiTest`.

## Two layers

A suite has two layers. Each catches what the other cannot see.

| Layer | Derived from | Written | Lives |
|-------|--------------|---------|-------|
| Card test | A requirement: what an actor observes | Content before the code (`/plan` Stage 6, `/evolve` Stage 2) | Card in `spec/tests/`; the test carries its marker |
| Code test | The written code: each member, branch, guard | In the step that wrote the code, after its Edit | Test project only; no card, no marker |

- ❗ The cards are the agreed minimum, never the whole suite. All cards `✅` does not mean the code is tested.
- ❗ A step or finding fix is not done until its code tests exist and its ledger line is written (§ Code tests).

## Card tests

Write test **content** as `TEST-<AREA>-<TOPIC>` cards in `spec/tests/` (`tech-spec.md`). Agree the **minimum**: important scenarios, edges, special constellations, contradictions, gaps, what is out, and the time budget. Test code in the plan is allowed; it does not replace Content.

Implement those cards as steps. Do not treat a green build as a test case.

## Code tests

The plan cannot know every branch; the written code decides how many exist. After each Edit, walk every member the Edit added or changed and book each item:

| Item | Book |
|------|------|
| Branch | Each branch and early return |
| Error path | Each failure result, error code, and guard throw |
| Parameter guard | Each rejected value, absence where the type allows it, and the first accepted value |
| Loop | 0, 1, and 2 iterations; first and last element |
| Arithmetic | Each overflow guard, at the maximum (Section 4.1) |
| State | Each transition, and one refused transition |
| Limit | The limit cases of § What exhaustive means |
| Claim | Each thread-safety claim: a concurrent test. Each allocation claim: § Allocation tests |

- Book each item as one of: a new code test · the card test that already proves it, by ID · `n/a — {reason}`. Never write a second test for what a card test proves.
- Test a private member through the nearest internal or public member. Never widen visibility for a test (Section 4.9).
- A code test passes on its first run, because the code exists. Make it able to fail: assert the exact value, error code, or message. `no exception`, `not null`, or `count > 0` alone is no assertion.
- A code test that shows user-visible behavior no requirement names (a message, a refusal, a default) is a spec gap. Keep the test, then raise the behavior: `/implement` → **Needs you** in the run report; `/evolve` → a proposed card in the next Spec delta. Never let it become an obligation silently.
- A code test changes only with the member it tests, in a planned step or an accepted finding. Never to get green (Section 4.5).

Record one ledger line where the step or finding closes, next to the Sync check:

```text
Code tests: Export.Create — empty name, name at limit, one above limit, duplicate (= TEST-EXPORT-NO-DUPLICATE);
            ExportStore.Contains — absent, case differs · 5 added · n/a: ExportStore.Dispose (no branch)
```

```text
Wrong: Every card of Step 2 is ✅, so Step 2 is tested. The overflow guard in `Total.Add` and the empty-file
       branch in `Reader.Read` have no test.
Right: After the Edit, walk `Total.Add` and `Reader.Read`. Add `AddAtMaxReturnsOverflow` and
       `ReadEmptyFileReturnsNoRows`. Write both into the ledger line.
```

## Design

- Use real deterministic implementations. Mock only external or non-deterministic dependencies.
- Write tests that can fail. A test that cannot fail is not coverage.
- Name tests `unit_scenario_expected` unless the tech test skill says
  otherwise. **C#:** PascalCase methods (`tech-tunit.md`). The plan ID
  (`TEST-<AREA>-<TOPIC>`) goes into the test's marker (`tech-spec.md` § Trace), never into the method name.
- Separate Arrange, Act, Assert with a blank line.
- Cover Windows/Linux/macOS, x64/ARM64 unless the user scoped otherwise.

## Debugging

On failure: run the smallest filter from the tech skill, then debug (Section 4.15). Treat file locks and parallel-agent runners as collision before rewriting product code (Section 4.14).
