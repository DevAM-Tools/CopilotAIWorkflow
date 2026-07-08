// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Finds Cobertura files inside an isolated results directory.</summary>
internal static class CoberturaPathFinder
{
    /// <summary>Finds the newest Cobertura file under a results directory.</summary>
    /// <param name="resultsDirectory">Directory passed to MTP <c>--results-directory</c>.</param>
    /// <param name="coberturaPath">Resolved Cobertura file path.</param>
    /// <param name="error">Error message when no file is found.</param>
    /// <returns><see langword="true"/> when a Cobertura file exists.</returns>
    public static bool TryFindNewest(string resultsDirectory, out string? coberturaPath, out string? error)
    {
        coberturaPath = null;
        error = null;
        ArgumentException.ThrowIfNullOrEmpty(resultsDirectory);

        if (!Directory.Exists(resultsDirectory))
        {
            error = $"Results directory not found: {resultsDirectory}";
            return false;
        }

        string? newest = Directory.EnumerateFiles(resultsDirectory, "*.cobertura.xml", SearchOption.AllDirectories)
            .OrderByDescending(static path => File.GetLastWriteTimeUtc(path))
            .FirstOrDefault();

        if (string.IsNullOrEmpty(newest))
        {
            error = $"No Cobertura file found under {resultsDirectory}.";
            return false;
        }

        coberturaPath = newest;
        return true;
    }
}
