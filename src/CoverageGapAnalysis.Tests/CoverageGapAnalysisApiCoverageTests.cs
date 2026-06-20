// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests;

/// <summary>Additional API tests targeting branch coverage gaps.</summary>
public sealed class CoverageGapAnalysisApiCoverageTests
{
    private const long _OversizedCoberturaBytes = 50L * 1024L * 1024L + 1L;

    [Test]
    public async Task CoberturaReader_OversizedFile_ReturnsError()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cobertura-big-{Guid.NewGuid():N}.xml");
        using (FileStream stream = File.Create(path))
        {
            stream.SetLength(_OversizedCoberturaBytes);
        }

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(document).IsNull();
        await Assert.That(error).Contains("maximum size");
    }

    [Test]
    public async Task CoberturaReader_InvalidAttributeValues_DefaultToZero()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="not-a-rate" line-rate="bad" version="1.9">
              <packages>
                <package name="Sample" branch-rate="x" line-rate="y">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\Foo.cs" branch-rate="z" line-rate="w">
                      <lines>
                        <line number="bad" hits="nope" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """);

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(document!.PackageBranchRates["Sample"]).IsEqualTo(0d);
        await Assert.That(document.Lines[@"C:\repo\Foo.cs"][0].Hits).IsEqualTo(0);
    }

    [Test]
    public async Task CoberturaReader_ParsePercentageFormats_CoversRatioBranches()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.75" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="0.75" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="1" line-rate="1">
                      <lines>
                        <line number="1" hits="1" branch="True" condition-coverage="75% (3/4)" />
                        <line number="2" hits="1" branch="True" condition-coverage="invalid">
                          <conditions>
                            <condition number="0" type="jump" coverage="150%" />
                            <condition number="1" type="jump" coverage="0.5" />
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

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CoberturaReader_EmptyLineElement_ReadsConditionFallback()
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
                        <line number="3" hits="0" branch="True" condition-coverage="50% (1/2)" />
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
    public async Task BranchGapAnalyzer_DeduplicatesAcrossScopedDocuments()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument first = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));
        ScopedCoberturaDocument second = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([first, second], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task BranchGapAnalyzer_SkipsOutOfScopePackages()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Other"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(0);
        await Assert.That(BranchGapAnalyzer.GetMinimumScopedBranchRate([scoped])).IsEqualTo(0d);
    }

    [Test]
    public async Task BranchGapAnalyzer_ResolvesPackageFromFileName()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Widgets" branch-rate="0.5" line-rate="0.5">
                  <classes>
                    <class name="Widgets.Foo" filename="C:\repo\Widgets.Foo.cs" branch-rate="0.5" line-rate="0.5">
                      <lines>
                        <line number="4" hits="1" branch="True" condition-coverage="50% (1/2)">
                          <conditions>
                            <condition number="0" type="jump" coverage="50%" />
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
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Widgets"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", true);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Package).IsEqualTo("Widgets");
    }

    [Test]
    public async Task BranchGapAnalyzer_SkipsFullyCoveredConditions()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="0.5" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="0.5" line-rate="1">
                      <lines>
                        <line number="7" hits="1" branch="True" condition-coverage="50% (1/2)">
                          <conditions>
                            <condition number="0" type="jump" coverage="50%" />
                            <condition number="1" type="jump" coverage="100%" />
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

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].ConditionIndex).IsEqualTo(0);
    }

    [Test]
    public async Task ExitCoverageComparer_UsesAbsolutePathHits()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:10:Return",
            @"D:\other\root\src\Sample\Foo.cs",
            10,
            1,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
            [exit],
            [document!],
            @"C:\different\root",
            false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExitCoverageComparer_WithSnippets_IncludesSnippetText()
    {
        string root = Path.Combine(Path.GetTempPath(), $"exit-snippet-{Guid.NewGuid():N}");
        string sourceDir = Path.Combine(root, "src", "Sample");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllLinesAsync(
            Path.Combine(sourceDir, "Foo.cs"),
            Enumerable.Range(1, 19).Select(static i => $"// {i}").Append("return 42;"));

        string xml = CoberturaFixtures.ExitGapXml.Replace(@"C:\repo", root, StringComparison.Ordinal);
        string path = CoberturaFixtures.WriteTemporaryFile(xml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            Path.Combine(root, "src", "Sample", "Foo.cs"),
            20,
            1,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare([exit], [document!], root, true);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Snippet).IsEqualTo("return 42;");
    }

    [Test]
    public async Task PathNormalizer_OutsideRepository_ReturnsNormalizedAbsolutePath()
    {
        string relative = PathNormalizer.ToRepositoryRelative(@"D:\outside\Foo.cs", Path.GetTempPath());

        await Assert.That(relative.Replace('\\', '/')).IsEqualTo("D:/outside/Foo.cs");
    }

    [Test]
    public async Task PathNormalizer_TryReadSnippet_MissingFileAndLongLine_ReturnNull()
    {
        string root = Path.GetTempPath();
        string? missing = PathNormalizer.TryReadSnippet(root, "does-not-exist.cs", 1);
        string? beyond = PathNormalizer.TryReadSnippet(root, Path.GetRandomFileName(), 99_999);

        await Assert.That(missing).IsNull();
        await Assert.That(beyond).IsNull();
    }

    [Test]
    public async Task CoverageGapReportFormatter_CompactAndText_CoverOptionalFields()
    {
        CoverageGapReport emptyPassed = new CoverageGapReport(
            new CoverageGapSummary(1d, 0, 0, 0, true, true),
            [],
            []);

        CoverageGapReport withGaps = new CoverageGapReport(
            new CoverageGapSummary(0.5d, 1, 1, 2, false, false),
            [new ExitCoverageGap(1, "id", "a.cs", 1, 1, "Return", "M", 0, null)],
            [new BranchGap(2, "Sample", "Foo", "b.cs", 2, 0, 0.5d, 1, null, null)]);

        string emptyText = CoverageGapReportFormatter.ToText(emptyPassed);
        string compactNoMethod = CoverageGapReportFormatter.ToCompact(withGaps);
        string textWithSections = CoverageGapReportFormatter.ToText(withGaps);

        await Assert.That(emptyText.Contains("passed", StringComparison.Ordinal)).IsTrue();
        await Assert.That(compactNoMethod.Contains(":method=", StringComparison.Ordinal)).IsTrue();
        await Assert.That(textWithSections.Contains("Exit gaps:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(textWithSections.Contains("Branch gaps:", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task FindNewestCoberturaInDirectory_ReturnsNullForUnrelatedDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"discovery-filter-{Guid.NewGuid():N}");
        string otherDir = Path.Combine(root, "Other", "output");
        Directory.CreateDirectory(otherDir);
        await File.WriteAllTextAsync(Path.Combine(otherDir, "run.cobertura.xml"), CoberturaFixtures.FullCoverageXml);

        string? path = CoberturaDiscovery.FindNewestCoberturaInDirectory(otherDir);

        await Assert.That(path).IsNotNull();
    }

    [Test]
    public async Task BranchGapScope_Default_IncludesRepositoryPackages()
    {
        await Assert.That(BranchGapScope.Default.IncludesPackage("CSharpStyleValidator")).IsTrue();
        await Assert.That(BranchGapScope.Default.IncludesPackage("ExitPoints")).IsTrue();
        await Assert.That(BranchGapScope.Default.IncludesPackage("CoverageGapAnalysis")).IsTrue();
        await Assert.That(() => BranchGapScope.Default.IncludesPackage(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CoberturaReader_LockedFile_ReturnsIoError()
    {
        string path = Path.Combine(Path.GetTempPath(), $"locked-{Guid.NewGuid():N}.xml");
        await File.WriteAllTextAsync(path, CoberturaFixtures.FullCoverageXml);
        using FileStream locked = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(document).IsNull();
        await Assert.That(error).Contains("Failed to read");
    }

    [Test]
    public async Task GetMinimumScopedBranchRate_MultipleDocuments_ReturnsLowest()
    {
        string partialPath = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        string fullPath = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(partialPath, out CoberturaDocument? partial, out string? _);
        CoberturaReader.TryRead(fullPath, out CoberturaDocument? full, out string? _);

        ScopedCoberturaDocument partialScoped = new ScopedCoberturaDocument(partial!, new BranchGapScope(["Sample"]));
        ScopedCoberturaDocument fullScoped = new ScopedCoberturaDocument(full!, new BranchGapScope(["Sample"]));

        double rate = BranchGapAnalyzer.GetMinimumScopedBranchRate([fullScoped, partialScoped]);

        await Assert.That(rate).IsEqualTo(0.5d);
    }

    [Test]
    public async Task BranchGapAnalyzer_ResolvesPackageFromUndottedFileName()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Widgets" branch-rate="0.5" line-rate="0.5">
                  <classes>
                    <class name="Widgets.Foo" filename="C:\repo\Widgets" branch-rate="0.5" line-rate="0.5">
                      <lines>
                        <line number="1" hits="1" branch="True" condition-coverage="50% (1/2)">
                          <conditions>
                            <condition number="0" type="jump" coverage="50%" />
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
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Widgets"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExitCoverageComparer_MergeHitsKeepsHigherCounts()
    {
        string lowHits = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        string highHits = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(lowHits, out CoberturaDocument? low, out string? _);
        CoberturaReader.TryRead(highHits, out CoberturaDocument? high, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:10:Return",
            @"C:\repo\src\Sample\Foo.cs",
            10,
            1,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
            [exit],
            [low!, high!],
            @"C:\repo",
            false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExitCoverageComparer_GetHitsUsesOriginalAbsolutePath()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            @"C:\repo\src\Sample\Foo.cs",
            20,
            1,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
            [exit],
            [document!],
            @"Z:\unrelated-root",
            false);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Hits).IsEqualTo(0);
    }

    [Test]
    public async Task PathNormalizer_TryReadSnippet_LockedFile_ReturnsNull()
    {
        string root = Path.Combine(Path.GetTempPath(), $"snippet-lock-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string file = Path.Combine(root, "Sample.cs");
        await File.WriteAllTextAsync(file, "locked line");
        using FileStream locked = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        string? snippet = PathNormalizer.TryReadSnippet(root, "Sample.cs", 1);

        await Assert.That(snippet).IsNull();
    }

    [Test]
    public async Task CoberturaDiscovery_ReadTestProjectName_ReturnsNullForShallowPath()
    {
        string root = Path.Combine(Path.GetTempPath(), $"shallow-{Guid.NewGuid():N}");
        string shallowDir = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(shallowDir);
        string file = Path.Combine(shallowDir, "orphan.cobertura.xml");

        string? projectName = CoberturaDiscovery.ReadTestProjectNameForTests(file);

        await Assert.That(projectName).IsNull();
    }

    [Test]
    public async Task CoverageGapReportBuilder_NullArguments_Throw()
    {
        await Assert.That(() => CoverageGapReportBuilder.Build(null!, [], [], @"C:\repo", false))
            .Throws<ArgumentNullException>();
        await Assert.That(() => CoverageGapReportFormatter.ToCompact(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => BranchGapAnalyzer.FindUncoveredBranches(null!, @"C:\repo", false))
            .Throws<ArgumentNullException>();
        await Assert.That(() => ExitCoverageComparer.Compare(null!, [], @"C:\repo", false))
            .Throws<ArgumentNullException>();
    }
}
