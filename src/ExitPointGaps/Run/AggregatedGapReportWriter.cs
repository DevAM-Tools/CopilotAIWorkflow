// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Writes aggregated gate output without shared overwrite paths.</summary>
internal static class AggregatedGapReportWriter
{
    public static async Task WriteAsync(
        AggregatedGapReport report,
        CliOptions options,
        string workDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);

        RunSummaryAccumulator accumulator = new();
        for (int projectIndex = 0; projectIndex < report.Projects.Count; projectIndex++)
        {
            ProjectGapResult project = report.Projects[projectIndex];
            accumulator.AddProject(new ProjectSummaryEntry(
                project.ProjectPath,
                project.TestProjectPath,
                project.GatePassed,
                project.Report.Summary.ExitGapCount,
                project.Report.Summary.BranchGapCount,
                project.TestExitCode,
                $"{Path.GetFileNameWithoutExtension(project.ProjectPath)}.json",
                false));
        }

        StreamingGapReportWriter writer = new StreamingGapReportWriter(options.Format, streamToStdout: false);
        string? resolvedOutput = RunIsolation.ResolveOutputPath(options.OutputPath, workDirectory);

        if (resolvedOutput is null)
        {
            string payload = AggregatedGapReportSerializer.ToAgentJsonV3(accumulator, report.Projects.Count);
            await OutputWriter.WriteAsync(payload, null, appendNewLine: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_IsDirectoryOutput(options.OutputPath, resolvedOutput))
        {
            string outputDirectory = RunIsolation.ResolveOutputDirectory(options.OutputPath, workDirectory);
            for (int projectIndex = 0; projectIndex < report.Projects.Count; projectIndex++)
            {
                await writer.WriteProjectAsync(report.Projects[projectIndex], outputDirectory, cancellationToken)
                    .ConfigureAwait(false);
            }

            await writer.WriteSummaryAsync(accumulator, report.Projects.Count, outputDirectory, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (report.Projects.Count == 1)
        {
            await writer.WriteSingleProjectFileAsync(report.Projects[0], resolvedOutput, cancellationToken).ConfigureAwait(false);
            return;
        }

        string summaryPayload = AggregatedGapReportSerializer.ToAgentJsonV3(accumulator, report.Projects.Count);
        await File.WriteAllTextAsync(resolvedOutput, summaryPayload, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
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
}
