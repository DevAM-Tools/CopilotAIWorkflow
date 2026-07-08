// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis;

/// <summary>Builds unified exit-point gap reports.
/// Thread-safe; all members are stateless.
/// </summary>
public static class ExitPointGapReportBuilder
{
    /// <summary>Builds a report from exit points and scoped Cobertura documents.</summary>
    /// <param name="exits">Exit points from Roslyn collection.</param>
    /// <param name="allDocuments">All Cobertura documents for line-hit merging.</param>
    /// <param name="scopedDocuments">Per-test-project scoped documents for branch analysis.</param>
    /// <param name="repositoryRoot">Repository root.</param>
    /// <param name="includeSnippets">Whether to include source snippets.</param>
    /// <returns>Report with exit gaps before branch gaps.</returns>
    public static Models.ExitPointGapReport Build(
        IReadOnlyList<ExitPoints.ExitPointEntry> exits,
        IReadOnlyList<Models.CoberturaDocument> allDocuments,
        IReadOnlyList<Models.ScopedCoberturaDocument> scopedDocuments,
        string repositoryRoot,
        bool includeSnippets)
    {
        ArgumentNullException.ThrowIfNull(exits);
        ArgumentNullException.ThrowIfNull(allDocuments);
        ArgumentNullException.ThrowIfNull(scopedDocuments);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        IReadOnlyList<Models.ExitCoverageGap> exitGaps =
            ExitCoverageComparer.Compare(exits, allDocuments, repositoryRoot, includeSnippets);

        IReadOnlyList<Models.BranchGap> branchGaps =
            BranchGapAnalyzer.FindUncoveredBranches(scopedDocuments, repositoryRoot, includeSnippets);

        double branchRate = BranchGapAnalyzer.GetMinimumScopedBranchRate(scopedDocuments);
        int exitGapCount = exitGaps.Count;
        int branchGapCount = branchGaps.Count;
        int totalGapCount = exitGapCount + branchGapCount;
        bool exitGatePassed = exitGapCount == 0;
        bool branchGatePassed = branchRate >= 1d && branchGapCount == 0;

        Models.ExitPointGapSummary summary = new Models.ExitPointGapSummary(
            branchRate,
            exitGapCount,
            branchGapCount,
            totalGapCount,
            exitGatePassed,
            branchGatePassed);

        return new Models.ExitPointGapReport(summary, exitGaps, branchGaps);
    }
}
