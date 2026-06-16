// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

internal static class ProjectExitLoader
{
    /// <summary>Loads a compilation and filtered exit points for a project.</summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <param name="flags">Parsed command-line flags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compilation and exits on success.</returns>
    public static async Task<(Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)?> TryLoadAsync(
        string projectPath,
        CommandLineFlags flags,
        CancellationToken cancellationToken = default)
    {
        if (!ProjectCompilationLoader.TryCreate(
                projectPath,
                flags.NoBuild,
                flags.Configuration,
                out Compilation? compilation,
                out string? error))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Console.Error.WriteLineAsync(error ?? "Failed to load compilation.").ConfigureAwait(false);
            return null;
        }

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(compilation!);
        exits = ExitPointFilter.RemoveExcluded(exits, compilation!);
        return (compilation!, exits);
    }
}
