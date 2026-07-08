// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis.Models;

/// <summary>Unified exit-point gap report with exit gaps before branch gaps.</summary>
public sealed record ExitPointGapReport(
    ExitPointGapSummary Summary,
    IReadOnlyList<ExitCoverageGap> ExitGaps,
    IReadOnlyList<BranchGap> BranchGaps);
