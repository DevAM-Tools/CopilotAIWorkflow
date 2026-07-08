// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Runs the exit-point gate for one production project.</summary>
internal static class SingleProjectGapRunner
{
    /// <summary>Runs gap analysis for one production project.</summary>
    /// <param name="project">Production project record.</param>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="workDirectory">Invocation work directory.</param>
    /// <param name="forceSkipBuild">When <see langword="true"/>, skips per-project build.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Project gap result on success.</returns>
    public static async Task<ProjectGapResult?> RunAsync(
        ProductionProjectRecord project,
        CliOptions options,
        string workDirectory,
        bool forceSkipBuild,
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
                await ParallelRunCoordinator.WriteErrorLineAsync(
                    $"No test project paired for {project.ProjectPath}.",
                    cancellationToken).ConfigureAwait(false);
                return null;
            }

            bool skipTestBuild = options.NoBuild || forceSkipBuild;
            TestRunResult? testRun = await TestRunner.RunAsync(
                project.TestProjectPath,
                project.Name,
                workDirectory,
                options.Configuration,
                skipTestBuild,
                cancellationToken).ConfigureAwait(false);
            if (testRun is null)
            {
                return null;
            }

            coberturaPaths.Add(testRun.CoberturaPath);
            testExitCode = testRun.TestExitCode;
            ranTests = true;
        }

        bool skipBuild = options.NoBuild || forceSkipBuild || ranTests || options.CoberturaPaths.Count > 0;

        Task<CoberturaLoadResult?> coberturaTask = Task.Run(
            () => _TryLoadCobertura(coberturaPaths, project.Name, options.AllowEmptyCoverage),
            cancellationToken);
        Task<IReadOnlyList<ExitPointEntry>?> exitsTask = ProjectExitLoader.TryLoadAsync(
            project.ProjectPath,
            options,
            skipBuild,
            cancellationToken);

        await Task.WhenAll(coberturaTask, exitsTask).ConfigureAwait(false);

        CoberturaLoadResult? coberturaResult = await coberturaTask.ConfigureAwait(false);
        if (coberturaResult is null || !string.IsNullOrEmpty(coberturaResult.Error))
        {
            await ParallelRunCoordinator.WriteErrorLineAsync(
                coberturaResult?.Error ?? "Failed to load Cobertura.",
                cancellationToken).ConfigureAwait(false);
            return null;
        }

        IReadOnlyList<ExitPointEntry>? exits = await exitsTask.ConfigureAwait(false);
        if (exits is null)
        {
            return null;
        }

        string repositoryRoot = Path.GetFullPath(options.RepositoryRoot);
        ExitPointGapReport report = ExitPointGapReportBuilder.Build(
            exits,
            coberturaResult.AllDocuments,
            coberturaResult.ScopedDocuments,
            repositoryRoot,
            options.IncludeSnippets);

        bool gatePassed = report.Summary.GatePassed && testExitCode == 0;
        return new ProjectGapResult(
            project.ProjectPath,
            project.TestProjectPath,
            report,
            gatePassed,
            testExitCode);
    }

    private static CoberturaLoadResult? _TryLoadCobertura(
        IReadOnlyList<string> coberturaPaths,
        string productionProjectName,
        bool allowEmptyCoverage)
    {
        if (!CoberturaDocumentLoader.TryLoad(
                coberturaPaths,
                productionProjectName,
                allowEmptyCoverage,
                out List<CoberturaDocument> documents,
                out List<ScopedCoberturaDocument> scopedDocuments,
                out string? coberturaError))
        {
            return new CoberturaLoadResult([], [], coberturaError ?? "Failed to load Cobertura.");
        }

        return new CoberturaLoadResult(documents, scopedDocuments, null);
    }

    private sealed record CoberturaLoadResult(
        List<CoberturaDocument> AllDocuments,
        List<ScopedCoberturaDocument> ScopedDocuments,
        string? Error);
}

/// <summary>Gap-run outcome for one production project.</summary>
internal sealed record ProjectGapResult(
    string ProjectPath,
    string? TestProjectPath,
    ExitPointGapReport Report,
    bool GatePassed,
    int TestExitCode);
