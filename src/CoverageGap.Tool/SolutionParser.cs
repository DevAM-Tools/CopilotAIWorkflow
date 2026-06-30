// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Reads project paths from solution files.</summary>
internal static class SolutionParser
{
    /// <summary>Reads project paths from a <c>.slnx</c> or <c>.sln</c> file.</summary>
    /// <param name="solutionPath">Absolute solution path.</param>
    /// <param name="repositoryRoot">Repository root for resolving relative paths.</param>
    /// <param name="paths">Absolute project paths listed in the solution.</param>
    /// <param name="error">Error message when parsing fails.</param>
    /// <returns><see langword="true"/> when project paths were read.</returns>
    public static bool TryReadProjectPaths(
        string solutionPath,
        string repositoryRoot,
        out IReadOnlyList<string> paths,
        out string? error)
    {
        ArgumentException.ThrowIfNullOrEmpty(solutionPath);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        if (solutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            return _TryReadSlnxProjectPaths(solutionPath, repositoryRoot, out paths, out error);
        }

        return _TryReadSlnProjectPaths(solutionPath, repositoryRoot, out paths, out error);
    }

    private static bool _TryReadSlnxProjectPaths(
        string solutionPath,
        string repositoryRoot,
        out IReadOnlyList<string> paths,
        out string? error)
    {
        paths = [];
        if (!ProjectXmlLoader.TryLoadDocument(solutionPath, out XDocument? document, out error))
        {
            return false;
        }

        List<string> projectPaths = [];
        foreach (XElement projectElement in document!.Descendants("Project"))
        {
            XAttribute? pathAttribute = projectElement.Attribute("Path");
            if (pathAttribute is null || string.IsNullOrWhiteSpace(pathAttribute.Value))
            {
                continue;
            }

            projectPaths.Add(Path.GetFullPath(pathAttribute.Value, repositoryRoot));
        }

        paths = projectPaths;
        return true;
    }

    private static bool _TryReadSlnProjectPaths(
        string solutionPath,
        string repositoryRoot,
        out IReadOnlyList<string> paths,
        out string? error)
    {
        paths = [];
        error = null;
        string solutionDirectory = Path.GetDirectoryName(solutionPath) ?? repositoryRoot;
        List<string> projectPaths = [];

        try
        {
            foreach (string line in File.ReadLines(solutionPath))
            {
                if (!line.StartsWith("Project(", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Split('"');
                if (parts.Length < 4)
                {
                    continue;
                }

                string relativePath = parts[3];
                if (!relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                projectPaths.Add(Path.GetFullPath(relativePath, solutionDirectory));
            }
        }
        catch (IOException ioException)
        {
            error = $"Failed to read solution file: {ioException.Message}";
            return false;
        }

        paths = projectPaths;
        return true;
    }
}
