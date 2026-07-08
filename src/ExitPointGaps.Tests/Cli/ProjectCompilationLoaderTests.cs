// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Cli;

/// <summary>Tests for <see cref="ProjectCompilationLoader"/>.</summary>
public sealed class ProjectCompilationLoaderTests
{
    [Test]
    public async Task TryCreate_MissingAssets_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"pcl-missing-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string projectPath = Path.Combine(root, "Empty.csproj");
        await File.WriteAllTextAsync(projectPath, _MinimalProjectXml);

        (Compilation? compilation, string? error) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild: true,
            "Release");

        await Assert.That(compilation).IsNull();
        await Assert.That(error).Contains("restore");
    }

    [Test]
    public async Task TryCreate_NoSourceFiles_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"pcl-no-src-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string projectPath = Path.Combine(root, "Empty.csproj");
        await File.WriteAllTextAsync(projectPath, _MinimalProjectXml);
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "obj", "project.assets.json"),
            """
            {
              "version": 3,
              "targets": { "net10.0": {} },
              "project": { "frameworks": { "net10.0": {} } }
            }
            """);

        (Compilation? compilation, string? error) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild: true,
            "Release");

        await Assert.That(compilation).IsNull();
        await Assert.That(error).Contains("source");
    }

    [Test]
    public async Task TryCreate_ValidProject_ReturnsCompilation()
    {
        string root = Path.Combine(Path.GetTempPath(), $"pcl-valid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string projectPath = Path.Combine(root, "Sample.csproj");
        await File.WriteAllTextAsync(projectPath, _MinimalProjectXml);
        await File.WriteAllTextAsync(
            Path.Combine(root, "Sample.cs"),
            """
            namespace Sample;
            public sealed class Type
            {
                public int M() => 1;
            }
            """);
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "obj", "project.assets.json"),
            """
            {
              "version": 3,
              "targets": { "net10.0": {} },
              "project": { "frameworks": { "net10.0": {} } }
            }
            """);

        (Compilation? compilation, string? _) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild: true,
            "Release");

        await Assert.That(compilation).IsNotNull();
        await Assert.That(compilation!.SyntaxTrees.Count()).IsGreaterThan(0);
    }

    [Test]
    public async Task ResolveConfiguration_UsesExplicitValue()
    {
        string configuration = ProjectCompilationLoader.ResolveConfiguration("Debug");

        await Assert.That(configuration).IsEqualTo("Debug");
    }

    [Test]
    public async Task TryCreate_MissingTargetFramework_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"pcl-no-tfm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string projectPath = Path.Combine(root, "Sample.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(
            Path.Combine(root, "Sample.cs"),
            "namespace Sample; public sealed class Type { }");
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "obj", "project.assets.json"),
            """
            {
              "version": 3,
              "targets": { "net10.0": {} },
              "project": { "frameworks": {} }
            }
            """);

        (Compilation? compilation, string? error) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild: true,
            "Release");

        await Assert.That(compilation).IsNull();
        await Assert.That(error).Contains("framework");
    }

    [Test]
    [NotInParallel]
    public async Task TryCreate_WithBuild_SucceedsForTempProject()
    {
        string root = Path.Combine(Path.GetTempPath(), $"pcl-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "global.json"), await File.ReadAllTextAsync(Path.Combine(_RepositoryRoot, "global.json")));
        string projectPath = Path.Combine(root, "Sample.csproj");
        await File.WriteAllTextAsync(projectPath, _MinimalProjectXml);
        await File.WriteAllTextAsync(
            Path.Combine(root, "Sample.cs"),
            """
            namespace Sample;
            public sealed class Type
            {
                public int M() => 1;
            }
            """);

        (Compilation? compilation, string? _) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild: false,
            "Release");

        await Assert.That(compilation).IsNotNull();
    }

    [Test]
    public async Task ResolveConfiguration_UsesEnvironmentVariable()
    {
        string? previous = Environment.GetEnvironmentVariable("Configuration");
        try
        {
            Environment.SetEnvironmentVariable("Configuration", "Debug");
            string configuration = ProjectCompilationLoader.ResolveConfiguration(null);
            await Assert.That(configuration).IsEqualTo("Debug");
        }
        finally
        {
            Environment.SetEnvironmentVariable("Configuration", previous);
        }
    }

    [Test]
    public async Task TryCreate_InvalidProjectXml_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"pcl-bad-xml-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string projectPath = Path.Combine(root, "Sample.csproj");
        await File.WriteAllTextAsync(projectPath, "<Project><NotClosed>");
        Directory.CreateDirectory(Path.Combine(root, "obj"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "obj", "project.assets.json"),
            """
            {
              "version": 3,
              "targets": { "net10.0": {} },
              "project": { "frameworks": { "net10.0": {} } }
            }
            """);

        (Compilation? compilation, string? error) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild: true,
            "Release");

        await Assert.That(compilation).IsNull();
        await Assert.That(error).Contains("XML");
    }

    [Test]
    [NotInParallel]
    public async Task TryCreate_BuildFailure_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"pcl-build-fail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "global.json"), await File.ReadAllTextAsync(Path.Combine(_RepositoryRoot, "global.json")));
        string projectPath = Path.Combine(root, "Broken.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(
            Path.Combine(root, "Broken.cs"),
            "namespace Broken; public class Type { invalid syntax here }");

        (Compilation? compilation, string? error) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild: false,
            "Release");

        await Assert.That(compilation).IsNull();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task CreateAsync_ReturnsCompilationForRepositoryProject()
    {
        string projectPath = Path.Combine(
            _RepositoryRoot,
            "src",
            "ExitPoints",
            "ExitPoints.csproj");

        Compilation? compilation = await ProjectCompilationLoader.CreateAsync(projectPath, skipBuild: true);

        await Assert.That(compilation).IsNotNull();
    }

    private static string _RepositoryRoot { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private const string _MinimalProjectXml =
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;
}
