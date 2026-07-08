// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

/// <summary>Tests for <see cref="BranchGapAnalyzer"/>.</summary>
public sealed class BranchGapAnalyzerTests
{
    [Test]
    public async Task FindUncoveredBranches_PartialCondition_EmitsGap()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Line).IsEqualTo(10);
        await Assert.That(gaps[0].ConditionIndex).IsEqualTo(0);
    }

    [Test]
    public async Task FindUncoveredBranches_FullCoverage_EmitsEmpty()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetMinimumScopedBranchRate_PartialPackage_ReturnsMinimum()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["Sample"]));

        double rate = BranchGapAnalyzer.GetMinimumScopedBranchRate([scoped]);

        await Assert.That(rate).IsEqualTo(0.5d);
    }

    [Test]
    public async Task FindUncoveredBranches_PrefixPackageNames_ResolvesLongestMatch()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
              <packages>
                <package name="Foo" branch-rate="1" line-rate="1">
                  <classes />
                </package>
                <package name="FooBar" branch-rate="0.5" line-rate="0.5">
                  <classes>
                    <class name="FooBar.Widget" filename="C:\repo\src\FooBar\Widget.cs" branch-rate="0.5" line-rate="0.5">
                      <lines>
                        <line number="3" hits="1" branch="True" condition-coverage="50% (1/2)">
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
        ScopedCoberturaDocument scoped = new ScopedCoberturaDocument(document!, new BranchGapScope(["FooBar"]));

        IReadOnlyList<BranchGap> gaps = BranchGapAnalyzer.FindUncoveredBranches([scoped], @"C:\repo", false);

        await Assert.That(gaps.Count).IsEqualTo(1);
        await Assert.That(gaps[0].Package).IsEqualTo("FooBar");
    }
}
