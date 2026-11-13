# Test cards — Trace

### TEST-TRACE-UNREADABLE-LINE

- **Proves:** REQ-TRACE-UNREADABLE-LINE
- **Class:** error
- **Layer:** unit
- **Content:** A requirement file has a free-text line between two entries and an unknown field line inside an entry. Each is reported with file, line, and expected form. Prose before the first area heading and under `## User stories` is not reported. Catches a parser that silently skips what it does not understand.
- **State:** active · **Status:** ✅

### TEST-TRACE-NEAR-MISS

- **Proves:** REQ-TRACE-NEAR-MISS
- **Class:** error
- **Layer:** unit
- **Content:** Entries headed `req-export-x`, `REQ_EXPORT_X`, `REQ-1`, and `*REQ-EXPORT-X*` are each reported as a malformed ID. A valid entry beside them is still read.
- **State:** active · **Status:** ✅

### TEST-TRACE-MISSING-FIELD

- **Proves:** REQ-TRACE-MISSING-FIELD
- **Class:** error
- **Layer:** unit
- **Content:** One requirement lacks Done when, one has State `maybe`, one lacks Proof. A card lacks Proves. Each is named with its ID and field.
- **State:** active · **Status:** ✅

### TEST-TRACE-FIX-SHOWS-FORM

- **Proves:** REQ-TRACE-FIX-SHOWS-FORM
- **Class:** error
- **Layer:** unit
- **Content:** One requirement lacks Proof; its fix holds the line `  - **Proof:** test`. One has Proof `maybe`; its fix lists `test`, `demo — {reason}`, `review — {reason}`, `waived — {reason}`. One has Met `🟢`; its fix lists `⬜`, `✅`, `❌`. A card has Class `smoke`; its fix lists all seven classes. A card lacks Content; its fix holds a line that starts with `- **Content:** ` at column 0. Each fix names `reqtrace syntax requirement` or `reqtrace syntax card`, matching the entry kind. Catches a fix that names the field but not its form, and a requirement fix that shows card indentation.
- **State:** active · **Status:** ✅
### TEST-TRACE-COUNTS

- **Proves:** REQ-TRACE-COUNTS
- **Class:** happy
- **Layer:** unit
- **Content:** A repository with 3 requirements, 2 cards, 1 marker, and 1 ignored path prints exactly those counts. An empty spec prints zeros and passes.
- **State:** active · **Status:** ✅

### TEST-TRACE-DUPLICATE-ID

- **Proves:** REQ-TRACE-DUPLICATE-ID
- **Class:** error
- **Layer:** unit
- **Content:** The same ID heads an entry in two area files, and a card ID appears twice in one file. Both locations are named per duplicate.
- **State:** active · **Status:** ✅

### TEST-TRACE-UNKNOWN-REFERENCE

- **Proves:** REQ-TRACE-UNKNOWN-REFERENCE
- **Class:** error
- **Layer:** unit
- **Content:** A marker names `TEST-GAPS-EXIT-GAPP`; a card proves a retired requirement; a card proves an unknown one. Each is reported at its location. The misspelling suggests `TEST-GAPS-EXIT-GAP`. With no close ID, no suggestion appears.
- **State:** active · **Status:** ✅

### TEST-TRACE-UNPROVEN

- **Proves:** REQ-TRACE-UNPROVEN
- **Class:** boundary
- **Layer:** unit
- **Content:** Requirement with no card; requirement whose only card is retired; card with no marker; requirement with one active and one retired card (no gap); card with two markers (no gap).
- **State:** active · **Status:** ✅

### TEST-TRACE-PROOF-KINDS

- **Proves:** REQ-TRACE-PROOF-KINDS
- **Class:** boundary
- **Layer:** unit
- **Content:** Requirements with `demo — x`, `review — x`, and `waived — x` report no gap and are counted per kind. `review` without a reason and an unknown kind are reported.
- **State:** active · **Status:** ✅

### TEST-TRACE-MARKER-LANGUAGES

- **Proves:** REQ-TRACE-MARKER-LANGUAGES
- **Class:** happy
- **Layer:** unit
- **Content:** A C# `[Property("Card", "…")]` above a `Task` method, a Python `@pytest.mark.card("…")` above `def`, and a Rust `// card: …` above `fn` each yield ID, file, line, and method name. Other attributes between marker and method are skipped. A marker with no method within 8 lines is reported.
- **State:** active · **Status:** ✅

### TEST-TRACE-NO-PRODUCTION-MARKS

- **Proves:** REQ-TRACE-NO-PRODUCTION-MARKS
- **Class:** absence
- **Layer:** unit
- **Content:** Accepted repository whose production files contain no ID. The check passes and no finding names a production file.
- **State:** active · **Status:** ✅

### TEST-TRACE-SHOW

- **Proves:** REQ-TRACE-SHOW
- **Class:** happy
- **Layer:** unit
- **Content:** `show` on a requirement prints its text, cards, and each marked test with file, line, and method. `show` on a card names its requirements. An unknown ID exits 2 with the closest match.
- **State:** active · **Status:** ✅

### TEST-TRACE-VANISHED

- **Proves:** REQ-TRACE-VANISHED
- **Class:** error
- **Layer:** unit
- **Content:** An accepted requirement is deleted: reported, exit 1. A deleted area file reports each of its IDs. A whole file rewritten as a table: reported as holding no readable entry and each ID as vanished, and the counts show 0. An entry moved to `## Retired` with `Replaced by` is not vanished.
- **State:** active · **Status:** ✅

### TEST-TRACE-MARKER-REMOVED

- **Proves:** REQ-TRACE-MARKER-REMOVED
- **Class:** error
- **Layer:** unit
- **Content:** A card had one marked test at accept. The test is deleted: reported, exit 1. With two markers and one deleted: no finding. The card retired together with the deletion: no finding.
- **State:** active · **Status:** ✅

### TEST-TRACE-CHANGED-VISIBLE

- **Proves:** REQ-TRACE-CHANGED-VISIBLE
- **Class:** happy
- **Layer:** unit
- **Content:** "always" → "usually" shows exactly those words as removed and added. A whitespace-only or line-wrap change is not a change. A Done-when change counts as a change.
- **State:** active · **Status:** ✅

### TEST-TRACE-SUSPECT

- **Proves:** REQ-TRACE-SUSPECT
- **Class:** error
- **Layer:** unit
- **Content:** A requirement changes. Its two cards are unconfirmed, check exits 1, `accept` refuses. `confirm` on one leaves the other open. After both, check passes and `report` shows both notes. `confirm` without a note exits 2. Changing the requirement again re-opens both.
- **State:** active · **Status:** ✅

### TEST-TRACE-WEAKENED

- **Proves:** REQ-TRACE-WEAKENED
- **Class:** boundary
- **Layer:** unit
- **Content:** Flagged: "always" removed, "usually" added, 30 → 60, "shall" → "should". Not flagged: a typo fix with no such word. The flag does not change the exit code by itself.
- **State:** active · **Status:** ✅

### TEST-TRACE-CLAIM-NEEDS-PASS

- **Proves:** REQ-TRACE-CLAIM-NEEDS-PASS
- **Class:** error
- **Layer:** unit
- **Content:** Met ✅ with a TRX where the marked test failed; where it is absent; where it passed (no finding); where the method ran with arguments, so its result name carries `(…)`. A method name matching two results in different files is reported as ambiguous, not as passed.
- **State:** active · **Status:** ✅

### TEST-TRACE-NO-RESULTS-HONEST

- **Proves:** REQ-TRACE-NO-RESULTS-HONEST
- **Class:** absence
- **Layer:** unit
- **Content:** Met ✅ and no `--results`: output says evidence not checked, exit code unaffected, no passed count. A given path that does not exist exits 2.
- **State:** active · **Status:** ✅

### TEST-TRACE-STALE-RESULTS

- **Proves:** REQ-TRACE-STALE-RESULTS
- **Class:** boundary
- **Layer:** unit
- **Content:** Result file older than the marked test file: stale, not passed. Same time or newer: used. A malformed TRX file exits 2 and names the file.
- **State:** active · **Status:** ✅

### TEST-TRACE-ONLY-CHANGE-BLOCKS

- **Proves:** REQ-TRACE-ONLY-CHANGE-BLOCKS
- **Class:** boundary
- **Layer:** unit
- **Content:** 50 accepted gaps plus one correct new requirement with card and marker: exit 0, states 50 open gaps. A new requirement with no card: exit 1 naming only it. A broken line in an unchanged file still fails, because reading errors always count.
- **State:** active · **Status:** ✅

### TEST-TRACE-ALLOW-GAP

- **Proves:** REQ-TRACE-ALLOW-GAP
- **Class:** error
- **Layer:** unit
- **Content:** `accept` on a new gap refuses and names it. With `--allow-gap "reason"` it writes the lock and `report` shows the reason. An empty reason exits 2. A first accept on a repository with no lock records all gaps without a reason and is labelled as the starting point.
- **State:** active · **Status:** ✅

### TEST-TRACE-IGNORE-PATH

- **Proves:** REQ-TRACE-IGNORE-PATH
- **Class:** boundary
- **Layer:** unit
- **Content:** A listed folder is not scanned and appears in `report`. A line without a reason is an error. A pattern that leaves the repository root (`../x`) is an error. `bin`, `obj`, `target`, `node_modules`, and `.git` are always skipped.
- **State:** active · **Status:** ✅

### TEST-TRACE-ACTIONABLE

- **Proves:** REQ-TRACE-ACTIONABLE
- **Class:** happy
- **Layer:** unit
- **Content:** Every rule produces a finding whose location, message, and fix are non-empty, and whose text names the concrete ID or line. Walks the whole rule table, so a new rule cannot ship without a fix text.
- **State:** active · **Status:** ✅

### TEST-TRACE-SYNTAX

- **Proves:** REQ-TRACE-SYNTAX
- **Class:** happy
- **Layer:** unit
- **Content:** The entry block printed by `syntax requirement` is written unchanged under an area heading of a requirement file, and the block printed by `syntax card` unchanged into a test-card file. `check` exits 0 with `Read: 1 requirements · 1 test cards`. The line printed by `syntax ignore`, written to `spec/trace.ignore`, raises no L7. Catches a printed form that the reader would reject.
- **State:** active · **Status:** ✅
### TEST-TRACE-SYNTAX-MARKER

- **Proves:** REQ-TRACE-SYNTAX
- **Class:** happy
- **Layer:** unit
- **Content:** Each marker line printed by `syntax marker` (C#, Python, Rust), placed directly above a test method in a test file of its language, counts as one marker of `TEST-AREA-TOPIC`. Catches a printed marker the scanner does not find.
- **State:** active · **Status:** ✅
### TEST-TRACE-SYNTAX-CLOSED-SETS

- **Proves:** REQ-TRACE-SYNTAX
- **Class:** collection
- **Layer:** unit
- **Content:** The output of `syntax` holds every value of every closed set the reader accepts (requirement State, Met, Proof; card State, Status, Class, Layer). The expected values come from the reader's own value lists, not from the test, so a value added to the reader and missing from the output fails. Catches drift between reader and printed help.
- **State:** active · **Status:** ✅
### TEST-TRACE-SYNTAX-KINDS

- **Proves:** REQ-TRACE-SYNTAX
- **Class:** boundary
- **Layer:** unit
- **Content:** `syntax` with no kind prints the four sections requirement, card, marker, ignore in that order. Each single kind prints only its section. It runs in a temporary folder without `spec/index.md` and exits 0. `--help` lists `syntax`. Catches a command that still demands a repository.
- **State:** active · **Status:** ✅
### TEST-TRACE-SYNTAX-UNKNOWN-KIND

- **Proves:** REQ-TRACE-SYNTAX
- **Class:** error
- **Layer:** unit
- **Content:** `syntax frobnicate` and `syntax card extra` exit 2; standard error names the four kinds `requirement`, `card`, `marker`, `ignore`; standard output stays empty. Catches a silent fallback to all sections on a typo.
- **State:** active · **Status:** ✅
### TEST-TRACE-ONE-COMMAND

- **Proves:** REQ-TRACE-ONE-COMMAND
- **Class:** happy
- **Layer:** unit
- **Content:** No arguments from the root and from a nested folder run the check. Outside any repository with a spec: exit 2 with a message that names `spec/index.md`. An unknown option exits 2.
- **State:** active · **Status:** ✅

### TEST-TRACE-REPORT

- **Proves:** REQ-TRACE-REPORT
- **Class:** collection
- **Layer:** unit
- **Content:** With no changes the report says so. With one change of each kind each section lists exactly one item. With two changed requirements, both appear in ID order.
- **State:** active · **Status:** ✅

### TEST-TRACE-JSON

- **Proves:** REQ-TRACE-JSON
- **Class:** happy
- **Layer:** unit
- **Content:** `check --json` output parses, holds `schemaVersion`, `counts`, and `findings` with `rule`, `severity`, `file`, `line`, `message`, `fix`. Field names are locked by the test. No extra text precedes the JSON.
- **State:** active · **Status:** ✅

### TEST-TRACE-SABOTAGE

- **Proves:** none — guards the whole design against an agent that works badly; each row maps to several requirements.
- **Class:** trust-boundary
- **Layer:** integration
- **Content:** One repository, one accepted state. Each sabotage is applied alone and must end with exit 1 naming the entry: malformed ID, deleted requirement, deleted marked test, misspelled marker, Met ticked with a failing test, requirement reworded without confirming, every entry reformatted, duplicate ID pasted. The sabotages the tool cannot catch (marker on the wrong test; test that asserts nothing) are listed as passing, so the limit stays visible.
- **State:** active · **Status:** ✅
