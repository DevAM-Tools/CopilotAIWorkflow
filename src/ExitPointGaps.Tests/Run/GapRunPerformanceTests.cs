// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Run;

/// <summary>Performance comparison between serial and parallel orchestration.</summary>
/// <remarks>Serializes tests because each case spawns nested <c>dotnet test</c> subprocesses.</remarks>
[NotInParallel]
public sealed class GapRunPerformanceTests
{
    [Test]
    public async Task RunSolution_Parallel_IsFasterThanSerial()
    {
        await using GapMultiProjectWorkspace sample = await GapMultiProjectWorkspace.CreateAsync(projectCount: 6);
        string serialDirectory = Path.Combine(sample.WorkDirectory, "serial") + Path.DirectorySeparatorChar;
        string parallelDirectory = Path.Combine(sample.WorkDirectory, "parallel") + Path.DirectorySeparatorChar;

        Stopwatch serialWatch = Stopwatch.StartNew();
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
        serialWatch.Stop();

        Stopwatch parallelWatch = Stopwatch.StartNew();
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
        parallelWatch.Stop();

        await Assert.That(serialExit).IsEqualTo(0);
        await Assert.That(parallelExit).IsEqualTo(0);
        await Assert.That(parallelWatch.Elapsed).IsLessThan(serialWatch.Elapsed);
    }
}
