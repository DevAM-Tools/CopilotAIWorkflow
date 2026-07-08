// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

internal static class ProjectExitLoader
{
    /// <summary>Loads filtered exit points for a project.</summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit points on success.</returns>
    public static async Task<IReadOnlyList<ExitPointEntry>?> TryLoadAsync(
        string projectPath,
        CliOptions options,
        CancellationToken cancellationToken = default)
    {
        return await TryLoadAsync(projectPath, options, options.NoBuild, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads filtered exit points for a project.</summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="skipBuild">When <see langword="true"/>, skips restore/build.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit points on success.</returns>
    public static async Task<IReadOnlyList<ExitPointEntry>?> TryLoadAsync(
        string projectPath,
        CliOptions options,
        bool skipBuild,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        (Compilation? compilation, string? error) = await ProjectCompilationLoader.TryCreateAsync(
            projectPath,
            skipBuild,
            options.Configuration,
            cancellationToken).ConfigureAwait(false);

        if (compilation is null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ParallelRunCoordinator.WriteErrorLineAsync(error ?? "Failed to load compilation.", cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(compilation);
        exits = ExitPointFilter.RemoveExcluded(exits, compilation);
        return exits;
    }
}
