// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Run;

/// <summary>Tests for <see cref="TestProjectIndex"/>.</summary>
public sealed class TestProjectIndexTests
{
    [Test]
    public async Task Build_ReferencePairing_MatchesLegacyScan()
    {
        await using GapSolutionWorkspace sample = await GapSolutionWorkspace.CreateAsync();
        TestProjectIndex index = TestProjectIndex.Build(sample.RootPath);

        string firstProduction = Path.Combine(sample.RootPath, "GapSample", "GapSample.csproj");
        string secondProduction = Path.Combine(sample.RootPath, "GapMore", "GapMore.csproj");

        string? firstFromIndex = index.TryGetTestProject(firstProduction);
        string? secondFromIndex = index.TryGetTestProject(secondProduction);
        string? firstLegacy = TestProjectPairer.FindTestProject(firstProduction, sample.RootPath, null);
        string? secondLegacy = TestProjectPairer.FindTestProject(secondProduction, sample.RootPath, null);

        await Assert.That(firstFromIndex).IsEqualTo(firstLegacy);
        await Assert.That(secondFromIndex).IsEqualTo(secondLegacy);
    }
}
