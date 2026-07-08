// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Exports exit-point manifests without running tests.</summary>
internal static class PlanOrchestrator
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!CliParser.IsValidPlanFormat(options.Format))
        {
            await ParallelRunCoordinator.WriteErrorLineAsync(
                $"Unknown manifest format: {options.Format}. Use agent or text.",
                cancellationToken).ConfigureAwait(false);
            return CliConstants.ExitUsageError;
        }

        if (!ProjectGraphBuilder.TryBuild(options, out IReadOnlyList<ProductionProjectRecord> projects, out string? graphError))
        {
            await ParallelRunCoordinator.WriteErrorLineAsync(graphError ?? "Failed to build project graph.", cancellationToken)
                .ConfigureAwait(false);
            return CliConstants.ExitGateOrToolFailure;
        }

        if (!RunIsolation.TryReserveWorkDirectory(options.WorkDirectory, out string? workDirectory, out string? workDirError)
            || string.IsNullOrEmpty(workDirectory))
        {
            await ParallelRunCoordinator.WriteErrorLineAsync(workDirError ?? "Failed to reserve work directory.", cancellationToken)
                .ConfigureAwait(false);
            return CliConstants.ExitGateOrToolFailure;
        }

        try
        {
            if (projects.Count == 1)
            {
                return await _WriteSingleProjectAsync(projects[0], options, workDirectory, forceSkipBuild: false, cancellationToken)
                    .ConfigureAwait(false);
            }

            bool solutionBuilt = await SolutionBuildRunner.TryBuildOnceAsync(options, projects, cancellationToken)
                .ConfigureAwait(false);
            if (!solutionBuilt)
            {
                return CliConstants.ExitGateOrToolFailure;
            }

            string outputDirectory = RunIsolation.ResolveOutputDirectory(options.OutputPath, workDirectory);
            int parallelism = ParallelismDefaults.Resolve(options, projects.Count);
            RunSummaryAccumulator accumulator = await ParallelRunCoordinator.RunAllAsync(
                projects,
                async (project, projectCancellationToken) =>
                {
                    IReadOnlyList<ExitPointEntry>? exits = await ProjectExitLoader.TryLoadAsync(
                        project.ProjectPath,
                        options,
                        skipBuild: true,
                        projectCancellationToken).ConfigureAwait(false);
                    if (exits is null)
                    {
                        return new ProjectRunOutcome(
                            new ProjectSummaryEntry(
                                project.ProjectPath,
                                project.TestProjectPath,
                                false,
                                0,
                                0,
                                1,
                                null,
                                true),
                            $"Failed to load exits for {project.ProjectPath}.");
                    }

                    string output = ExitManifestFormatter.Format(exits, options.Format);
                    string projectFile = Path.Combine(outputDirectory, $"{project.Name}-exits.json");
                    await File.WriteAllTextAsync(projectFile, output, Encoding.UTF8, projectCancellationToken).ConfigureAwait(false);

                    ProjectSummaryEntry entry = new ProjectSummaryEntry(
                        project.ProjectPath,
                        project.TestProjectPath,
                        true,
                        0,
                        0,
                        0,
                        Path.GetFileName(projectFile),
                        false);
                    return new ProjectRunOutcome(entry, null);
                },
                parallelism,
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                await Console.Out.WriteLineAsync(outputDirectory).ConfigureAwait(false);
            }

            if (accumulator.HasToolFailure)
            {
                return CliConstants.ExitGateOrToolFailure;
            }

            return CliConstants.ExitSuccess;
        }
        finally
        {
            RunIsolation.TryCleanupWorkDirectory(workDirectory, options.KeepWorkDir);
        }
    }

    private static async Task<int> _WriteSingleProjectAsync(
        ProductionProjectRecord project,
        CliOptions options,
        string workDirectory,
        bool forceSkipBuild,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ExitPointEntry>? exits = await ProjectExitLoader.TryLoadAsync(
            project.ProjectPath,
            options,
            forceSkipBuild ? true : options.NoBuild,
            cancellationToken).ConfigureAwait(false);
        if (exits is null)
        {
            return CliConstants.ExitGateOrToolFailure;
        }

        string output = ExitManifestFormatter.Format(exits, options.Format);
        string? resolvedOutput = RunIsolation.ResolveOutputPath(options.OutputPath, workDirectory);
        await OutputWriter.WriteAsync(output, resolvedOutput, appendNewLine: true, cancellationToken).ConfigureAwait(false);
        return CliConstants.ExitSuccess;
    }
}
