// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Writes aggregated gate output without shared overwrite paths.</summary>
internal static class AggregatedGateReportWriter
{
    public static async Task WriteAsync(
        AggregatedGateReport report,
        CliOptions options,
        string workDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);

        string payload = AggregatedGateReportSerializer.ToAgentJson(report);
        string? resolvedOutput = RunIsolation.ResolveOutputPath(options.OutputPath, workDirectory);

        if (resolvedOutput is null)
        {
            await OutputWriter.WriteAsync(payload, null, appendNewLine: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_IsDirectoryOutput(options.OutputPath, resolvedOutput))
        {
            string outputDirectory = RunIsolation.ResolveOutputDirectory(options.OutputPath, workDirectory);
            string summaryPath = Path.Combine(outputDirectory, CliConstants.SummaryFileName);
            await File.WriteAllTextAsync(summaryPath, payload, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            for (int projectIndex = 0; projectIndex < report.Projects.Count; projectIndex++)
            {
                ProjectGateResult project = report.Projects[projectIndex];
                string projectFile = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(project.ProjectPath)}.json");
                string projectPayload = GateReportFormatter.Format(project.Report, options.Format);
                await File.WriteAllTextAsync(projectFile, projectPayload, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        await File.WriteAllTextAsync(resolvedOutput, payload, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
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
