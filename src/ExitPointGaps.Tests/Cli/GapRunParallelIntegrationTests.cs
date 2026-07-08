// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Cli;

/// <summary>Parallel vs serial gap-run integration tests.</summary>
/// <remarks>Serializes tests because each case spawns nested <c>dotnet test</c> subprocesses.</remarks>
[NotInParallel]
public sealed class GapRunParallelIntegrationTests
{
    [Test]
    public async Task RunSolution_SerialAndParallel_ProduceSameExitGapCounts()
    {
        await using GapSolutionWorkspace sample = await GapSolutionWorkspace.CreateAsync();
        string serialDirectory = Path.Combine(sample.WorkDirectory, "serial") + Path.DirectorySeparatorChar;
        string parallelDirectory = Path.Combine(sample.WorkDirectory, "parallel") + Path.DirectorySeparatorChar;

        int serialExit = await Program.Main(
        [
            "run", "solution", sample.SolutionPath,
            "--repo-root", sample.RootPath,
            "--work-dir", Path.Combine(sample.WorkDirectory, "serial-work"),
            "--keep-work-dir",
            "--no-build",
            "--max-parallelism", "1",
            "-o", serialDirectory,
        ]);

        int parallelExit = await Program.Main(
        [
            "run", "solution", sample.SolutionPath,
            "--repo-root", sample.RootPath,
            "--work-dir", Path.Combine(sample.WorkDirectory, "parallel-work"),
            "--keep-work-dir",
            "--no-build",
            "--max-parallelism", "4",
            "-o", parallelDirectory,
        ]);

        using JsonDocument serialSummary = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(serialDirectory, "summary.json")));
        using JsonDocument parallelSummary = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(parallelDirectory, "summary.json")));

        int serialExitGaps = serialSummary.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();
        int parallelExitGaps = parallelSummary.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();

        await Assert.That(serialExit).IsEqualTo(0);
        await Assert.That(parallelExit).IsEqualTo(0);
        await Assert.That(parallelExitGaps).IsEqualTo(serialExitGaps);
    }
}
