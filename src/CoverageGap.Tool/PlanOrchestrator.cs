// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Exports exit-point manifests without running tests.</summary>
internal static class PlanOrchestrator
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!CliParser.IsValidPlanFormat(options.Format))
        {
            await Console.Error.WriteLineAsync($"Unknown manifest format: {options.Format}. Use agent or text.")
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
            if (projects.Count == 1)
            {
                return await _WriteSingleProjectAsync(projects[0], options, workDirectory, cancellationToken).ConfigureAwait(false);
            }

            string outputDirectory = RunIsolation.ResolveOutputDirectory(options.OutputPath, workDirectory);
            for (int projectIndex = 0; projectIndex < projects.Count; projectIndex++)
            {
                ProductionProjectRecord project = projects[projectIndex];
                (Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)? loaded =
                    await ProjectExitLoader.TryLoadAsync(project.ProjectPath, options, cancellationToken).ConfigureAwait(false);
                if (loaded is null)
                {
                    return CliConstants.ExitGateOrToolFailure;
                }

                string output = ExitManifestFormatter.Format(loaded.Value.Exits, options.Format);
                string projectFile = Path.Combine(outputDirectory, $"{project.Name}-exits.json");
                await File.WriteAllTextAsync(projectFile, output, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                await Console.Out.WriteLineAsync(outputDirectory).ConfigureAwait(false);
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
        CancellationToken cancellationToken)
    {
        (Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)? loaded =
            await ProjectExitLoader.TryLoadAsync(project.ProjectPath, options, cancellationToken).ConfigureAwait(false);
        if (loaded is null)
        {
            return CliConstants.ExitGateOrToolFailure;
        }

        string output = ExitManifestFormatter.Format(loaded.Value.Exits, options.Format);
        string? resolvedOutput = RunIsolation.ResolveOutputPath(options.OutputPath, workDirectory);
        await OutputWriter.WriteAsync(output, resolvedOutput, appendNewLine: true, cancellationToken).ConfigureAwait(false);
        return CliConstants.ExitSuccess;
    }
}
