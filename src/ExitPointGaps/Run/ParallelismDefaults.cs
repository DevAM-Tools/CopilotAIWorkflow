// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Resolves effective parallelism for orchestrated runs.</summary>
internal static class ParallelismDefaults
{
    /// <summary>Returns the effective max parallelism for the current invocation.</summary>
    /// <param name="options">Parsed CLI options.</param>
    /// <param name="projectCount">Number of production projects in scope.</param>
    /// <returns>Parallelism value between 1 and <paramref name="projectCount"/>.</returns>
    public static int Resolve(CliOptions options, int projectCount)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (projectCount < 1)
        {
            return 1;
        }

        if (options.MaxParallelism is int explicitParallelism)
        {
            return Math.Min(explicitParallelism, projectCount);
        }

        return projectCount;
    }
}
