// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

/// <summary>Tests for <see cref="ExitCoverageComparer"/>.</summary>
public sealed class ExitCoverageComparerTests
{
    [Test]
    public async Task Compare_LineHitZero_EmitsGap()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.ExitGapXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            @"C:\repo\src\Sample\Foo.cs",
            20,
            4,
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
    public async Task Compare_LineHitPositive_NoGap()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

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
            [document!],
            @"C:\repo",
            false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Compare_LineNotInstrumented_NoGap()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.FullCoverageXml);
        CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? _);

        ExitPointEntry exit = new ExitPointEntry(
            "Foo.Bar:20:Return",
            @"C:\repo\src\Sample\Foo.cs",
            20,
            4,
            "Foo.Bar()",
            "Bar",
            ExitKind.Return);

        IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
            [exit],
            [document!],
            @"C:\repo",
            false);

        await Assert.That(gaps.Count).IsEqualTo(0);
    }
}
