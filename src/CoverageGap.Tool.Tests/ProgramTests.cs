// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool.Tests;

/// <summary>CLI smoke tests for <see cref="Program"/>.</summary>
public sealed class ProgramTests
{
    private static string _RepositoryRoot { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public async Task Main_InvalidArgs_ReturnsOne()
    {
        int exitCode = await Program.Main(["report", "missing.csproj"]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_NoArgs_ReturnsOne()
    {
        int exitCode = await Program.Main([]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_UnknownCommand_ReturnsOne()
    {
        int exitCode = await Program.Main(["unknown-command"]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_MissingProject_ReturnsOne()
    {
        string missing = Path.Combine(_RepositoryRoot, "missing-project.csproj");
        int exitCode = await Program.Main(["report", "project", missing]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_ReportCompilationFailure_ReturnsOne()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"coveragegap-broken-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        string projectPath = Path.Combine(tempRoot, "Broken.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        string coberturaPath = Path.Combine(tempRoot, "coverage.cobertura.xml");
        await File.WriteAllTextAsync(
            coberturaPath,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1" line-rate="1" version="1.9">
              <packages>
                <package name="Broken" branch-rate="1" line-rate="1">
                  <classes />
                </package>
              </packages>
            </coverage>
            """);

        int exitCode = await Program.Main(
        [
            "report",
            "project",
            projectPath,
            "--cobertura",
            coberturaPath,
            "--repo-root",
            tempRoot,
            "--scope",
            "Broken",
            "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_Manifest_MatchesFilteredExitCount()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        string outputPath = Path.Combine(Path.GetTempPath(), $"manifest-{Guid.NewGuid():N}.json");

        int exitCode = await Program.Main(
        [
            "manifest",
            "project",
            csproj,
            "-o",
            outputPath,
            "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(0);

        Compilation? compilation = await ProjectCompilationLoader.CreateAsync(csproj, skipBuild: true).ConfigureAwait(false);
        await Assert.That(compilation).IsNotNull();

        IReadOnlyList<ExitPointEntry> filtered = ExitPointFilter.RemoveExcluded(
            ExitPointCollector.Collect(compilation!),
            compilation!);
        List<ExitPointEntry> manifestExits = JsonSerializer.Deserialize<List<ExitPointEntry>>(await File.ReadAllTextAsync(outputPath))!;

        await Assert.That(manifestExits.Count).IsEqualTo(filtered.Count);
    }

    [Test]
    public async Task Main_InvalidReportFormat_ReturnsOne()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        int exitCode = await Program.Main(
        [
            "report",
            "project",
            csproj,
            "--search-root",
            "src",
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
            "--format",
            "invalid",
        ]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_Report_ScopeMismatch_ExitGateStillPasses()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "CoverageGapAnalysis", "CoverageGapAnalysis.csproj");
        string outputPath = Path.Combine(Path.GetTempPath(), $"scope-{Guid.NewGuid():N}.json");
        int exitCode = await Program.Main(
        [
            "report",
            "project",
            csproj,
            "--search-root",
            Path.Combine(_RepositoryRoot, "src"),
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
            "--scope",
            "NonExistentPackage",
            "--format",
            "agent",
            "-o",
            outputPath,
        ]);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
        bool branchGatePassed = document.RootElement.GetProperty("summary").GetProperty("branchGatePassed").GetBoolean();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(branchGatePassed).IsFalse();
    }

    [Test]
    public async Task Main_Report_MissingCobertura_ReturnsOne()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"coveragegap-nocob-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        string projectPath = Path.Combine(tempRoot, "Empty.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        int exitCode = await Program.Main(
        [
            "report",
            "project",
            projectPath,
            "--search-root",
            tempRoot,
            "--repo-root",
            tempRoot,
            "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_Report_Success_ReturnsZero()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "CoverageGapAnalysis", "CoverageGapAnalysis.csproj");
        int exitCode = await Program.Main(
        [
            "report",
            "project",
            csproj,
            "--search-root",
            Path.Combine(_RepositoryRoot, "src"),
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Main_Report_TextAndCompactFormats_ReturnZero()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "CoverageGapAnalysis", "CoverageGapAnalysis.csproj");
        string searchRoot = Path.Combine(_RepositoryRoot, "src");

        int textExit = await Program.Main(
        [
            "report", "project", csproj, "--search-root", searchRoot, "--repo-root", _RepositoryRoot,
            "--no-build", "--format", "text",
        ]);
        int compactExit = await Program.Main(
        [
            "report", "project", csproj, "--search-root", searchRoot, "--repo-root", _RepositoryRoot,
            "--no-build", "--format", "compact",
        ]);

        await Assert.That(textExit).IsEqualTo(0);
        await Assert.That(compactExit).IsEqualTo(0);
    }

    [Test]
    public async Task Main_Manifest_TextFormat_ReturnsZero()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        int exitCode = await Program.Main(
        [
            "manifest", "project", csproj, "--format", "text", "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Main_Manifest_InvalidFormat_ReturnsOne()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "ExitPoints", "ExitPoints.csproj");
        int exitCode = await Program.Main(
        [
            "manifest", "project", csproj, "--format", "compact", "--no-build",
        ]);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Main_Report_ExitGateCountZero_ReturnsZero()
    {
        string csproj = Path.Combine(_RepositoryRoot, "src", "CoverageGapAnalysis", "CoverageGapAnalysis.csproj");
        string outputPath = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.json");
        int exitCode = await Program.Main(
        [
            "report",
            "project",
            csproj,
            "--search-root",
            Path.Combine(_RepositoryRoot, "src"),
            "--repo-root",
            _RepositoryRoot,
            "--no-build",
            "--format",
            "agent",
            "-o",
            outputPath,
        ]);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
        int exitGapCount = document.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(exitGapCount).IsEqualTo(0);
    }
}
