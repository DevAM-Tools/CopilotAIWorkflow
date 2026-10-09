// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Filters exit points excluded by coverage attributes.</summary>
/// <remarks>Delegates to <see cref="ExitPointExclusion"/>; unit-tested in ExitPoints.Tests.</remarks>
[ExcludeFromCodeCoverage]
internal static class ExitPointFilter
{
    #region Public API

    public static IReadOnlyList<ExitPointEntry> RemoveExcluded(IReadOnlyList<ExitPointEntry> exits, Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(exits);
        ArgumentNullException.ThrowIfNull(compilation);

        Dictionary<string, SyntaxTree> treesByPath = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            if (!string.IsNullOrEmpty(tree.FilePath))
            {
                treesByPath[tree.FilePath] = tree;
            }
        }

        List<ExitPointEntry> filtered = new List<ExitPointEntry>(exits.Count);
        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPointEntry exit = exits[exitIndex];
            if (treesByPath.TryGetValue(exit.FilePath, out SyntaxTree? tree)
                && ExitPointExclusion.IsExcludedAtPosition(tree, exit.Line, exit.Column))
            {
                continue;
            }

            filtered.Add(exit);
        }

        return filtered;
    }

    #endregion
}
