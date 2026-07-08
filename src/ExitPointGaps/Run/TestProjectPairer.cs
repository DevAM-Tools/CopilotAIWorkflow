// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Pairs production projects with test projects using conventions.</summary>
internal static class TestProjectPairer
{
    /// <summary>Finds the paired test project for a production project.</summary>
    /// <param name="productionProjectPath">Absolute production project path.</param>
    /// <param name="repositoryRoot">Repository root for reference scans.</param>
    /// <param name="overrideTestProjectPath">Optional explicit override.</param>
    /// <param name="index">Optional pre-built test project index.</param>
    /// <returns>Absolute test project path, or <see langword="null"/> when none is found.</returns>
    public static string? FindTestProject(
        string productionProjectPath,
        string repositoryRoot,
        string? overrideTestProjectPath,
        TestProjectIndex? index = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(productionProjectPath);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        if (!string.IsNullOrWhiteSpace(overrideTestProjectPath))
        {
            string overridePath = Path.GetFullPath(overrideTestProjectPath, repositoryRoot);
            if (File.Exists(overridePath))
            {
                return overridePath;
            }

            return null;
        }

        string? sibling = _TrySiblingConvention(productionProjectPath);
        if (!string.IsNullOrEmpty(sibling))
        {
            return sibling;
        }

        if (index is not null)
        {
            return index.TryGetTestProject(productionProjectPath);
        }

        return _TryReferenceScan(productionProjectPath, repositoryRoot);
    }

    private static string? _TrySiblingConvention(string productionProjectPath)
    {
        string productionDirectory = Path.GetDirectoryName(productionProjectPath) ?? ".";
        string productionName = Path.GetFileNameWithoutExtension(productionProjectPath);
        string parentDirectory = Path.GetDirectoryName(productionDirectory) ?? productionDirectory;

        string underParent = Path.Combine(parentDirectory, $"{productionName}.Tests", $"{productionName}.Tests.csproj");
        if (File.Exists(underParent))
        {
            return Path.GetFullPath(underParent);
        }

        string underSame = Path.Combine(productionDirectory, $"{productionName}.Tests", $"{productionName}.Tests.csproj");
        if (File.Exists(underSame))
        {
            return Path.GetFullPath(underSame);
        }

        return null;
    }

    private static string? _TryReferenceScan(string productionProjectPath, string repositoryRoot)
    {
        string normalizedProduction = Path.GetFullPath(productionProjectPath);
        IReadOnlyList<string> testProjects = ProjectReferenceScanner.FindTestProjects(repositoryRoot);
        for (int testIndex = 0; testIndex < testProjects.Count; testIndex++)
        {
            string testProjectPath = testProjects[testIndex];
            IReadOnlyList<string> references = ProjectReferenceScanner.ReadProjectReferences(testProjectPath);
            for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
            {
                if (string.Equals(Path.GetFullPath(references[referenceIndex]), normalizedProduction, StringComparison.OrdinalIgnoreCase))
                {
                    return testProjectPath;
                }
            }
        }

        return null;
    }
}
