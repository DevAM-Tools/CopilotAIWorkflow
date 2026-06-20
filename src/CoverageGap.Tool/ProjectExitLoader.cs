// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

internal static class ProjectExitLoader
{
    /// <summary>Loads a compilation and filtered exit points for a project.</summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compilation and exits on success.</returns>
    public static async Task<(Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)?> TryLoadAsync(
        string projectPath,
        CliOptions options,
        CancellationToken cancellationToken = default)
    {
        return await TryLoadAsync(projectPath, options, options.NoBuild, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads a compilation and filtered exit points for a project.</summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="skipBuild">When <see langword="true"/>, skips restore/build (e.g. after <c>dotnet test</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compilation and exits on success.</returns>
    public static async Task<(Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)?> TryLoadAsync(
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
            await Console.Error.WriteLineAsync(error ?? "Failed to load compilation.").ConfigureAwait(false);
            return null;
        }

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(compilation!);
        exits = ExitPointFilter.RemoveExcluded(exits, compilation!);
        return (compilation!, exits);
    }
}
