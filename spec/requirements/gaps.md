# Requirements — ExitPointGaps (pilot)

Only the exit codes of the gate are specified, to try the trace on existing code. Everything else stays unspecified on purpose.

### Exit codes

- **REQ-GAPS-EXIT-PASS** — The tool shall exit with code 0 when no exit-point gap remains.
  - **Done when:** A person runs the gate on a fully covered project and the process ends with code 0.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-GAPS-EXIT-GAP** — The tool shall exit with code 1 when an exit-point gap remains.
  - **Done when:** A person runs the gate on a project with one untested exit point and the process ends with code 1.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-GAPS-EXIT-USAGE** — The tool shall exit with code 2 on invalid options.
  - **Done when:** A person passes an invalid value for `--format` and the process ends with code 2.
  - **State:** active · **Met:** ✅
  - **Proof:** test
- **REQ-GAPS-ZERO-TESTS** — The tool shall fail the gate when no test ran.
  - **Done when:** A person runs the gate on a project whose test project holds no tests and the process ends with code 1.
  - **State:** active · **Met:** ✅
  - **Proof:** test
