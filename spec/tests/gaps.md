# Test cards — ExitPointGaps (pilot)

### TEST-GAPS-EXIT-PASS

- **Proves:** REQ-GAPS-EXIT-PASS
- **Class:** happy
- **Layer:** integration
- **Content:** A sample project in which every exit point is reached. The gate runs and ends with code 0 and an exit-gap count of 0.
- **State:** active · **Status:** ✅

### TEST-GAPS-EXIT-GAP

- **Proves:** REQ-GAPS-EXIT-GAP
- **Class:** error
- **Layer:** integration
- **Content:** The same sample project with one exit point no test reaches. The gate ends with code 1 and a positive exit-gap count.
- **State:** active · **Status:** ✅

### TEST-GAPS-EXIT-USAGE

- **Proves:** REQ-GAPS-EXIT-USAGE
- **Class:** error
- **Layer:** unit
- **Content:** The run command receives an invalid `--format` value and ends with the usage error code.
- **State:** active · **Status:** ✅

### TEST-GAPS-ZERO-TESTS

- **Proves:** REQ-GAPS-ZERO-TESTS
- **Class:** absence
- **Layer:** integration
- **Content:** A project whose test project holds no tests. The gate ends with code 1 instead of reporting success on nothing.
- **State:** active · **Status:** ✅
