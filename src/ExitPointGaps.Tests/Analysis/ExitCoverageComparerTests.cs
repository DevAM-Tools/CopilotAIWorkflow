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

    [Test]
    public async Task Compare_RelativeCoberturaFilename_WithForeignCwd_MatchesExit()
    {
        string repoRoot = Path.Combine(Path.GetTempPath(), Invariant($"gap-repo-{Guid.NewGuid():N}"));
        string foreignCwd = Path.Combine(Path.GetTempPath(), Invariant($"gap-cwd-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(Path.Combine(repoRoot, "src", "Sample"));
        Directory.CreateDirectory(foreignCwd);

        string coberturaXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1" line-rate="1" version="1.9">
              <packages>
                <package name="Sample" branch-rate="1" line-rate="1">
                  <classes>
                    <class name="Sample.Foo" filename="src/Sample/Foo.cs" branch-rate="1" line-rate="1">
                      <lines>
                        <line number="20" hits="0" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
        string coberturaPath = CoberturaFixtures.WriteTemporaryFile(coberturaXml);
        CoberturaReader.TryRead(coberturaPath, out CoberturaDocument? document, out string? _);

        string previousCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = foreignCwd;

            ExitPointEntry exit = new ExitPointEntry(
                "Foo.Bar:20:Return",
                Path.Combine(repoRoot, "src", "Sample", "Foo.cs"),
                20,
                4,
                "Foo.Bar()",
                "Bar",
                ExitKind.Return);

            IReadOnlyList<ExitCoverageGap> gaps = ExitCoverageComparer.Compare(
                [exit],
                [document!],
                repoRoot,
                false);

            await Assert.That(gaps.Count).IsEqualTo(1);
            await Assert.That(gaps[0].FilePath).IsEqualTo("src/Sample/Foo.cs");
        }
        finally
        {
            Environment.CurrentDirectory = previousCwd;
        }
    }
}
