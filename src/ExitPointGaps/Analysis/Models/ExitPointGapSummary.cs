// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis.Models;

/// <summary>Aggregate counts for a exit-point gap report.</summary>
/// <param name="BranchRate">Minimum scoped package branch rate (informational).</param>
/// <param name="ExitGapCount">Uncovered exit points (release gate).</param>
/// <param name="BranchGapCount">Uncovered branch conditions (informational).</param>
/// <param name="TotalGapCount">Exit plus branch gap count.</param>
/// <param name="GatePassed"><see langword="true"/> when <paramref name="ExitGapCount"/> is zero (release gate).</param>
/// <param name="BranchGatePassed">Informational; <see langword="true"/> when branch rate is 100% and no branch gaps.</param>
public sealed record ExitPointGapSummary(
    double BranchRate,
    int ExitGapCount,
    int BranchGapCount,
    int TotalGapCount,
    bool GatePassed,
    bool BranchGatePassed);
