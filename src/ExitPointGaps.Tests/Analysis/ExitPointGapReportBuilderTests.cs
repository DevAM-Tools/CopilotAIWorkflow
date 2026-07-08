// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

/// <summary>Tests for <see cref="ExitPointGapReportBuilder"/> and <see cref="ExitPointGapReportFormatter"/>.</summary>
public sealed class ExitPointGapReportBuilderTests
{
    [Test]
    public async Task Build_ExitAndBranchGaps_OrdersExitGapsFirstInCompactOutput()
    {
        string branchPath = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        string exitPath = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        CoberturaReader.TryRead(branchPath, out CoberturaDocument? branchDocument, out string? _);
        CoberturaReader.TryRead(exitPath, out CoberturaDocument? exitDocument, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            @"C:\repo\src\Sample\Foo.cs",
            20,
            4,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        ExitPointGapReport report = ExitPointGapReportBuilder.Build(
            [exit],
            [branchDocument!, exitDocument!],
            [new ScopedCoberturaDocument(branchDocument!, new BranchGapScope(["Sample"]))],
            @"C:\repo",
            false);

        string compact = ExitPointGapReportFormatter.ToCompact(report);

        await Assert.That(report.Summary.GatePassed).IsFalse();
        await Assert.That(report.ExitGaps.Count).IsEqualTo(1);
        await Assert.That(report.BranchGaps.Count).IsEqualTo(1);
        await Assert.That(compact.StartsWith("exit:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(compact.Contains("branch:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(compact.IndexOf("exit:", StringComparison.Ordinal))
            .IsLessThan(compact.IndexOf("branch:", StringComparison.Ordinal));
    }

    [Test]
    public async Task ToAgentJson_ContainsExitGapsBeforeBranchGaps()
    {
        ExitPointGapReport report = new ExitPointGapReport(
            new ExitPointGapSummary(1d, 1, 1, 2, false, false),
            [new ExitCoverageGap(1, "id", "a.cs", 1, 1, "Return", "M", 0, null)],
            [new BranchGap(2, "Sample", "Foo", "b.cs", 2, 0, 0.5d, 1, "M", null)]);

        string json = ExitPointGapReportFormatter.ToAgentJson(report);

        await Assert.That(json.IndexOf("\"exitGaps\"", StringComparison.Ordinal))
            .IsLessThan(json.IndexOf("\"branchGaps\"", StringComparison.Ordinal));
    }

    [Test]
    public async Task ToText_IncludesSummaryAndGaps()
    {
        ExitPointGapReport report = new ExitPointGapReport(
            new ExitPointGapSummary(0.5d, 0, 1, 1, true, false),
            [],
            [new BranchGap(2, "Sample", "Foo", "b.cs", 2, 0, 0.5d, 1, "M", null)]);

        string text = ExitPointGapReportFormatter.ToText(report);

        await Assert.That(text.Contains("Branch gaps:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text.Contains("failed", StringComparison.Ordinal)).IsTrue();
    }
}
