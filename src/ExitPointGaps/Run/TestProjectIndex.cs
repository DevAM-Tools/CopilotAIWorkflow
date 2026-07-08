// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>
/// Immutable index mapping production projects to paired test projects.
/// Built once per invocation to avoid repeated repository scans.
/// </summary>
internal sealed class TestProjectIndex
{
    private readonly Dictionary<string, string> _TestByProduction;

    private TestProjectIndex(Dictionary<string, string> testByProduction)
    {
        _TestByProduction = testByProduction;
    }

    /// <summary>Builds the index by scanning all test projects once.</summary>
    /// <param name="repositoryRoot">Repository root.</param>
    /// <returns>Immutable index for read-only lookups.</returns>
    public static TestProjectIndex Build(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);
        Dictionary<string, string> testByProduction = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> testProjects = ProjectReferenceScanner.FindTestProjects(repositoryRoot);

        for (int testIndex = 0; testIndex < testProjects.Count; testIndex++)
        {
            string testProjectPath = testProjects[testIndex];
            IReadOnlyList<string> references = ProjectReferenceScanner.ReadProjectReferences(testProjectPath);
            for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
            {
                string productionPath = Path.GetFullPath(references[referenceIndex]);
                if (!testByProduction.ContainsKey(productionPath))
                {
                    testByProduction[productionPath] = testProjectPath;
                }
            }
        }

        return new TestProjectIndex(testByProduction);
    }

    /// <summary>Returns the paired test project for a production project path.</summary>
    /// <param name="productionProjectPath">Absolute production project path.</param>
    /// <returns>Absolute test project path, or <see langword="null"/> when none is indexed.</returns>
    public string? TryGetTestProject(string productionProjectPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(productionProjectPath);
        string normalized = Path.GetFullPath(productionProjectPath);
        if (_TestByProduction.TryGetValue(normalized, out string? testProject))
        {
            return testProject;
        }

        return null;
    }
}
