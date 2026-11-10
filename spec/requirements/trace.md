# Requirements — Trace

### Reading

- **REQ-TRACE-UNREADABLE-LINE** — The tool shall report every line inside a requirement or test-card area that matches no known form, with file, line number, and the expected form.
  - **Done when:** An agent adds a free-text line between two requirement entries, runs the check, and sees that file, that line number, and the expected form, with a failing exit code.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-NEAR-MISS** — The tool shall report text at an entry position that resembles a requirement or test ID but breaks the ID form.
  - **Done when:** An agent writes an entry headed `req-export-x`, runs the check, and sees it named as a malformed ID, not ignored.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-MISSING-FIELD** — The tool shall report an entry that lacks a required field or holds a value outside its closed set.
  - **Done when:** An agent removes the Done-when line of one requirement, runs the check, and sees that requirement and the missing field named.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-COUNTS** — The tool shall state on every run how many requirements, test cards, markers, and ignored paths it read.
  - **Done when:** A person runs the check on a repository with 3 requirements and sees the count 3 in the output.
  - **State:** active · **Met:** ✅
  - **Proof:** test

### Links

- **REQ-TRACE-DUPLICATE-ID** — The tool shall report an ID that heads more than one entry.
  - **Done when:** An agent copies a requirement entry into another area file, runs the check, and sees both locations named.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-UNKNOWN-REFERENCE** — The tool shall report a reference to a requirement or test card that does not exist or is retired, and name the closest existing ID.
  - **Done when:** An agent misspells a test ID in a test marker, runs the check, and sees the marker location and the correct spelling suggested.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-UNPROVEN** — The tool shall report an active requirement with proof kind test that no active test card proves, and an active test card that no test marker names.
  - **Done when:** A person adds a requirement without a test card and adds a test card without a marked test, runs the check, and sees both listed.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-PROOF-KINDS** — The tool shall accept the proof kinds test, demo, review, and waived on a requirement, and shall not demand a test for the last three.
  - **Done when:** A person marks a requirement `review — {reason}`, runs the check, and sees no gap reported for it and the review counted separately.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-MARKER-LANGUAGES** — The tool shall recognize test markers in C#, Python, and Rust test files.
  - **Done when:** A person places one marker in a file of each language and sees all three listed for their test cards by `show`.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-NO-PRODUCTION-MARKS** — The tool shall not require any mark in production code.
  - **Done when:** A repository with markers only in test files and none in production files passes the check.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-SHOW** — The tool shall list, for a given requirement or test-card ID, its text and every linked entry and marked test.
  - **Done when:** An agent runs `show` with a requirement ID and sees its text, its test cards, and each test's file, line, and method name.
  - **State:** active · **Met:** ✅
  - **Proof:** test

### Change

- **REQ-TRACE-VANISHED** — The tool shall report an entry that was accepted earlier and no longer exists, unless it was retired.
  - **Done when:** An agent deletes an accepted requirement entry, runs the check, and sees the ID named as vanished with a failing exit code.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-MARKER-REMOVED** — The tool shall report a test card that had a marked test at the last accepted state and has none now.
  - **Done when:** An agent deletes a marked test, runs the check, and sees the test card named with a failing exit code.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-CHANGED-VISIBLE** — The tool shall show each requirement whose text changed since the last accepted state, with the removed and added words.
  - **Done when:** An agent changes "always" to "usually" in an accepted requirement, a person runs `report`, and sees that requirement with "always" removed and "usually" added.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-SUSPECT** — The tool shall treat each test card proving a changed requirement as unconfirmed until someone confirms it with a note.
  - **Done when:** An agent changes a requirement's text, the check fails naming its test card, and after `confirm` with a note the note appears in `report` and the check passes.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-WEAKENED** — The tool shall flag a requirement change that removes a strong word, adds a weak word, or changes a number.
  - **Done when:** An agent changes "at most 30" to "at most 60" in a requirement, a person runs `report`, and sees it flagged as possibly weakened.
  - **State:** active · **Met:** ✅
  - **Proof:** test

### Evidence

- **REQ-TRACE-CLAIM-NEEDS-PASS** — The tool shall report a requirement ticked as met whose marked tests are missing from the results or did not pass.
  - **Done when:** An agent ticks Met on a requirement whose test failed, runs the check with the result file, and sees the requirement and the failing test named.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-NO-RESULTS-HONEST** — The tool shall state that evidence was not checked when it was given no result file, and shall not count that as passed.
  - **Done when:** A person runs the check without a result file and sees "evidence not checked".
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-STALE-RESULTS** — The tool shall treat a result file older than a marked test file as no evidence for that test.
  - **Done when:** A person edits a marked test file after the test run, runs the check with the old result file, and sees that test reported as stale.
  - **State:** active · **Met:** ✅
  - **Proof:** test

### Friction

- **REQ-TRACE-ONLY-CHANGE-BLOCKS** — The tool shall fail only for problems in entries or markers changed since the last accepted state, and for reading and identity errors; other gaps shall be listed without failing.
  - **Done when:** A repository holds 50 accepted gaps, an agent changes one unrelated requirement correctly, runs the check, and it passes while stating 50 open gaps.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-ALLOW-GAP** — The tool shall accept a new open gap only with a stated reason, and shall list that reason in `report`.
  - **Done when:** A person runs `accept` on a new requirement without a test: it refuses; with `--allow-gap "reason"` it succeeds, and `report` shows the reason.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-IGNORE-PATH** — The tool shall skip a path listed with a reason in the ignore file, and shall list every ignored path in `report`.
  - **Done when:** A person adds a folder with a reason to the ignore file and sees markers there not scanned and the folder listed in `report`. An entry without a reason is reported as an error.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-ACTIONABLE** — The tool shall state for each finding where it is, what is wrong, and what to do.
  - **Done when:** A person reads any finding and can name the file, the problem, and the fix without opening the tool's documentation.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-ONE-COMMAND** — The tool shall run with no arguments from anywhere inside a repository that has a spec.
  - **Done when:** A person runs the tool without arguments from a subfolder and receives the check result.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-SPEED** — The tool shall exhibit a check time of at most 200 milliseconds on this repository, without building or running anything.
  - **Done when:** A person runs the check on this repository with the tool already built and it finishes within 200 milliseconds.
  - **State:** active · **Met:** ✅
  - **Proof:** demo — wall time depends on the machine, so a person measures it once.
- **REQ-TRACE-REPORT** — The tool shall print one report for a person that lists changed, weakened, vanished, and retired requirements, unconfirmed test cards, allowed gaps, proof-kind changes, ignored paths, and counts.
  - **Done when:** An agent performs one of each change, a person runs `report`, and sees each listed once.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-TRACE-JSON** — The tool shall offer its findings in a documented JSON form with a schema version.
  - **Done when:** An agent runs `check --json` and reads a finding's rule, file, line, message, and fix from stable fields.
  - **State:** active · **Met:** ✅
  - **Proof:** test
