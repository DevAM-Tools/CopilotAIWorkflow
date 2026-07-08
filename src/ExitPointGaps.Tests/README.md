# ExitPointGaps.Tests

Merged test project for **ExitPointGaps** (CLI + analysis in one assembly).

## Layout

| Folder | Area | Coverage |
|--------|------|----------|
| `Cli/` | CLI | Parse, orchestration, integration |
| `Analysis/` | Analysis | Cobertura read/compare, report build, branch scope |
| `Helpers/` | both | `CoberturaFixtures`, `TempWorkspace`, `Gap*Workspace` |

Single `ProjectReference` → `../ExitPointGaps/ExitPointGaps.csproj`.
