// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Orchestrates multi-project gate runs.</summary>
internal static class GateOrchestrator
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!CliParser.IsValidReportFormat(options.Format))
        {
            await Console.Error.WriteLineAsync($"Unknown report format: {options.Format}. Use agent, text, or compact.")
                .ConfigureAwait(false);
            return CliConstants.ExitUsageError;
        }

        if (!ProjectGraphBuilder.TryBuild(options, out IReadOnlyList<ProductionProjectRecord> projects, out string? graphError))
        {
            await Console.Error.WriteLineAsync(graphError ?? "Failed to build project graph.").ConfigureAwait(false);
            return CliConstants.ExitGateOrToolFailure;
        }

        if (!RunIsolation.TryReserveWorkDirectory(options.WorkDirectory, out string? workDirectory, out string? workDirError)
            || string.IsNullOrEmpty(workDirectory))
        {
            await Console.Error.WriteLineAsync(workDirError ?? "Failed to reserve work directory.").ConfigureAwait(false);
            return CliConstants.ExitGateOrToolFailure;
        }

        try
        {
            List<ProjectGateResult> results = new List<ProjectGateResult>(projects.Count);
            for (int projectIndex = 0; projectIndex < projects.Count; projectIndex++)
            {
                ProjectGateResult? result = await SingleProjectGateRunner.RunAsync(
                    projects[projectIndex],
                    options,
                    workDirectory,
                    cancellationToken).ConfigureAwait(false);
                if (result is null)
                {
                    return CliConstants.ExitGateOrToolFailure;
                }

                results.Add(result);
            }

            AggregatedGateReport aggregated = _BuildAggregatedReport(results);
            await AggregatedGateReportWriter.WriteAsync(aggregated, options, workDirectory, cancellationToken)
                .ConfigureAwait(false);

            if (!options.NoFail && !aggregated.Summary.GatePassed)
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

    private static AggregatedGateReport _BuildAggregatedReport(List<ProjectGateResult> results)
    {
        int exitGapCount = 0;
        int branchGapCount = 0;
        bool gatePassed = true;
        for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
        {
            ProjectGateResult result = results[resultIndex];
            exitGapCount += result.Report.Summary.ExitGapCount;
            branchGapCount += result.Report.Summary.BranchGapCount;
            if (!result.GatePassed)
            {
                gatePassed = false;
            }
        }

        return new AggregatedGateReport
        {
            Summary = new AggregatedGateSummary
            {
                ProjectCount = results.Count,
                GatePassed = gatePassed,
                ExitGapCount = exitGapCount,
                BranchGapCount = branchGapCount,
            },
            Projects = results,
        };
    }
}
