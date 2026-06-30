// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Scans project files for references and package markers.</summary>
internal static class ProjectReferenceScanner
{
    /// <summary>Finds project reference paths declared in a <c>.csproj</c> file.</summary>
    /// <param name="projectPath">Absolute project path.</param>
    /// <returns>Absolute paths to referenced projects.</returns>
    public static IReadOnlyList<string> ReadProjectReferences(string projectPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectPath);
        if (!ProjectXmlLoader.TryLoadDocument(projectPath, out XDocument? document, out string? _)
            || document is null)
        {
            return [];
        }

        string projectDirectory = Path.GetDirectoryName(projectPath) ?? ".";
        List<string> references = [];
        foreach (XElement referenceElement in document.Descendants("ProjectReference"))
        {
            XAttribute? includeAttribute = referenceElement.Attribute("Include");
            if (includeAttribute is null || string.IsNullOrWhiteSpace(includeAttribute.Value))
            {
                continue;
            }

            references.Add(Path.GetFullPath(includeAttribute.Value, projectDirectory));
        }

        return references;
    }

    /// <summary>Detects whether a test project references TUnit.</summary>
    /// <param name="testProjectPath">Absolute test project path.</param>
    /// <returns><see langword="true"/> when TUnit is referenced.</returns>
    public static bool ReferencesTUnit(string testProjectPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(testProjectPath);
        if (!ProjectXmlLoader.TryLoadDocument(testProjectPath, out XDocument? document, out string? _)
            || document is null)
        {
            return false;
        }

        return document.Descendants("PackageReference")
            .Any(element => string.Equals(element.Attribute("Include")?.Value, "TUnit", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Enumerates all test projects under a repository root.</summary>
    /// <param name="repositoryRoot">Repository root.</param>
    /// <returns>Absolute paths to <c>*.Tests.csproj</c> files.</returns>
    public static IReadOnlyList<string> FindTestProjects(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);
        if (!Directory.Exists(repositoryRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(repositoryRoot, "*.Tests.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
