// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis;

/// <summary>Normalizes file paths for cross-tool comparison.
/// Thread-safe; all members are stateless.
/// </summary>
public static class PathNormalizer
{
    /// <summary>Converts an absolute path to a repository-relative path with forward slashes.</summary>
    /// <param name="absolutePath">Absolute or relative input path.</param>
    /// <param name="repositoryRoot">Repository root directory.</param>
    /// <returns>Normalized relative path.</returns>
    public static string ToRepositoryRelative(string absolutePath, string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(absolutePath);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        string fullPath = Path.GetFullPath(absolutePath);
        string fullRoot = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            string relative = fullPath.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace('\\', '/');
        }

        return fullPath.Replace('\\', '/');
    }

    /// <summary>Attempts to read a single source line for snippet output.</summary>
    /// <param name="repositoryRoot">Repository root.</param>
    /// <param name="relativeFilePath">Repository-relative file path.</param>
    /// <param name="line">One-based line number.</param>
    /// <returns>Trimmed line text or <see langword="null"/> when unavailable.</returns>
    public static string? TryReadSnippet(string repositoryRoot, string relativeFilePath, int line)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);
        ArgumentException.ThrowIfNullOrEmpty(relativeFilePath);

        if (line <= 0)
        {
            return null;
        }

        string fullPath = Path.Combine(repositoryRoot, relativeFilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            int currentLine = 1;
            foreach (string candidate in File.ReadLines(fullPath))
            {
                if (currentLine == line)
                {
                    return candidate.Trim();
                }

                currentLine++;
            }

            return null;
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
}
