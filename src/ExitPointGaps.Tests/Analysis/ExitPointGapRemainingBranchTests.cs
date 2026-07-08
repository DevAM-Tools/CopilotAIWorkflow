// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

/// <summary>Targets remaining branch gaps reported by <c>exitpointgaps</c>.</summary>
public sealed class ExitPointGapRemainingBranchTests
{
    [Test]
    public async Task GetMinimumScopedBranchRate_EmptyDocuments_ReturnsOne()
    {
        double rate = BranchGapAnalyzer.GetMinimumScopedBranchRate([]);

        await Assert.That(rate).IsEqualTo(1d);
    }

    [Test]
    public async Task GetMinimumScopedBranchRate_NoMatchingPackages_ReturnsZero()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Other"]));

        double rate = BranchGapAnalyzer.GetMinimumScopedBranchRate([scoped]);

        await Assert.That(rate).IsEqualTo(0d);
    }

    [Test]
    public async Task BranchGapAnalyzer_ResolvePackageName_RejectsSubstringMatch()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Sample" branch-rate="0.5" line-rate="0.5">
                  <classes>
                    <class name="NotSample.Foo" filename="C:\repo\src\NotSample\Foo.cs" branch-rate="0.5" line-rate="0.5">
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
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task BranchGapAnalyzer_ResolvesLongestPackageSegmentMatch()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Widgets" branch-rate="1" line-rate="1">
                  <classes />
                </package>
                <package name="WidgetsExtra" branch-rate="0.5" line-rate="0.5">
                  <classes>
                    <class name="WidgetsExtra.Foo" filename="C:\repo\Widgets\WidgetsExtra\Foo.cs" branch-rate="0.5" line-rate="0.5">
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
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["WidgetsExtra"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Package).IsEqualTo("WidgetsExtra");
    }

    [Test]
    public async Task BranchGapAnalyzer_PathSegmentMatch_StartsWithPackageName()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Widgets" branch-rate="0.5" line-rate="0.5">
                  <classes>
                    <class name="Widgets.Foo" filename="Widgets/Foo.cs" branch-rate="0.5" line-rate="0.5">
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
        await Assert.That(gaps[0].Package).IsEqualTo("Widgets");
    }

    [Test]
    public async Task GetMinimumScopedBranchRate_ClassRateAbovePackageMinimum_SkipsClassUpdate()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="0.5" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="0.9" line-rate="1">
                      <lines>
                        <line number="1" hits="1" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """);

        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        double rate = BranchGapAnalyzer.GetMinimumScopedBranchRate([scoped]);

        await Assert.That(rate).IsEqualTo(0.5d);
    }

    [Test]
    public async Task BranchGapAnalyzer_ResolvesPackageFromDottedFileName()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Other" branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Other.Bar" filename="C:\data\MyWidget.cs" branch-rate="0.5" line-rate="0.5">
                      <lines>
                        <line number="2" hits="1" branch="True" condition-coverage="50% (1/2)">
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
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["MyWidget"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\data", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CoberturaReader_SkipsEmptyPackageAndClassNames()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1" line-rate="1" version="1.9">
              <packages>
                <package name="" branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Ignored" filename="" branch-rate="1" line-rate="1">
                      <lines>
                        <line number="1" hits="1" branch="False" />
                      </lines>
                    </class>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="1" line-rate="1">
                      <lines>
                        <line number="2" hits="1" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
                <package name="Sample" branch-rate="1" line-rate="1">
                  <classes />
                </package>
              </packages>
            </coverage>
            """);

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(document!.PackageBranchRates.ContainsKey("Sample")).IsTrue();
        await Assert.That(document.Lines.ContainsKey(@"C:\repo\src\Sample\Foo.cs")).IsTrue();
    }

    [Test]
    public async Task CoberturaReader_ParsePercentageWithoutPercentSign_ParsesRatio()
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
                        <line number="4" hits="1" branch="True" condition-coverage="0.25" />
                        <line number="5" hits="1" branch="True" condition-coverage="not-parsable">
                          <conditions>
                            <condition number="0" type="jump" coverage="n/a" />
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
    public async Task CoberturaReader_ReadConditionsFallbackWithoutChildConditions()
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
                        <line number="8" hits="1" branch="True" condition-coverage="40% (2/5)"></line>
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
    public async Task ExitCoverageComparer_SkipsLowerHitCountsWhenMerging()
    {
        string higherFirst = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="1" line-rate="1">
                      <lines>
                        <line number="10" hits="5" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """);

        string lowerSecond = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="1" line-rate="1">
                      <lines>
                        <line number="10" hits="1" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """);

        CoberturaReader.TryRead(higherFirst, out CoberturaDocument? high, out string? _);
        CoberturaReader.TryRead(lowerSecond, out CoberturaDocument? low, out string? _);

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
            [high!, low!],
            @"C:\repo",
            false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExitCoverageComparer_UsesOriginalPathWhenRelativePathDiffers()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        string repositoryRoot = Path.Combine(Path.GetTempPath(), $"relative-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(repositoryRoot);

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
            repositoryRoot,
            false);

        await Assert.That(gaps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task PathNormalizer_TryReadSnippet_LineBeyondEnd_ReturnsNull()
    {
        string root = Path.Combine(Path.GetTempPath(), $"snippet-beyond-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Sample.cs"), "only");

        string? snippet = PathNormalizer.TryReadSnippet(root, "Sample.cs", 2);

        await Assert.That(snippet).IsNull();
    }

    [Test]
    public async Task ExitCoverageComparer_FallsBackToOriginalPathKey()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            "src/Sample/Foo.cs",
            20,
            1,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
            [exit],
            [document!],
            @"C:\repo",
            false);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Hits).IsEqualTo(0);
    }

    [Test]
    public async Task CoberturaReader_SkipsPackageAndClassWithoutNameAttributes()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1" line-rate="1" version="1.9">
              <packages>
                <package branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" branch-rate="1" line-rate="1">
                      <lines>
                        <line number="1" hits="1" branch="False" />
                      </lines>
                    </class>
                    <class name="Sample.Bar" filename="C:\repo\src\Sample\Bar.cs" branch-rate="1" line-rate="1">
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
        await Assert.That(document!.Lines.ContainsKey(@"C:\repo\src\Sample\Bar.cs")).IsTrue();
        await Assert.That(document.Lines.Count).IsEqualTo(1);
    }

    [Test]
    public async Task BranchGapAnalyzer_ResolvesExtensionlessFileName()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Other" branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Other.Widget" filename="C:\repo\Widget" branch-rate="0.5" line-rate="0.5">
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
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Widget"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CoberturaDiscovery_ShallowTestResults_FindsCoberturaInDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"drive-root-{Guid.NewGuid():N}");
        string testResults = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(testResults);
        string file = Path.Combine(testResults, "drive.cobertura.xml");
        await File.WriteAllTextAsync(file, CoberturaFixtures.FullCoverageXml);

        string? path = CoberturaDiscovery.FindNewestCoberturaInDirectory(testResults);

        await Assert.That(path).IsEqualTo(file);
    }

    [Test]
    public async Task ExitCoverageComparer_GetHitsFallsBackToNormalizedOriginalPath()
    {
        string repositoryRoot = @"C:\coverage-fallback-repo";
        string xml = CoberturaFixtures.ExitGapXml.Replace(
            @"C:\repo",
            repositoryRoot,
            StringComparison.Ordinal);
        string path = CoberturaFixtures.WriteTemporaryFile(xml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            @"C:\coverage-fallback-repo\src\Sample\Foo.cs",
            20,
            1,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
            [exit],
            [document!],
            repositoryRoot,
            false);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Hits).IsEqualTo(0);
    }
}
