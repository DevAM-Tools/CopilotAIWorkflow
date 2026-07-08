// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Helpers;

/// <summary>Temporary multi-project workspace for performance comparisons.</summary>
internal sealed class GapMultiProjectWorkspace : IAsyncDisposable
{
    private GapMultiProjectWorkspace(string rootPath, string solutionPath, string workDirectory, int projectCount)
    {
        RootPath = rootPath;
        SolutionPath = solutionPath;
        WorkDirectory = workDirectory;
        ProjectCount = projectCount;
    }

    public string RootPath { get; }

    public string SolutionPath { get; }

    public string WorkDirectory { get; }

    public int ProjectCount { get; }

    public static async Task<GapMultiProjectWorkspace> CreateAsync(int projectCount = 6)
    {
        string repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string rootPath = Path.Combine(Path.GetTempPath(), $"gap-multi-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        await File.WriteAllTextAsync(
            Path.Combine(rootPath, "global.json"),
            await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "global.json")));

        StringBuilder solutionBuilder = new();
        solutionBuilder.AppendLine("<Solution>");

        for (int projectIndex = 1; projectIndex <= projectCount; projectIndex++)
        {
            string libraryName = $"PerfLib{projectIndex}";
            string testsName = $"{libraryName}.Tests";
            string libraryDirectory = Path.Combine(rootPath, libraryName);
            string testsDirectory = Path.Combine(rootPath, testsName);
            Directory.CreateDirectory(libraryDirectory);
            Directory.CreateDirectory(testsDirectory);

            string libraryProject = Path.Combine(libraryDirectory, $"{libraryName}.csproj");
            string testsProject = Path.Combine(testsDirectory, $"{testsName}.csproj");
            await File.WriteAllTextAsync(
                libraryProject,
                GapSampleWorkspace.LibraryProjectXml.Replace("GapSample", libraryName, StringComparison.Ordinal));
            await File.WriteAllTextAsync(
                testsProject,
                GapSampleWorkspace.TestsProjectXml
                    .Replace("GapSample.Tests", testsName, StringComparison.Ordinal)
                    .Replace("GapSample\\GapSample.csproj", $"{libraryName}\\{libraryName}.csproj", StringComparison.Ordinal));
            await File.WriteAllTextAsync(
                Path.Combine(libraryDirectory, "Calculator.cs"),
                GapSampleWorkspace.CalculatorSource.Replace("GapSample", libraryName, StringComparison.Ordinal));
            await File.WriteAllTextAsync(
                Path.Combine(testsDirectory, "CalculatorTests.cs"),
                GapSampleWorkspace.FullCoverageTestsSource.Replace("GapSample", libraryName, StringComparison.Ordinal));
            await File.WriteAllTextAsync(
                Path.Combine(testsDirectory, "GlobalUsings.cs"),
                "global using TUnit.Assertions;");

            solutionBuilder.AppendLine(CultureInfo.InvariantCulture, $"  <Project Path=\"{libraryName}/{libraryName}.csproj\" />");
            solutionBuilder.AppendLine(CultureInfo.InvariantCulture, $"  <Project Path=\"{testsName}/{testsName}.csproj\" />");
        }

        solutionBuilder.AppendLine("</Solution>");
        string solutionPath = Path.Combine(rootPath, "PerfBench.slnx");
        await File.WriteAllTextAsync(solutionPath, solutionBuilder.ToString());

        int restoreCode = await _RunDotNetAsync($"restore \"{solutionPath}\"", rootPath);
        await Assert.That(restoreCode).IsEqualTo(0);

        int buildCode = await _RunDotNetAsync($"build \"{solutionPath}\" -c Release --no-restore", rootPath);
        await Assert.That(buildCode).IsEqualTo(0);

        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        return new GapMultiProjectWorkspace(rootPath, solutionPath, workDirectory, projectCount);
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
