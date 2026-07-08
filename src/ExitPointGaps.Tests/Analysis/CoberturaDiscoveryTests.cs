// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

/// <summary>Tests for <see cref="CoberturaDiscovery"/>.</summary>
public sealed class CoberturaDiscoveryTests
{
    [Test]
    public async Task FindNewestCoberturaInDirectory_PicksNewestFile()
    {
        string root = Path.Combine(Path.GetTempPath(), $"discovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        string older = Path.Combine(root, "older.cobertura.xml");
        string newer = Path.Combine(root, "newer.cobertura.xml");
        await File.WriteAllTextAsync(older, "<coverage branch-rate=\"1\" line-rate=\"1\" version=\"1.9\"><packages></packages></coverage>");
        await File.WriteAllTextAsync(newer, "<coverage branch-rate=\"1\" line-rate=\"1\" version=\"1.9\"><packages></packages></coverage>");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-5));

        string? path = CoberturaDiscovery.FindNewestCoberturaInDirectory(root);

        await Assert.That(path).IsEqualTo(newer);
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
        string projectDir = Path.Combine(root, "ExitPointGaps.Tests");
        Directory.CreateDirectory(projectDir);
        DirectoryInfo directory = new DirectoryInfo(projectDir);

        string? projectName = CoberturaDiscovery.ReadTestProjectDirectoryForTests(directory);

        await Assert.That(projectName).IsEqualTo("ExitPointGaps.Tests");
    }

    [Test]
    public async Task ReadTestProjectName_ReturnsTestsFolderFromCoberturaPath()
    {
        string root = Path.Combine(Path.GetTempPath(), $"tests-name-{Guid.NewGuid():N}");
        string coberturaPath = Path.Combine(root, "ExitPointGaps.Tests", "TestResults", "x.cobertura.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(coberturaPath)!);
        await File.WriteAllTextAsync(coberturaPath, "<coverage/>");

        string? projectName = CoberturaDiscovery.ReadTestProjectNameForTests(coberturaPath);

        await Assert.That(projectName).IsEqualTo("ExitPointGaps.Tests");
    }

    [Test]
    public async Task FindNewestCoberturaInDirectory_ReturnsNullForMissingDirectory()
    {
        string? path = CoberturaDiscovery.FindNewestCoberturaInDirectory(
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"));

        await Assert.That(path).IsNull();
    }
}
