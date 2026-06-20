// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool.Tests;

/// <summary>Tests for <see cref="ProjectGraphBuilder"/> and pairing.</summary>
public sealed class ProjectGraphTests
{
    [Test]
    public async Task Pairer_SiblingConvention_FindsTestProject()
    {
        string root = await TempWorkspace.CreateAsync();
        string libraryDirectory = Path.Combine(root, "GapSample");
        string testsDirectory = Path.Combine(root, "GapSample.Tests");
        Directory.CreateDirectory(libraryDirectory);
        Directory.CreateDirectory(testsDirectory);
        string production = Path.Combine(libraryDirectory, "GapSample.csproj");
        string tests = Path.Combine(testsDirectory, "GapSample.Tests.csproj");
        await File.WriteAllTextAsync(production, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        await File.WriteAllTextAsync(tests, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        string? paired = TestProjectPairer.FindTestProject(production, root, null);

        await Assert.That(paired).IsEqualTo(Path.GetFullPath(tests));
    }

    [Test]
    public async Task Pairer_ReferenceScan_FindsTestProject()
    {
        string root = await TempWorkspace.CreateAsync();
        string libraryDirectory = Path.Combine(root, "lib", "Core");
        string testsDirectory = Path.Combine(root, "tests", "Core.Tests");
        Directory.CreateDirectory(libraryDirectory);
        Directory.CreateDirectory(testsDirectory);
        string production = Path.Combine(libraryDirectory, "Core.csproj");
        string tests = Path.Combine(testsDirectory, "Core.Tests.csproj");
        await File.WriteAllTextAsync(production, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        await File.WriteAllTextAsync(
            tests,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\lib\Core\Core.csproj" />
              </ItemGroup>
            </Project>
            """);

        string? paired = TestProjectPairer.FindTestProject(production, root, null);

        await Assert.That(paired).IsEqualTo(Path.GetFullPath(tests));
    }

    [Test]
    public async Task GraphBuilder_MissingTest_ReturnsError()
    {
        string root = await TempWorkspace.CreateAsync();
        string production = Path.Combine(root, "Lonely", "Lonely.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(production)!);
        await File.WriteAllTextAsync(production, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        CliOptions options = new CliOptions
        {
            TargetKind = RunTargetKind.Projects,
            RepositoryRoot = root,
        };
        options.ProjectPaths.Add(production);

        bool success = ProjectGraphBuilder.TryBuild(options, out _, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task SolutionParser_ReadsSlnxProjects()
    {
        string root = await TempWorkspace.CreateAsync();
        string solutionPath = Path.Combine(root, "Demo.slnx");
        string projectPath = Path.Combine(root, "src", "Demo", "Demo.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
        await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        await File.WriteAllTextAsync(
            solutionPath,
            $"""
            <Solution>
              <Project Path="src/Demo/Demo.csproj" />
            </Solution>
            """);

        IReadOnlyList<string> projects = SolutionParser.TryReadProjectPaths(solutionPath, root, out IReadOnlyList<string> parsed, out string? _)
            ? parsed
            : [];

        await Assert.That(projects.Count).IsEqualTo(1);
        await Assert.That(projects[0]).IsEqualTo(Path.GetFullPath(projectPath));
    }

    [Test]
    public async Task SolutionParser_MalformedSlnx_ReturnsError()
    {
        string root = await TempWorkspace.CreateAsync();
        string solutionPath = Path.Combine(root, "Broken.slnx");
        await File.WriteAllTextAsync(solutionPath, "<Solution><NotClosed>");

        bool success = SolutionParser.TryReadProjectPaths(solutionPath, root, out IReadOnlyList<string> projects, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(projects.Count).IsEqualTo(0);
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task ProjectXmlLoader_MalformedProject_ReturnsError()
    {
        string root = await TempWorkspace.CreateAsync();
        string projectPath = Path.Combine(root, "Broken.csproj");
        await File.WriteAllTextAsync(projectPath, "<Project><NotClosed>");

        bool success = ProjectXmlLoader.TryLoadDocument(projectPath, out XDocument? document, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(document).IsNull();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task GraphBuilder_SkipNoTests_OmitsUnpairedProject()
    {
        string root = await TempWorkspace.CreateAsync();
        string pairedLibraryDirectory = Path.Combine(root, "Paired");
        string pairedTestsDirectory = Path.Combine(root, "Paired.Tests");
        string lonelyDirectory = Path.Combine(root, "Lonely");
        Directory.CreateDirectory(pairedLibraryDirectory);
        Directory.CreateDirectory(pairedTestsDirectory);
        Directory.CreateDirectory(lonelyDirectory);

        string pairedProduction = Path.Combine(pairedLibraryDirectory, "Paired.csproj");
        string pairedTests = Path.Combine(pairedTestsDirectory, "Paired.Tests.csproj");
        string lonelyProduction = Path.Combine(lonelyDirectory, "Lonely.csproj");
        await File.WriteAllTextAsync(pairedProduction, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        await File.WriteAllTextAsync(pairedTests, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        await File.WriteAllTextAsync(lonelyProduction, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        CliOptions options = new CliOptions
        {
            TargetKind = RunTargetKind.Projects,
            RepositoryRoot = root,
            SkipNoTests = true,
        };
        options.ProjectPaths.Add(pairedProduction);
        options.ProjectPaths.Add(lonelyProduction);

        bool success = ProjectGraphBuilder.TryBuild(options, out IReadOnlyList<ProductionProjectRecord> projects, out string? error);

        await Assert.That(success).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(projects.Count).IsEqualTo(1);
        await Assert.That(projects[0].ProjectPath).IsEqualTo(Path.GetFullPath(pairedProduction));
    }

    [Test]
    public async Task GraphBuilder_SkipNoTests_Plan_IncludesUnpairedProject()
    {
        string root = await TempWorkspace.CreateAsync();
        string lonelyDirectory = Path.Combine(root, "Lonely");
        Directory.CreateDirectory(lonelyDirectory);
        string lonelyProduction = Path.Combine(lonelyDirectory, "Lonely.csproj");
        await File.WriteAllTextAsync(lonelyProduction, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        CliOptions options = new CliOptions
        {
            Command = CliConstants.PlanCommand,
            TargetKind = RunTargetKind.Projects,
            RepositoryRoot = root,
            SkipNoTests = true,
        };
        options.ProjectPaths.Add(lonelyProduction);

        bool success = ProjectGraphBuilder.TryBuild(options, out IReadOnlyList<ProductionProjectRecord> projects, out string? error);

        await Assert.That(success).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(projects.Count).IsEqualTo(1);
        await Assert.That(projects[0].ProjectPath).IsEqualTo(Path.GetFullPath(lonelyProduction));
        await Assert.That(projects[0].TestProjectPath).IsNull();
    }

    [Test]
    public async Task GraphBuilder_ExecutableProject_ReturnsActionableError()
    {
        string root = await TempWorkspace.CreateAsync();
        string executableProject = Path.Combine(root, "Tool", "Tool.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(executableProject)!);
        await File.WriteAllTextAsync(
            executableProject,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
            </Project>
            """);

        CliOptions options = new CliOptions
        {
            TargetKind = RunTargetKind.Projects,
            RepositoryRoot = root,
        };
        options.ProjectPaths.Add(executableProject);

        bool success = ProjectGraphBuilder.TryBuild(options, out _, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(error).Contains("Executable");
        await Assert.That(error).Contains("excluded");
    }
}
