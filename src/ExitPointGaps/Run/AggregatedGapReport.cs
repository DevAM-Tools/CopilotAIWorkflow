// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Aggregated exit-point gap report across multiple production projects.</summary>
internal sealed class AggregatedGapReport
{
    public required AggregatedGapSummary Summary { get; init; }

    public required IReadOnlyList<ProjectGapResult> Projects { get; init; }
}

/// <summary>Summary metrics for an aggregated exit-point gap report.</summary>
internal sealed class AggregatedGapSummary
{
    public int ProjectCount { get; init; }

    public int CompletedProjectCount { get; init; }

    public bool GatePassed { get; init; }

    public int ExitGapCount { get; init; }

    public int BranchGapCount { get; init; }
}
