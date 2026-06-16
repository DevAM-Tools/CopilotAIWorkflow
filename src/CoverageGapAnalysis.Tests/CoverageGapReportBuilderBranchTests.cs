// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests;

/// <summary>Branch-coverage tests for <see cref="CoverageGapReportBuilder"/> and related APIs.</summary>
public sealed class CoverageGapReportBuilderBranchTests
{
    [Test]
    public async Task Build_AllGapsClosed_GatePassed()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        CoverageGapReport report = CoverageGapReportBuilder.Build(
            [],
            [document!],
            [scoped],
            @"C:\repo",
            false);

        await Assert.That(report.Summary.GatePassed).IsTrue();
        await Assert.That(report.Summary.BranchRate).IsEqualTo(1d);
    }

    [Test]
    public async Task Build_BranchGapsPresent_GateFailed()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        CoverageGapReport report = CoverageGapReportBuilder.Build(
            [],
            [document!],
            [scoped],
            @"C:\repo",
            false);

        await Assert.That(report.Summary.GatePassed).IsTrue();
        await Assert.That(report.Summary.BranchGatePassed).IsFalse();
        await Assert.That(report.Summary.BranchGapCount).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_ExitGapsPresent_GateFailed()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            @"C:\repo\src\Sample\Foo.cs",
            20,
            4,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        CoverageGapReport report = CoverageGapReportBuilder.Build(
            [exit],
            [document!],
            [scoped],
            @"C:\repo",
            false);

        await Assert.That(report.Summary.GatePassed).IsFalse();
        await Assert.That(report.Summary.ExitGapCount).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_WithSnippets_PopulatesSnippetFields()
    {
        string root = Path.Combine(Path.GetTempPath(), $"snippet-{Guid.NewGuid():N}");
        string sourceDir = Path.Combine(root, "src", "Sample");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllLinesAsync(
            Path.Combine(sourceDir, "Foo.cs"),
            Enumerable.Range(1, 9).Select(static i => $"// line {i}").Append("partial line"));

        string xml = CoberturaFixtures.PartialBranchXml.Replace(@"C:\repo", root, StringComparison.Ordinal);
        string cobertura = CoberturaFixtures.WriteTemporaryFile(xml);
        CoberturaReader.TryRead(cobertura, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(
            document!,
            new BranchGapScope(["Sample"]));

        CoverageGapReport report = CoverageGapReportBuilder.Build(
            [],
            [document!],
            [scoped],
            root,
            true);

        await Assert.That(report.BranchGaps[0].Snippet).IsEqualTo("partial line");
    }

    [Test]
    public async Task Build_LowBranchRateWithoutLineGaps_GateFailed()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.75" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="0.75" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="0.75" line-rate="1">
                      <lines>
                        <line number="1" hits="1" branch="True" condition-coverage="100% (2/2)">
                          <conditions>
                            <condition number="0" type="jump" coverage="100%" />
                          </conditions>
                        </line>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """);

        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        CoverageGapReport report = CoverageGapReportBuilder.Build(
            [],
            [document!],
            [scoped],
            @"C:\repo",
            false);

        await Assert.That(report.Summary.BranchGapCount).IsEqualTo(0);
        await Assert.That(report.Summary.BranchRate).IsEqualTo(0.75d);
        await Assert.That(report.Summary.GatePassed).IsTrue();
        await Assert.That(report.Summary.BranchGatePassed).IsFalse();
    }

    [Test]
    public async Task ExitCoverageComparer_MergedHitsPreferHigherValues()
    {
        string fullPath = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        string exitPath = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        CoberturaReader.TryRead(fullPath, out CoberturaDocument? full, out string? _);
        CoberturaReader.TryRead(exitPath, out CoberturaDocument? exitDoc, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:10:Return",
            @"C:\repo\src\Sample\Foo.cs",
            10,
            4,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
            [exit],
            [full!, exitDoc!],
            @"C:\repo",
            false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CoberturaReader_ReadsMethodSignatureName()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="1" line-rate="1">
                      <methods>
                        <method signature="()">
                          <lines>
                            <line number="2" hits="1" branch="False" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="2" hits="1" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """);

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(document!.Lines.Count).IsEqualTo(1);
    }
}
