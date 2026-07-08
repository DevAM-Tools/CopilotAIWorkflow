// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests;

/// <summary>Temporary solution for solution-scoped CLI integration tests.</summary>
internal sealed class GapSolutionWorkspace : IAsyncDisposable
{
    private GapSolutionWorkspace(string rootPath, string solutionPath, string workDirectory)
    {
        RootPath = rootPath;
        SolutionPath = solutionPath;
        WorkDirectory = workDirectory;
    }

    public string RootPath { get; }

    public string SolutionPath { get; }

    public string WorkDirectory { get; }

    public static Task<GapSolutionWorkspace> CreateAsync() => CreateAsync(includeUnpairedLibrary: false);

    public static async Task<GapSolutionWorkspace> CreateAsync(bool includeUnpairedLibrary)
    {
        string repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string rootPath = Path.Combine(Path.GetTempPath(), $"gap-solution-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        await File.WriteAllTextAsync(
            Path.Combine(rootPath, "global.json"),
            await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "global.json")));

        string firstLibraryDirectory = Path.Combine(rootPath, "GapSample");
        string firstTestsDirectory = Path.Combine(rootPath, "GapSample.Tests");
        string secondLibraryDirectory = Path.Combine(rootPath, "GapMore");
        string secondTestsDirectory = Path.Combine(rootPath, "GapMore.Tests");
        Directory.CreateDirectory(firstLibraryDirectory);
        Directory.CreateDirectory(firstTestsDirectory);
        Directory.CreateDirectory(secondLibraryDirectory);
        Directory.CreateDirectory(secondTestsDirectory);

        string firstLibraryProject = Path.Combine(firstLibraryDirectory, "GapSample.csproj");
        string firstTestsProject = Path.Combine(firstTestsDirectory, "GapSample.Tests.csproj");
        string secondLibraryProject = Path.Combine(secondLibraryDirectory, "GapMore.csproj");
        string secondTestsProject = Path.Combine(secondTestsDirectory, "GapMore.Tests.csproj");

        await File.WriteAllTextAsync(firstLibraryProject, GapSampleWorkspace.LibraryProjectXml);
        await File.WriteAllTextAsync(firstTestsProject, GapSampleWorkspace.TestsProjectXml);
        await File.WriteAllTextAsync(
            secondLibraryProject,
            GapSampleWorkspace.LibraryProjectXml.Replace("GapSample", "GapMore", StringComparison.Ordinal));
        await File.WriteAllTextAsync(
            secondTestsProject,
            GapSampleWorkspace.TestsProjectXml
                .Replace("GapSample.Tests", "GapMore.Tests", StringComparison.Ordinal)
                .Replace("GapSample\\GapSample.csproj", "GapMore\\GapMore.csproj", StringComparison.Ordinal));

        await File.WriteAllTextAsync(Path.Combine(firstLibraryDirectory, "Calculator.cs"), GapSampleWorkspace.CalculatorSource);
        await File.WriteAllTextAsync(
            Path.Combine(secondLibraryDirectory, "Calculator.cs"),
            GapSampleWorkspace.CalculatorSource.Replace("GapSample", "GapMore", StringComparison.Ordinal));
        await File.WriteAllTextAsync(
            Path.Combine(firstTestsDirectory, "CalculatorTests.cs"),
            GapSampleWorkspace.FullCoverageTestsSource);
        await File.WriteAllTextAsync(
            Path.Combine(secondTestsDirectory, "CalculatorTests.cs"),
            GapSampleWorkspace.FullCoverageTestsSource.Replace("GapSample", "GapMore", StringComparison.Ordinal));
        await File.WriteAllTextAsync(
            Path.Combine(firstTestsDirectory, "GlobalUsings.cs"),
            "global using TUnit.Assertions;");
        await File.WriteAllTextAsync(
            Path.Combine(secondTestsDirectory, "GlobalUsings.cs"),
            "global using TUnit.Assertions;");

        string solutionContent =
            """
            <Solution>
              <Project Path="GapSample/GapSample.csproj" />
              <Project Path="GapMore/GapMore.csproj" />
              <Project Path="GapSample.Tests/GapSample.Tests.csproj" />
              <Project Path="GapMore.Tests/GapMore.Tests.csproj" />
            </Solution>
            """;

        if (includeUnpairedLibrary)
        {
            string lonelyDirectory = Path.Combine(rootPath, "GapLonely");
            Directory.CreateDirectory(lonelyDirectory);
            string lonelyProject = Path.Combine(lonelyDirectory, "GapLonely.csproj");
            await File.WriteAllTextAsync(
                lonelyProject,
                GapSampleWorkspace.LibraryProjectXml.Replace("GapSample", "GapLonely", StringComparison.Ordinal));
            await File.WriteAllTextAsync(
                Path.Combine(lonelyDirectory, "Calculator.cs"),
                GapSampleWorkspace.CalculatorSource.Replace("GapSample", "GapLonely", StringComparison.Ordinal));

            solutionContent =
                """
                <Solution>
                  <Project Path="GapSample/GapSample.csproj" />
                  <Project Path="GapMore/GapMore.csproj" />
                  <Project Path="GapLonely/GapLonely.csproj" />
                  <Project Path="GapSample.Tests/GapSample.Tests.csproj" />
                  <Project Path="GapMore.Tests/GapMore.Tests.csproj" />
                </Solution>
                """;
        }

        string solutionPath = Path.Combine(rootPath, "GapSample.slnx");
        await File.WriteAllTextAsync(solutionPath, solutionContent);

        int restoreCode = await _RunDotNetAsync($"restore \"{solutionPath}\"", rootPath);
        await Assert.That(restoreCode).IsEqualTo(0);

        int buildCode = await _RunDotNetAsync($"build \"{solutionPath}\" -c Release --no-restore", rootPath);
        await Assert.That(buildCode).IsEqualTo(0);

        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        return new GapSolutionWorkspace(rootPath, solutionPath, workDirectory);
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }

        if (Directory.Exists(WorkDirectory))
        {
            Directory.Delete(WorkDirectory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    private static async Task<int> _RunDotNetAsync(string arguments, string workingDirectory)
    {
        DotNetProcessResult result = await DotNetProcess.RunAsync(arguments, workingDirectory);
        if (result.ExitCode != 0)
        {
            string details = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
            throw new InvalidOperationException($"dotnet {arguments} failed ({result.ExitCode}): {details}");
        }

        return result.ExitCode;
    }
}
