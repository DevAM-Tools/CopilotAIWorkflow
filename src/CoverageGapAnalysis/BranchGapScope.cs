// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis;

/// <summary>Filters Cobertura packages included in branch analysis.</summary>
public sealed class BranchGapScope
{
    /// <summary>Initializes scope with package suffix filters.</summary>
    /// <param name="packageSuffixes">Package name suffixes to include.</param>
    public BranchGapScope(IReadOnlyList<string> packageSuffixes)
    {
        ArgumentNullException.ThrowIfNull(packageSuffixes);
        if (packageSuffixes.Count == 0)
        {
            throw new ArgumentException("At least one package suffix is required.", nameof(packageSuffixes));
        }

        PackageSuffixes = packageSuffixes;
    }

    /// <summary>Package name suffixes to include.</summary>
    public IReadOnlyList<string> PackageSuffixes { get; }

    /// <summary>
    /// Default scope for this repository's production assemblies.
    /// Pass explicit suffixes via <see cref="BranchGapScope"/> in other solutions.
    /// </summary>
    public static BranchGapScope Default { get; } = new BranchGapScope(
        ["CSharpStyleValidator", "ExitPoints", "CoverageGapAnalysis"]);

    /// <summary>Returns whether a package name matches the scope.</summary>
    /// <param name="packageName">Cobertura package name.</param>
    /// <returns><see langword="true"/> when included.</returns>
    public bool IncludesPackage(string packageName)
    {
        ArgumentNullException.ThrowIfNull(packageName);

        for (int i = 0; i < PackageSuffixes.Count; i++)
        {
            string suffix = PackageSuffixes[i];
            if (packageName.Equals(suffix, StringComparison.Ordinal)
                || packageName.EndsWith("." + suffix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
