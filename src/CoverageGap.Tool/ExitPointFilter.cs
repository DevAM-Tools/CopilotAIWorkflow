// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Filters exit points excluded by coverage attributes.</summary>
/// <remarks>Delegates to <see cref="ExitPointExclusion"/>; unit-tested in ExitPoints.Tests.</remarks>
[ExcludeFromCodeCoverage]
internal static class ExitPointFilter
{
    public static IReadOnlyList<ExitPointEntry> RemoveExcluded(IReadOnlyList<ExitPointEntry> exits, Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(exits);
        ArgumentNullException.ThrowIfNull(compilation);

        List<ExitPointEntry> filtered = new List<ExitPointEntry>(exits.Count);
        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPointEntry exit = exits[exitIndex];
            if (_IsExcluded(exit, compilation))
            {
                continue;
            }

            filtered.Add(exit);
        }

        return filtered;
    }

    /// <remarks>When no syntax tree matches the exit file path, the exit is not filtered out.</remarks>
    private static bool _IsExcluded(ExitPointEntry exit, Compilation compilation)
    {
        SyntaxTree? tree = compilation.SyntaxTrees.FirstOrDefault(
            candidate => string.Equals(candidate.FilePath, exit.FilePath, StringComparison.OrdinalIgnoreCase));

        if (tree is null)
        {
            return false;
        }

        return ExitPointExclusion.IsExcludedAtPosition(tree, exit.Line, exit.Column);
    }
}

