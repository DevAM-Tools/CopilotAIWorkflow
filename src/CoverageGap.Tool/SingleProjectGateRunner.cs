// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Runs the exit-point gate for one production project.</summary>
internal static class SingleProjectGateRunner
{
    public static async Task<ProjectGateResult?> RunAsync(
        ProductionProjectRecord project,
        CliOptions options,
        string workDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);

        List<string> coberturaPaths = new List<string>(options.CoberturaPaths);
        int testExitCode = 0;

        bool ranTests = false;
        if (coberturaPaths.Count == 0)
        {
            if (string.IsNullOrEmpty(project.TestProjectPath))
            {
                await Console.Error.WriteLineAsync($"No test project paired for {project.ProjectPath}.").ConfigureAwait(false);
                return null;
            }

            TestRunResult? testRun = await TestRunner.RunAsync(
                project.TestProjectPath,
                project.Name,
                workDirectory,
                options.Configuration,
                options.NoBuild,
                cancellationToken).ConfigureAwait(false);
            if (testRun is null)
            {
                return null;
            }

            coberturaPaths.Add(testRun.CoberturaPath);
            testExitCode = testRun.TestExitCode;
            ranTests = true;
        }

        if (!CoberturaDocumentLoader.TryLoad(
                coberturaPaths,
                project.Name,
                options.AllowEmptyCoverage,
                out List<CoberturaDocument> documents,
                out List<ScopedCoberturaDocument> scopedDocuments,
                out string? coberturaError))
        {
            await Console.Error.WriteLineAsync(coberturaError ?? "Failed to load Cobertura.").ConfigureAwait(false);
            return null;
        }

        bool skipBuild = options.NoBuild || ranTests || options.CoberturaPaths.Count > 0;
        (Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)? loaded =
            await ProjectExitLoader.TryLoadAsync(project.ProjectPath, options, skipBuild, cancellationToken).ConfigureAwait(false);
        if (loaded is null)
        {
            return null;
        }

        string repositoryRoot = Path.GetFullPath(options.RepositoryRoot);
        CoverageGapReport report = CoverageGapReportBuilder.Build(
            loaded.Value.Exits,
            documents,
            scopedDocuments,
            repositoryRoot,
            options.IncludeSnippets);

        bool gatePassed = report.Summary.GatePassed && testExitCode == 0;
        return new ProjectGateResult(
            project.ProjectPath,
            project.TestProjectPath,
            report,
            gatePassed,
            testExitCode);
    }
}

/// <summary>Gate outcome for one production project.</summary>
internal sealed record ProjectGateResult(
    string ProjectPath,
    string? TestProjectPath,
    CoverageGapReport Report,
    bool GatePassed,
    int TestExitCode);
