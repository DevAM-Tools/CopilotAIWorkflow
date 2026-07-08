// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Orchestrates multi-project gate runs.</summary>
internal static class GapRunOrchestrator
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!CliParser.IsValidReportFormat(options.Format))
        {
            await ParallelRunCoordinator.WriteErrorLineAsync(
                $"Unknown report format: {options.Format}. Use agent, text, or compact.",
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
                return await _RunSingleProjectAsync(projects[0], options, workDirectory, forceSkipBuild: false, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!_TryValidateMultiProjectOutput(options, workDirectory, out string? outputError))
            {
                await ParallelRunCoordinator.WriteErrorLineAsync(outputError ?? "Invalid output path.", cancellationToken)
                    .ConfigureAwait(false);
                return CliConstants.ExitUsageError;
            }

            bool solutionBuilt = await SolutionBuildRunner.TryBuildOnceAsync(options, projects, cancellationToken)
                .ConfigureAwait(false);
            if (!solutionBuilt)
            {
                return CliConstants.ExitGateOrToolFailure;
            }

            int parallelism = ParallelismDefaults.Resolve(options, projects.Count);
            bool streamStdout = _ShouldStreamStdout(options, projects.Count);
            string outputDirectory = RunIsolation.ResolveOutputDirectory(options.OutputPath, workDirectory);
            StreamingGapReportWriter writer = new StreamingGapReportWriter(options.Format, streamStdout);
            RunSummaryAccumulator accumulator = await ParallelRunCoordinator.RunAllAsync(
                projects,
                async (project, projectCancellationToken) =>
                {
                    ProjectGapResult? result = await SingleProjectGapRunner.RunAsync(
                        project,
                        options,
                        workDirectory,
                        forceSkipBuild: true,
                        projectCancellationToken).ConfigureAwait(false);
                    if (result is null)
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
                            $"Failed to run gap analysis for {project.ProjectPath}.");
                    }

                    string reportFile = await writer.WriteProjectAsync(result, outputDirectory, projectCancellationToken)
                        .ConfigureAwait(false);
                    ProjectSummaryEntry entry = new ProjectSummaryEntry(
                        result.ProjectPath,
                        result.TestProjectPath,
                        result.GatePassed,
                        result.Report.Summary.ExitGapCount,
                        result.Report.Summary.BranchGapCount,
                        result.TestExitCode,
                        reportFile,
                        false);
                    return new ProjectRunOutcome(entry, null);
                },
                parallelism,
                cancellationToken).ConfigureAwait(false);

            await writer.WriteSummaryAsync(accumulator, projects.Count, outputDirectory, cancellationToken)
                .ConfigureAwait(false);

            AggregatedGapSummary summary = accumulator.ToSummary(projects.Count);
            if (!options.NoFail && (!summary.GatePassed || accumulator.HasToolFailure))
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

    private static async Task<int> _RunSingleProjectAsync(
        ProductionProjectRecord project,
        CliOptions options,
        string workDirectory,
        bool forceSkipBuild,
        CancellationToken cancellationToken)
    {
        ProjectGapResult? result = await SingleProjectGapRunner.RunAsync(
            project,
            options,
            workDirectory,
            forceSkipBuild,
            cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return CliConstants.ExitGateOrToolFailure;
        }

        StreamingGapReportWriter writer = new StreamingGapReportWriter(options.Format, streamToStdout: false);
        string? resolvedOutput = RunIsolation.ResolveOutputPath(options.OutputPath, workDirectory);
        if (resolvedOutput is null)
        {
            await writer.WriteSingleProjectStdoutAsync(result, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await writer.WriteSingleProjectFileAsync(result, resolvedOutput, cancellationToken).ConfigureAwait(false);
        }

        if (!options.NoFail && !result.GatePassed)
        {
            return CliConstants.ExitGateOrToolFailure;
        }

        return CliConstants.ExitSuccess;
    }

    private static bool _TryValidateMultiProjectOutput(CliOptions options, string workDirectory, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            return true;
        }

        string? resolvedOutput = RunIsolation.ResolveOutputPath(options.OutputPath, workDirectory);
        if (resolvedOutput is null)
        {
            return true;
        }

        if (_IsDirectoryOutput(options.OutputPath, resolvedOutput))
        {
            return true;
        }

        error = "Multi-project runs require a directory output path (trailing slash) or stdout without -o.";
        return false;
    }

    private static bool _IsDirectoryOutput(string? outputPath, string resolvedOutput)
    {
        if (!string.IsNullOrWhiteSpace(outputPath)
            && (outputPath.EndsWith('/') || outputPath.EndsWith('\\')))
        {
            return true;
        }

        return Directory.Exists(resolvedOutput);
    }

    private static bool _ShouldStreamStdout(CliOptions options, int projectCount)
    {
        if (options.NoStream)
        {
            return false;
        }

        if (options.Stream)
        {
            return true;
        }

        return projectCount > 1 && string.IsNullOrWhiteSpace(options.OutputPath);
    }
}
