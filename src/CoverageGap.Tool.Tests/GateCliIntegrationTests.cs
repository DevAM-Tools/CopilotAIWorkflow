// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool.Tests;

/// <summary>End-to-end gate CLI tests on temporary demo solutions.</summary>
/// <remarks>Serializes tests because each case spawns nested <c>dotnet test</c> subprocesses.</remarks>
[NotInParallel]
public sealed class GateCliIntegrationTests
{
    [Test]
    public async Task RunProject_FullCoverage_PassesGate()
    {
        await using GapSampleWorkspace sample = await GapSampleWorkspace.CreateAsync(includeSecondReturnTest: true);
        string outputPath = Path.Combine(sample.WorkDirectory, "summary.json");

        int exitCode = await Program.Main(
        [
            "run",
            "project",
            sample.LibraryProjectPath,
            "--repo-root",
            sample.RootPath,
            "--work-dir",
            sample.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
            "-o",
            outputPath,
        ]);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
        int exitGapCount = document.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(exitGapCount).IsEqualTo(0);
    }

    [Test]
    public async Task RunProject_PartialCoverage_FailsGate()
    {
        await using GapSampleWorkspace sample = await GapSampleWorkspace.CreateAsync(includeSecondReturnTest: false);
        string outputPath = Path.Combine(sample.WorkDirectory, "summary.json");

        int exitCode = await Program.Main(
        [
            "run",
            "project",
            sample.LibraryProjectPath,
            "--repo-root",
            sample.RootPath,
            "--work-dir",
            sample.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
            "-o",
            outputPath,
        ]);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
        int exitGapCount = document.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(exitGapCount).IsGreaterThan(0);
    }

    [Test]
    public async Task RunProject_ZeroTests_Fails()
    {
        await using GapSampleWorkspace sample = await GapSampleWorkspace.CreateAsync(
            includeSecondReturnTest: false,
            includeTestClass: false);

        int exitCode = await Program.Main(
        [
            "run",
            "project",
            sample.LibraryProjectPath,
            "--repo-root",
            sample.RootPath,
            "--work-dir",
            sample.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task PlanProject_ZeroTests_Succeeds()
    {
        await using GapSampleWorkspace sample = await GapSampleWorkspace.CreateAsync(
            includeSecondReturnTest: false,
            includeTestClass: false);
        string outputPath = Path.Combine(sample.WorkDirectory, "exits.json");

        int exitCode = await Program.Main(
        [
            "plan",
            "project",
            sample.LibraryProjectPath,
            "--repo-root",
            sample.RootPath,
            "--work-dir",
            sample.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
            "-o",
            outputPath,
        ]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That((await File.ReadAllTextAsync(outputPath)).Length).IsGreaterThan(2);
    }

    [Test]
    public async Task PlanProject_UnpairedLibrary_Succeeds()
    {
        await using GapSampleWorkspace sample = await GapSampleWorkspace.CreateUnpairedLibraryAsync();
        string outputPath = Path.Combine(sample.WorkDirectory, "exits.json");

        int exitCode = await Program.Main(
        [
            "plan",
            "project",
            sample.LibraryProjectPath,
            "--repo-root",
            sample.RootPath,
            "--work-dir",
            sample.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
            "-o",
            outputPath,
        ]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That((await File.ReadAllTextAsync(outputPath)).Length).IsGreaterThan(2);
    }

    [Test]
    public async Task RunSolution_TwoProjects_PassesGate()
    {
        await using GapSolutionWorkspace sample = await GapSolutionWorkspace.CreateAsync();
        string outputPath = Path.Combine(sample.WorkDirectory, "summary.json");

        int exitCode = await Program.Main(
        [
            "run",
            "solution",
            sample.SolutionPath,
            "--repo-root",
            sample.RootPath,
            "--work-dir",
            sample.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
            "-o",
            outputPath,
        ]);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
        int projectCount = document.RootElement.GetProperty("summary").GetProperty("projectCount").GetInt32();
        int exitGapCount = document.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(projectCount).IsEqualTo(2);
        await Assert.That(exitGapCount).IsEqualTo(0);
    }

    [Test]
    public async Task RunSolution_SkipNoTests_GatesOnlyPairedProjects()
    {
        await using GapSolutionWorkspace sample = await GapSolutionWorkspace.CreateAsync(includeUnpairedLibrary: true);
        string outputPath = Path.Combine(sample.WorkDirectory, "summary.json");

        int exitCode = await Program.Main(
        [
            "run",
            "solution",
            sample.SolutionPath,
            "--repo-root",
            sample.RootPath,
            "--work-dir",
            sample.WorkDirectory,
            "--keep-work-dir",
            "--skip-no-tests",
            "--no-build",
            "-o",
            outputPath,
        ]);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
        int projectCount = document.RootElement.GetProperty("summary").GetProperty("projectCount").GetInt32();
        int exitGapCount = document.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(projectCount).IsEqualTo(2);
        await Assert.That(exitGapCount).IsEqualTo(0);
    }

    [Test]
    public async Task ParallelRuns_UseDistinctWorkDirectories()
    {
        await using GapSampleWorkspace first = await GapSampleWorkspace.CreateAsync(includeSecondReturnTest: true);
        await using GapSampleWorkspace second = await GapSampleWorkspace.CreateAsync(includeSecondReturnTest: true);
        string firstOutput = Path.Combine(first.WorkDirectory, "summary.json");
        string secondOutput = Path.Combine(second.WorkDirectory, "summary.json");

        Task<int> firstRun = Program.Main(
        [
            "run", "project", first.LibraryProjectPath,
            "--repo-root", first.RootPath,
            "--work-dir", first.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
            "-o", firstOutput,
        ]);
        Task<int> secondRun = Program.Main(
        [
            "run", "project", second.LibraryProjectPath,
            "--repo-root", second.RootPath,
            "--work-dir", second.WorkDirectory,
            "--keep-work-dir",
            "--no-build",
            "-o", secondOutput,
        ]);

        int firstExit = await firstRun;
        int secondExit = await secondRun;

        await Assert.That(firstExit).IsEqualTo(0);
        await Assert.That(secondExit).IsEqualTo(0);
        await Assert.That(first.WorkDirectory).IsNotEqualTo(second.WorkDirectory);
        await Assert.That(File.Exists(firstOutput)).IsTrue();
        await Assert.That(File.Exists(secondOutput)).IsTrue();
    }
}
