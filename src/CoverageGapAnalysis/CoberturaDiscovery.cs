// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis;

/// <summary>Discovers Cobertura files within explicit directories.
/// Thread-safe; all members are stateless.
/// </summary>
public static class CoberturaDiscovery
{
    private const int _MaxParentHops = 32;

    /// <summary>Finds the newest Cobertura file under a single results directory.</summary>
    /// <param name="resultsDirectory">Directory that contains Cobertura output.</param>
    /// <returns>Newest <c>*.cobertura.xml</c> path, or <see langword="null"/> when none exist.</returns>
    public static string? FindNewestCoberturaInDirectory(string resultsDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(resultsDirectory);
        if (!Directory.Exists(resultsDirectory))
        {
            return null;
        }

        return Directory.EnumerateFiles(resultsDirectory, "*.cobertura.xml", SearchOption.AllDirectories)
            .OrderByDescending(static path => File.GetLastWriteTimeUtc(path))
            .FirstOrDefault();
    }

    /// <summary>Test seam for resolving a Cobertura path to a test project name.</summary>
    /// <param name="coberturaPath">Cobertura file path.</param>
    /// <returns>Test project folder name when found.</returns>
    internal static string? ReadTestProjectNameForTests(string coberturaPath)
    {
        return _ReadTestProjectName(coberturaPath);
    }

    [ExcludeFromCodeCoverage(Justification = "Directory is always present for absolute Cobertura paths supplied by the tool.")]
    private static string? _ReadTestProjectName(string coberturaPath)
    {
        DirectoryInfo? directory = new FileInfo(coberturaPath).Directory;
        if (directory is null)
        {
            return null;
        }

        return _ReadTestProjectDirectory(directory);
    }

    /// <summary>Test seam for walking from a known directory to a test project folder.</summary>
    /// <param name="directory">Starting directory.</param>
    /// <returns>Test project folder name when found.</returns>
    internal static string? ReadTestProjectDirectoryForTests(DirectoryInfo directory)
    {
        return _ReadTestProjectDirectory(directory);
    }

    private static string? _ReadTestProjectDirectory(DirectoryInfo directory)
    {
        int hops = 0;
        while (hops < _MaxParentHops && directory.Parent is not null)
        {
            if (directory.Name.EndsWith(".Tests", StringComparison.Ordinal))
            {
                return directory.Name;
            }

            directory = directory.Parent;
            hops++;
        }

        return null;
    }
}
