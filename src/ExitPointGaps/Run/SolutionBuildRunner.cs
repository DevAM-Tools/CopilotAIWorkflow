// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Runs a single solution-level build before parallel per-project work.</summary>
internal static class SolutionBuildRunner
{
    /// <summary>Builds the solution once when multiple projects are in scope.</summary>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="projects">Production projects in scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when pre-build succeeded or was skipped.</returns>
    public static async Task<bool> TryBuildOnceAsync(
        CliOptions options,
        IReadOnlyList<ProductionProjectRecord> projects,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(projects);

        if (options.NoBuild || projects.Count <= 1)
        {
            return true;
        }

        if (!WorkspaceResolver.TryResolveSolutionPath(options, out string? solutionPath, out string? error)
            || string.IsNullOrEmpty(solutionPath))
        {
            await ParallelRunCoordinator.WriteErrorLineAsync(
                error ?? "Solution path could not be resolved for pre-build.",
                cancellationToken).ConfigureAwait(false);
            return false;
        }

        string repositoryRoot = Path.GetFullPath(options.RepositoryRoot);
        DotNetProcessResult buildResult = await DotNetProcess.RunAsync(
            $"build \"{solutionPath}\" -c {options.Configuration} -v:q",
            repositoryRoot,
            cancellationToken).ConfigureAwait(false);

        if (buildResult.ExitCode != 0)
        {
            await ParallelRunCoordinator.WriteErrorLineAsync(
                "Solution pre-build failed.",
                cancellationToken).ConfigureAwait(false);
            return false;
        }

        return true;
    }
}
