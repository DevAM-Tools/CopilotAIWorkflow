// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Resolves repository root and solution files.</summary>
internal static class WorkspaceResolver
{
    /// <summary>Finds the first <c>.slnx</c> or <c>.sln</c> in the repository root.</summary>
    /// <param name="repositoryRoot">Repository root directory.</param>
    /// <returns>Absolute solution path, or <see langword="null"/> when none exists.</returns>
    public static string? FindSolutionInRepositoryRoot(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);
        string fullRoot = Path.GetFullPath(repositoryRoot);
        if (!Directory.Exists(fullRoot))
        {
            return null;
        }

        string? slnx = Directory.EnumerateFiles(fullRoot, "*.slnx", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (!string.IsNullOrEmpty(slnx))
        {
            return slnx;
        }

        return Directory.EnumerateFiles(fullRoot, "*.sln", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    /// <summary>Resolves the solution path from CLI options.</summary>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="solutionPath">Resolved absolute solution path.</param>
    /// <param name="error">Error message when resolution fails.</param>
    /// <returns><see langword="true"/> when a solution path was resolved.</returns>
    public static bool TryResolveSolutionPath(CliOptions options, out string? solutionPath, out string? error)
    {
        ArgumentNullException.ThrowIfNull(options);
        solutionPath = null;
        error = null;
        string repositoryRoot = Path.GetFullPath(options.RepositoryRoot);

        if (options.TargetKind == RunTargetKind.SolutionPath)
        {
            if (string.IsNullOrWhiteSpace(options.SolutionPath))
            {
                error = "Solution path is required.";
                return false;
            }

            solutionPath = Path.GetFullPath(options.SolutionPath, repositoryRoot);
            if (!File.Exists(solutionPath))
            {
                error = $"Solution file not found: {solutionPath}";
                return false;
            }

            return true;
        }

        if (options.TargetKind == RunTargetKind.SolutionAuto)
        {
            solutionPath = FindSolutionInRepositoryRoot(repositoryRoot);
            if (string.IsNullOrEmpty(solutionPath))
            {
                error = "No .slnx or .sln found in repo root. Pass 'solution <path>' or 'project <path.csproj>'.";
                return false;
            }

            return true;
        }

        error = "Solution resolution is not applicable for project targets.";
        return false;
    }
}
