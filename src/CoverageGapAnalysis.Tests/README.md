# CoverageGapAnalysis.Tests layout

`tech-tunit.md` prefers one `<ClassName>Tests.cs` per production class. This project intentionally groups additional scenarios in companion files when branch-coverage work would make a single file unwieldy:

| Production class | Primary test file | Companion files |
|------------------|-------------------|-----------------|
| `BranchGapAnalyzer` | `BranchGapAnalyzerTests.cs` | `CoverageGapAnalysisRemainingBranchTests.cs` |
| `CoberturaReader` | `CoberturaReaderTests.cs` | `CoberturaReaderEdgeCaseTests.cs`, `CoverageGapAnalysisApiCoverageTests.cs` |
| `CoverageGapReportBuilder` | `CoverageGapReportBuilderTests.cs` | `CoverageGapReportBuilderBranchTests.cs` |

Shared fixtures live under `Helpers/` only.
