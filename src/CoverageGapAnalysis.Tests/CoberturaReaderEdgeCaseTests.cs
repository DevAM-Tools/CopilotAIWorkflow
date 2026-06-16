// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests;

/// <summary>Additional edge-case tests for coverage analysis APIs.</summary>
public sealed class CoberturaReaderEdgeCaseTests
{
    [Test]
    public async Task TryRead_EmptyCoverageData_ReturnsError()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0" line-rate="0" version="1.9">
              <packages></packages>
            </coverage>
            """);

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(document).IsNull();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task TryRead_LineConditionCoverageFallback_ParsesGap()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Sample" branch-rate="0.5" line-rate="0.5">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="0.5" line-rate="0.5">
                      <lines>
                        <line number="5" hits="0" branch="True" condition-coverage="50% (1/2)" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """);

        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task FindUncoveredBranches_FullScopedRate_SkipsLineGaps()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ToCompact_IncludesMethodAndSnippetWhenPresent()
    {
        BranchGap gap = new BranchGap(2, "Sample", "Foo", "src/Foo.cs", 3, 0, 0.5d, 1, "M", "if (x)");
        ExitCoverageGap exitGap = new ExitCoverageGap(1, "id", "src/Bar.cs", 2, 1, "Return", "Bar", 0, "return 1;");
        CoverageGapReport report = new CoverageGapReport(
            new CoverageGapSummary(0.5d, 1, 1, 2, false, false),
            [exitGap],
            [gap]);

        string compact = CoverageGapReportFormatter.ToCompact(report);
        string text = CoverageGapReportFormatter.ToText(report);
        string json = CoverageGapReportFormatter.ToAgentJson(report);

        await Assert.That(compact.Contains("method=M", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text.Contains("exit gaps:", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(json.Contains("snippet", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task PathNormalizer_ReadSnippet_InvalidPath_Throws()
    {
        await Assert.That(() => PathNormalizer.TryReadSnippet(string.Empty, "file.cs", 1))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task CoberturaDiscovery_MissingRoot_ReturnsEmpty()
    {
        IReadOnlyList<string> files = CoberturaDiscovery.FindLatestFiles(
            ["missing-root-folder"],
            CoberturaDiscovery.DefaultTestProjectPackages);

        await Assert.That(files.Count).IsEqualTo(0);
    }
}
