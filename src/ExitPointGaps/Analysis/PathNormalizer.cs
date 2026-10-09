// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis;

/// <summary>Normalizes file paths for cross-tool comparison.
/// Thread-safe; all members are stateless.
/// </summary>
internal static class PathNormalizer
{
    #region Public API

    /// <summary>Converts an absolute or repository-relative path to a repository-relative path with forward slashes.</summary>
    /// <param name="absolutePath">Absolute or relative input path (relative paths resolve against <paramref name="repositoryRoot"/>).</param>
    /// <param name="repositoryRoot">Repository root directory.</param>
    /// <returns>Normalized relative path.</returns>
    public static string ToRepositoryRelative(string absolutePath, string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(absolutePath);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        string fullRoot = _NormalizeRoot(repositoryRoot);
        string fullPath = Path.IsPathRooted(absolutePath)
            ? Path.GetFullPath(absolutePath)
            : Path.GetFullPath(absolutePath, fullRoot);

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            string relative = fullPath.Substring(fullRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace('\\', '/');
        }

        return fullPath.Replace('\\', '/');
    }

    /// <summary>Attempts to read a single source line for snippet output.</summary>
    /// <param name="repositoryRoot">Repository root.</param>
    /// <param name="relativeFilePath">Repository-relative file path.</param>
    /// <param name="line">One-based line number.</param>
    /// <returns>Trimmed line text or <see langword="null"/> when unavailable or outside the repository.</returns>
    public static string? TryReadSnippet(string repositoryRoot, string relativeFilePath, int line)
    {
        Dictionary<string, string[]?> emptyCache = new Dictionary<string, string[]?>(StringComparer.OrdinalIgnoreCase);
        return TryReadSnippetCached(repositoryRoot, relativeFilePath, line, emptyCache);
    }

    /// <summary>Reads a snippet using a per-pass line cache so each file is loaded at most once.</summary>
    /// <param name="repositoryRoot">Repository root.</param>
    /// <param name="relativeFilePath">Repository-relative file path.</param>
    /// <param name="line">One-based line number.</param>
    /// <param name="snippetLines">Cache of file path to lines (or <see langword="null"/> on prior failure).</param>
    /// <returns>Trimmed line text or <see langword="null"/> when unavailable or outside the repository.</returns>
    public static string? TryReadSnippetCached(
        string repositoryRoot,
        string relativeFilePath,
        int line,
        Dictionary<string, string[]?> snippetLines)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);
        ArgumentException.ThrowIfNullOrEmpty(relativeFilePath);
        ArgumentNullException.ThrowIfNull(snippetLines);

        if (line <= 0)
        {
            return null;
        }

        string? fullPath = _TryResolveContainedPath(repositoryRoot, relativeFilePath);
        if (fullPath is null)
        {
            return null;
        }

        if (!snippetLines.TryGetValue(fullPath, out string[]? lines))
        {
            lines = _TryLoadLines(fullPath);
            snippetLines[fullPath] = lines;
        }

        if (lines is null || line > lines.Length)
        {
            return null;
        }

        return lines[line - 1].Trim();
    }

    #endregion

    #region Private helpers

    private static string _NormalizeRoot(string repositoryRoot)
    {
        return Path.GetFullPath(repositoryRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string? _TryResolveContainedPath(string repositoryRoot, string relativeFilePath)
    {
        string fullRoot = _NormalizeRoot(repositoryRoot);
        string combined = Path.Combine(fullRoot, relativeFilePath.Replace('/', Path.DirectorySeparatorChar));
        string fullPath = Path.GetFullPath(combined);

        if (!fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    private static string[]? _TryLoadLines(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            return File.ReadAllLines(fullPath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    #endregion
}
