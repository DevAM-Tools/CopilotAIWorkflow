// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Cli;

/// <summary>CLI smoke tests for <see cref="Program"/>.</summary>
public sealed class ProgramTests
{
    private static string _RepositoryRoot { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public async Task Main_Plan_ExitPoints_WritesManifest()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        string workDir = RunIsolation.CreateDefaultWorkDirectory();
        string outputPath = Path.Combine(workDir, "exits.json");

        int exitCode = await Program.Main(
        [
            "plan",
            "project",
            csproj,
            "-o",
            outputPath,
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
            "--work-dir",
            workDir,
            "--keep-work-dir",
        ]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(outputPath)).IsTrue();
    }

    [Test]
    public async Task Main_Run_WithCachedCobertura_ReturnsZero()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        string? cobertura = Directory
            .EnumerateFiles(
                Path.Combine(_RepositoryRoot, "src", "ExitPoints.Tests"),
                "*.cobertura.xml",
                SearchOption.AllDirectories)
            .OrderByDescending(static path => File.GetLastWriteTimeUtc(path))
            .FirstOrDefault();

        await Assert.That(cobertura).IsNotNull();

        string workDir = RunIsolation.CreateDefaultWorkDirectory();
        int exitCode = await Program.Main(
        [
            "run",
            "project",
            csproj,
            "--cobertura",
            cobertura!,
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
            "--work-dir",
            workDir,
            "--keep-work-dir",
        ]);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Main_Run_InvalidFormat_ReturnsUsageError()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        int exitCode = await Program.Main(
        [
            "run",
            "project",
            csproj,
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
            "--format",
            "invalid",
        ]);

        await Assert.That(exitCode).IsEqualTo(CliConstants.ExitUsageError);
    }

    [Test]
    public async Task Main_Plan_InvalidFormat_ReturnsUsageError()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        int exitCode = await Program.Main(
        [
            "plan",
            "project",
            csproj,
            "--format",
            "compact",
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(CliConstants.ExitUsageError);
    }

    [Test]
    public async Task Main_Plan_DeletesWorkDirectory_WithoutKeepWorkDir()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        string workDir = RunIsolation.CreateDefaultWorkDirectory();
        string outputPath = Path.Combine(workDir, "exits.json");

        int exitCode = await Program.Main(
        [
            "plan",
            "project",
            csproj,
            "-o",
            outputPath,
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
            "--work-dir",
            workDir,
        ]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(Directory.Exists(workDir)).IsFalse();
    }
}
