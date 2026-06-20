// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Runs test projects with isolated coverage output.</summary>
internal static class TestRunner
{
    private const int _MtpNoTestsExitCode = 8;

    /// <summary>Runs tests and returns the Cobertura path from the isolated results directory.</summary>
    /// <param name="testProjectPath">Absolute test project path.</param>
    /// <param name="productionProjectName">Production project name for work-dir layout.</param>
    /// <param name="workDirectory">Invocation work root.</param>
    /// <param name="configuration">Build configuration.</param>
    /// <param name="noBuild">Whether to skip build.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run result including Cobertura path and test exit code.</returns>
    public static async Task<TestRunResult?> RunAsync(
        string testProjectPath,
        string productionProjectName,
        string workDirectory,
        string configuration,
        bool noBuild,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(testProjectPath);
        ArgumentException.ThrowIfNullOrEmpty(productionProjectName);
        ArgumentException.ThrowIfNullOrEmpty(workDirectory);

        if (!File.Exists(testProjectPath))
        {
            await Console.Error.WriteLineAsync($"Test project not found: {testProjectPath}").ConfigureAwait(false);
            return null;
        }

        string resultsDirectory = RunIsolation.CreateTestResultsDirectory(workDirectory, productionProjectName);
        string testDirectory = Path.GetDirectoryName(testProjectPath) ?? workDirectory;
        string noBuildFlag = noBuild ? " --no-build --no-restore" : string.Empty;
        bool usesTUnit = ProjectReferenceScanner.ReferencesTUnit(testProjectPath);
        string coverageArgs = usesTUnit
            ? "-- --reflection --coverage --coverage-output-format cobertura"
            : "-- --coverage --coverage-output-format cobertura";

        string arguments =
            $"test \"{testProjectPath}\" -c {configuration}{noBuildFlag} --results-directory \"{resultsDirectory}\" {coverageArgs}";

        DotNetProcessResult processResult = await DotNetProcess.RunAsync(arguments, testDirectory, cancellationToken)
            .ConfigureAwait(false);

        if (!CoberturaPathFinder.TryFindNewest(resultsDirectory, out string? coberturaPath, out string? coberturaError))
        {
            if (processResult.ExitCode == _MtpNoTestsExitCode)
            {
                await Console.Error.WriteLineAsync(
                    "No tests were executed. Use 'coveragegap plan' to list exit points before adding tests.")
                    .ConfigureAwait(false);
            }
            else
            {
                await Console.Error.WriteLineAsync(coberturaError ?? "Cobertura file not found after test run.")
                    .ConfigureAwait(false);
            }

            return null;
        }

        return new TestRunResult(coberturaPath!, processResult.ExitCode, processResult.StandardError);
    }
}

/// <summary>Result of an isolated test run.</summary>
/// <param name="CoberturaPath">Absolute Cobertura file path.</param>
/// <param name="TestExitCode">MTP/dotnet test exit code.</param>
/// <param name="StandardError">Captured stderr from the test process.</param>
internal sealed record TestRunResult(string CoberturaPath, int TestExitCode, string StandardError);
