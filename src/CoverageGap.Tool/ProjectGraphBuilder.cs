// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Builds the production project graph for a CLI invocation.</summary>
internal static class ProjectGraphBuilder
{
    /// <summary>Builds production project records for the current CLI options.</summary>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="projects">Resolved production projects.</param>
    /// <param name="error">Error message when graph construction fails.</param>
    /// <returns><see langword="true"/> when at least one production project was resolved.</returns>
    public static bool TryBuild(CliOptions options, out IReadOnlyList<ProductionProjectRecord> projects, out string? error)
    {
        ArgumentNullException.ThrowIfNull(options);
        projects = [];
        error = null;
        string repositoryRoot = Path.GetFullPath(options.RepositoryRoot);

        IReadOnlyList<string> candidatePaths = _ResolveCandidatePaths(options, repositoryRoot, out error);
        if (error is not null)
        {
            return false;
        }

        List<ProductionProjectRecord> records = new List<ProductionProjectRecord>();
        for (int candidateIndex = 0; candidateIndex < candidatePaths.Count; candidateIndex++)
        {
            string candidatePath = Path.GetFullPath(candidatePaths[candidateIndex]);
            if (!_IsProductionProject(candidatePath, repositoryRoot))
            {
                continue;
            }

            if (!File.Exists(candidatePath))
            {
                error = $"Project file not found: {candidatePath}";
                return false;
            }

            string name = Path.GetFileNameWithoutExtension(candidatePath);
            string? testProject = TestProjectPairer.FindTestProject(
                candidatePath,
                repositoryRoot,
                options.ProjectPaths.Count == 1 ? options.TestProjectOverride : null);

            if (testProject is null)
            {
                if (!options.SkipNoTests && options.IsRunCommand)
                {
                    error = $"No paired test project found for {candidatePath}. Use --skip-no-tests or --test-project.";
                    return false;
                }

                if (options.SkipNoTests && options.IsRunCommand)
                {
                    continue;
                }
            }

            records.Add(new ProductionProjectRecord(candidatePath, name, testProject));
        }

        if (records.Count == 0)
        {
            error = "No production projects found in scope.";
            if (_AllGateCandidatesAreExecutable(candidatePaths, repositoryRoot))
            {
                error += " Executable projects (OutputType Exe) are excluded from gate scope; target class-library production projects instead.";
            }

            return false;
        }

        projects = records;
        return true;
    }

    private static IReadOnlyList<string> _ResolveCandidatePaths(CliOptions options, string repositoryRoot, out string? error)
    {
        error = null;
        if (options.TargetKind == RunTargetKind.Projects)
        {
            return options.ProjectPaths
                .Select(path => Path.GetFullPath(path, repositoryRoot))
                .ToList();
        }

        if (!WorkspaceResolver.TryResolveSolutionPath(options, out string? solutionPath, out error))
        {
            return [];
        }

        if (!SolutionParser.TryReadProjectPaths(solutionPath!, repositoryRoot, out IReadOnlyList<string> paths, out error))
        {
            return [];
        }

        return paths;
    }

    private static bool _IsProductionProject(string projectPath, string repositoryRoot)
    {
        string fileName = Path.GetFileName(projectPath);
        if (fileName.EndsWith(".Tests.csproj", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string relative = Path.GetRelativePath(repositoryRoot, projectPath);
        if (relative.StartsWith($"samples{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith($"samples{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (ProjectFileReader.IsExecutableProject(projectPath))
        {
            return false;
        }

        return fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool _AllGateCandidatesAreExecutable(IReadOnlyList<string> candidatePaths, string repositoryRoot)
    {
        bool anyGateCandidate = false;
        for (int candidateIndex = 0; candidateIndex < candidatePaths.Count; candidateIndex++)
        {
            string candidatePath = Path.GetFullPath(candidatePaths[candidateIndex]);
            string fileName = Path.GetFileName(candidatePath);
            if (fileName.EndsWith(".Tests.csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relative = Path.GetRelativePath(repositoryRoot, candidatePath);
            if (relative.StartsWith($"samples{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith($"samples{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            anyGateCandidate = true;
            if (!ProjectFileReader.IsExecutableProject(candidatePath))
            {
                return false;
            }
        }

        return anyGateCandidate;
    }
}
