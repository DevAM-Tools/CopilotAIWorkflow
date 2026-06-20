// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Aggregated gate report across multiple production projects.</summary>
internal sealed class AggregatedGateReport
{
    public required AggregatedGateSummary Summary { get; init; }

    public required IReadOnlyList<ProjectGateResult> Projects { get; init; }
}

/// <summary>Summary metrics for an aggregated gate report.</summary>
internal sealed class AggregatedGateSummary
{
    public int ProjectCount { get; init; }

    public bool GatePassed { get; init; }

    public int ExitGapCount { get; init; }

    public int BranchGapCount { get; init; }
}
