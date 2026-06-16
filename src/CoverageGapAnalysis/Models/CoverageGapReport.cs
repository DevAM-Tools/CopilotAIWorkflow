// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Models;

/// <summary>Unified coverage gap report with exit gaps before branch gaps.</summary>
public sealed record CoverageGapReport(
    CoverageGapSummary Summary,
    IReadOnlyList<ExitCoverageGap> ExitGaps,
    IReadOnlyList<BranchGap> BranchGaps);
