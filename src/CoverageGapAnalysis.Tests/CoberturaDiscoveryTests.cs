// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests;

/// <summary>Tests for <see cref="CoberturaDiscovery"/>.</summary>
public sealed class CoberturaDiscoveryTests
{
    [Test]
    public async Task FindLatestFiles_PicksNewestCoberturaPerProject()
    {
        string root = Path.Combine(Path.GetTempPath(), $"discovery-{Guid.NewGuid():N}");
        string testProjectRoot = Path.Combine(root, "CoverageGapAnalysis.Tests", "bin", "Release", "net10.0", "TestResults");
        Directory.CreateDirectory(testProjectRoot);

        string older = Path.Combine(testProjectRoot, "older.cobertura.xml");
        string newer = Path.Combine(testProjectRoot, "newer.cobertura.xml");
        await File.WriteAllTextAsync(older, "<coverage branch-rate=\"1\" line-rate=\"1\" version=\"1.9\"><packages></packages></coverage>");
        await Task.Delay(20);
        await File.WriteAllTextAsync(newer, "<coverage branch-rate=\"1\" line-rate=\"1\" version=\"1.9\"><packages></packages></coverage>");

        IReadOnlyList<string> files = CoberturaDiscovery.FindLatestFiles(
            [root],
            CoberturaDiscovery.DefaultTestProjectPackages);

        await Assert.That(files.Count).IsEqualTo(1);
        await Assert.That(files[0]).IsEqualTo(newer);
    }

    [Test]
    public async Task ReadTestProjectDirectory_ReturnsNullWhenNotUnderTestsProject()
    {
        string root = Path.Combine(Path.GetTempPath(), $"shallow-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        DirectoryInfo directory = new DirectoryInfo(root);

        string? projectName = CoberturaDiscovery.ReadTestProjectDirectoryForTests(directory);

        await Assert.That(projectName).IsNull();
    }

    [Test]
    public async Task ReadTestProjectDirectory_ReturnsTestsFolderName()
    {
        string root = Path.Combine(Path.GetTempPath(), $"tests-dir-{Guid.NewGuid():N}");
        string projectDir = Path.Combine(root, "CoverageGapAnalysis.Tests");
        Directory.CreateDirectory(projectDir);
        DirectoryInfo directory = new DirectoryInfo(projectDir);

        string? projectName = CoberturaDiscovery.ReadTestProjectDirectoryForTests(directory);

        await Assert.That(projectName).IsEqualTo("CoverageGapAnalysis.Tests");
    }

    [Test]
    public async Task ReadTestProjectName_ReturnsNullWhenDirectoryUnavailable()
    {
        string? projectName = CoberturaDiscovery.ReadTestProjectNameForTests(@"C:\");

        await Assert.That(projectName).IsNull();
    }

    [Test]
    public async Task FindLatestForTargetPackage_ReturnsOnlyMatchingTestProject()
    {
        string root = Path.Combine(Path.GetTempPath(), $"target-filter-{Guid.NewGuid():N}");
        string validatorResults = Path.Combine(root, "CSharpStyleValidator.Tests", "bin", "Release", "net10.0", "TestResults");
        string toolResults = Path.Combine(root, "CoverageGap.Tool.Tests", "bin", "Release", "net10.0", "TestResults");
        Directory.CreateDirectory(validatorResults);
        Directory.CreateDirectory(toolResults);

        string validatorCobertura = Path.Combine(validatorResults, "validator.cobertura.xml");
        string toolCobertura = Path.Combine(toolResults, "tool.cobertura.xml");
        await File.WriteAllTextAsync(validatorCobertura, "<coverage branch-rate=\"1\" line-rate=\"1\" version=\"1.9\"><packages></packages></coverage>");
        await File.WriteAllTextAsync(toolCobertura, "<coverage branch-rate=\"1\" line-rate=\"1\" version=\"1.9\"><packages></packages></coverage>");

        IReadOnlyDictionary<string, string> filtered = CoberturaDiscovery.FindLatestForTargetPackage(
            [root],
            CoberturaDiscovery.DefaultTestProjectPackages,
            "CSharpStyleValidator");

        await Assert.That(filtered.Count).IsEqualTo(1);
        await Assert.That(filtered.ContainsKey("CSharpStyleValidator.Tests")).IsTrue();
        await Assert.That(filtered["CSharpStyleValidator.Tests"]).IsEqualTo(validatorCobertura);
    }

    [Test]
    public async Task FindLatest_SkipsMissingParentDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing-dir-{Guid.NewGuid():N}", "run.cobertura.xml");

        IReadOnlyList<string> files = CoberturaDiscovery.FindLatestFiles(
            [Path.GetDirectoryName(path)!],
            CoberturaDiscovery.DefaultTestProjectPackages);

        await Assert.That(files.Count).IsEqualTo(0);
    }
}
