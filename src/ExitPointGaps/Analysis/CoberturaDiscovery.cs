// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis;

/// <summary>Discovers Cobertura files within explicit directories.
/// Thread-safe; all members are stateless.
/// </summary>
internal static class CoberturaDiscovery
{
    #region Fields

    private const int _MaxParentHops = 32;

    #endregion

    #region Public API

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

        return FindNewestCoberturaPath(resultsDirectory);
    }

    /// <summary>Tracks the newest Cobertura file under a directory in one pass.</summary>
    /// <param name="resultsDirectory">Directory that contains Cobertura output.</param>
    /// <returns>Newest path, or <see langword="null"/> when none exist.</returns>
    internal static string? FindNewestCoberturaPath(string resultsDirectory)
    {
        string? newest = null;
        DateTime newestWrite = DateTime.MinValue;
        foreach (string path in Directory.EnumerateFiles(resultsDirectory, "*.cobertura.xml", SearchOption.AllDirectories))
        {
            DateTime write = File.GetLastWriteTimeUtc(path);
            if (newest is null || write > newestWrite)
            {
                newest = path;
                newestWrite = write;
            }
        }

        return newest;
    }

    /// <summary>Test seam for resolving a Cobertura path to a test project name.</summary>
    /// <param name="coberturaPath">Cobertura file path.</param>
    /// <returns>Test project folder name when found.</returns>
    internal static string? ReadTestProjectNameForTests(string coberturaPath)
    {
        return _ReadTestProjectName(coberturaPath);
    }

    /// <summary>Test seam for walking from a known directory to a test project folder.</summary>
    /// <param name="directory">Starting directory.</param>
    /// <returns>Test project folder name when found.</returns>
    internal static string? ReadTestProjectDirectoryForTests(DirectoryInfo directory)
    {
        return _ReadTestProjectDirectory(directory);
    }

    #endregion

    #region Private helpers

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

    #endregion
}
