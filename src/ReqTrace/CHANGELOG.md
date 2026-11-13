# ReqTrace changelog

All notable changes to the **ReqTrace** dotnet tool are documented here.
Repository-wide and agent-workflow notes live in the root [CHANGELOG.md](../../CHANGELOG.md).

## 0.2.0

### New

- `reqtrace syntax [requirement|card|marker|ignore]` prints the form the tool reads: an example requirement, test card, marker per language, and ignore line that pass the check unchanged, with every allowed field value. It reads and writes no file and runs outside a repository.

### Changed

- Form fixes quote the form. A missing field (F3) names the line to add with its indent, such as `Add the line "  - **Proof:** test"`; a bad value (F4) lists every allowed value. F1–F5 name the `syntax` call. No fix points to `tech-spec.md` any more. `check --json` keeps its fields and `schemaVersion` 1; only the `fix` text changed.

## 0.1.0

### New

- `reqtrace check` reads `spec/requirements/` and `spec/tests/` closed-world: an entry it cannot read is an error, never skipped. It finds one card marker per test in C# (`[Property("Card", "TEST-…")]`), Python (`@pytest.mark.card("TEST-…")`), and Rust (`// card: TEST-…`), and reports broken and missing links and duplicate IDs with file, line, and fix.
- `spec/trace.lock` holds the state a person accepted. A requirement whose text changed after its card was accepted fails until `reqtrace confirm` records that the test still proves it. A gap in an unchanged entry is counted, never failing.
- `--results <file-or-folder>` reads TRX test results; a folder means every `.trx` below it, and the newest result per test wins. A requirement ticked as met fails when its test is missing from the results or did not pass, and a result file older than a marked test file fails as stale.
- `reqtrace show <ID>`, `reqtrace report`, `reqtrace accept [--allow-gap "<reason>"]`, and `--json` (with `schemaVersion` 1).
- About 80 ms per run on Windows and 40 ms on Linux on a repository with 31 requirements. Framework-dependent: one package runs on every platform with the .NET 10 SDK. Verified on Windows x64 and Linux x64; macOS and ARM64 are not verified yet.
