// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis;

/// <summary>Discovers latest Cobertura files per test project.
/// Thread-safe; all members are stateless.
/// </summary>
public static class CoberturaDiscovery
{
    private const int _MaxParentHops = 32;

    /// <summary>
    /// Default test-project to package mapping for this repository.
    /// Pass an explicit mapping to <see cref="FindLatest"/> in other solutions.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> DefaultTestProjectPackages { get; } =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["CSharpStyleValidator.Tests"] = ["CSharpStyleValidator"],
            ["ExitPoints.Tests"] = ["ExitPoints"],
            ["CoverageGapAnalysis.Tests"] = ["CoverageGapAnalysis"],
            ["CoverageGap.Tool.Tests"] = ["CoverageGap.Tool"],
        };

    /// <summary>Finds the newest Cobertura file per known test project under search roots.</summary>
    /// <param name="searchRoots">Directories to search recursively.</param>
    /// <param name="testProjectPackages">Test project name to allowed package suffixes.</param>
    /// <returns>Test project name to Cobertura path.</returns>
    public static IReadOnlyDictionary<string, string> FindLatest(
        IEnumerable<string> searchRoots,
        IReadOnlyDictionary<string, string[]> testProjectPackages)
    {
        ArgumentNullException.ThrowIfNull(searchRoots);
        ArgumentNullException.ThrowIfNull(testProjectPackages);

        Dictionary<string, (DateTime Modified, string Path)> latest =
            new Dictionary<string, (DateTime, string)>(StringComparer.Ordinal);

        foreach (string root in searchRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string testResultsDir in Directory.EnumerateDirectories(root, "TestResults", SearchOption.AllDirectories))
            {
                foreach (string file in Directory.EnumerateFiles(testResultsDir, "*.cobertura.xml", SearchOption.TopDirectoryOnly))
                {
                    _TryRecordLatest(file, testProjectPackages, latest);
                }
            }
        }

        return latest.ToDictionary(static pair => pair.Key, static pair => pair.Value.Path, StringComparer.Ordinal);
    }

    /// <summary>Finds newest Cobertura files for test projects covering <paramref name="targetPackage"/>.</summary>
    /// <param name="searchRoots">Directories to search recursively.</param>
    /// <param name="testProjectPackages">Test project name to allowed package suffixes.</param>
    /// <param name="targetPackage">Production project or package name under report.</param>
    /// <returns>Matching test project name to Cobertura path.</returns>
    public static IReadOnlyDictionary<string, string> FindLatestForTargetPackage(
        IEnumerable<string> searchRoots,
        IReadOnlyDictionary<string, string[]> testProjectPackages,
        string targetPackage)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetPackage);

        IReadOnlyDictionary<string, string> allLatest = FindLatest(searchRoots, testProjectPackages);
        Dictionary<string, string> filtered = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> entry in allLatest)
        {
            if (!testProjectPackages.TryGetValue(entry.Key, out string[]? packageSuffixes))
            {
                continue;
            }

            for (int suffixIndex = 0; suffixIndex < packageSuffixes.Length; suffixIndex++)
            {
                if (string.Equals(packageSuffixes[suffixIndex], targetPackage, StringComparison.Ordinal))
                {
                    filtered[entry.Key] = entry.Value;
                    break;
                }
            }
        }

        return filtered;
    }

    private static void _TryRecordLatest(
        string file,
        IReadOnlyDictionary<string, string[]> testProjectPackages,
        Dictionary<string, (DateTime Modified, string Path)> latest)
    {
        string? projectKey = _ReadTestProjectName(file);
        if (projectKey is null || !testProjectPackages.ContainsKey(projectKey))
        {
            return;
        }

        DateTime modified = File.GetLastWriteTimeUtc(file);
        if (!latest.TryGetValue(projectKey, out (DateTime Modified, string Path) existing) || modified > existing.Modified)
        {
            latest[projectKey] = (modified, file);
        }
    }

    /// <summary>Collects unique Cobertura paths from the latest discovery result.</summary>
    /// <param name="searchRoots">Directories to search.</param>
    /// <param name="testProjectPackages">Mapping of test projects.</param>
    /// <returns>Distinct Cobertura file paths.</returns>
    public static IReadOnlyList<string> FindLatestFiles(
        IEnumerable<string> searchRoots,
        IReadOnlyDictionary<string, string[]> testProjectPackages)
    {
        return FindLatest(searchRoots, testProjectPackages)
            .Values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Test seam for resolving a Cobertura path to a test project name.</summary>
    /// <param name="coberturaPath">Cobertura file path.</param>
    /// <returns>Test project folder name when found.</returns>
    internal static string? ReadTestProjectNameForTests(string coberturaPath)
    {
        return _ReadTestProjectName(coberturaPath);
    }

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
