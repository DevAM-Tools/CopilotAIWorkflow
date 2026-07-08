// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis;

/// <summary>Finds uncovered branch conditions in Cobertura documents. 
/// Thread-safe; all members are stateless.
/// </summary>
public static class BranchGapAnalyzer
{
    private const int _BranchPriority = 2;

    /// <summary>Finds branch gaps for packages matching each scoped document.</summary>
    /// <param name="scopedDocuments">Cobertura documents with package scope.</param>
    /// <param name="repositoryRoot">Repository root for path normalization.</param>
    /// <param name="includeSnippets">Whether to read source snippets.</param>
    /// <returns>Deduplicated branch gaps.</returns>
    public static IReadOnlyList<Models.BranchGap> FindUncoveredBranches(
        IReadOnlyList<Models.ScopedCoberturaDocument> scopedDocuments,
        string repositoryRoot,
        bool includeSnippets)
    {
        ArgumentNullException.ThrowIfNull(scopedDocuments);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        Dictionary<(string File, int Line, int ConditionIndex), Models.BranchGap> gaps = [];

        for (int scopedIndex = 0; scopedIndex < scopedDocuments.Count; scopedIndex++)
        {
            Models.ScopedCoberturaDocument scoped = scopedDocuments[scopedIndex];
            if (_GetMinimumScopedBranchRate(scoped.Document, scoped.Scope) >= 1d)
            {
                continue;
            }

            Models.CoberturaDocument document = scoped.Document;
            BranchGapScope scope = scoped.Scope;

            foreach (KeyValuePair<string, IReadOnlyDictionary<int, Models.CoberturaLineInfo>> fileEntry in document.Lines)
            {
                string relativeFile = PathNormalizer.ToRepositoryRelative(fileEntry.Key, repositoryRoot);
                string packageName = _ResolvePackageName(fileEntry.Key, document);

                if (!scope.IncludesPackage(packageName))
                {
                    continue;
                }

                foreach (KeyValuePair<int, Models.CoberturaLineInfo> lineEntry in fileEntry.Value)
                {
                    Models.CoberturaLineInfo lineInfo = lineEntry.Value;
                    if (!lineInfo.IsBranch || lineInfo.Conditions.Count == 0)
                    {
                        continue;
                    }

                    for (int conditionIndex = 0; conditionIndex < lineInfo.Conditions.Count; conditionIndex++)
                    {
                        double coverage = lineInfo.Conditions[conditionIndex];
                        if (coverage >= 1d)
                        {
                            continue;
                        }

                        (string File, int Line, int ConditionIndex) key = (relativeFile, lineEntry.Key, conditionIndex);
                        if (gaps.ContainsKey(key))
                        {
                            continue;
                        }

                        string? snippet = includeSnippets
                            ? PathNormalizer.TryReadSnippet(repositoryRoot, relativeFile, lineEntry.Key)
                            : null;

                        gaps[key] = new Models.BranchGap(
                            _BranchPriority,
                            packageName,
                            Path.GetFileNameWithoutExtension(fileEntry.Key),
                            relativeFile,
                            lineEntry.Key,
                            conditionIndex,
                            coverage,
                            lineInfo.Hits,
                            lineInfo.MethodName,
                            snippet);
                    }
                }
            }
        }

        return gaps.Values
            .OrderBy(static gap => gap.FilePath, StringComparer.Ordinal)
            .ThenBy(static gap => gap.Line)
            .ThenBy(static gap => gap.ConditionIndex)
            .ToList();
    }

    /// <summary>Returns the minimum scoped package branch rate across scoped documents.</summary>
    /// <param name="scopedDocuments">Scoped Cobertura documents.</param>
    /// <returns>Minimum branch rate in 0–1.</returns>
    public static double GetMinimumScopedBranchRate(IReadOnlyList<Models.ScopedCoberturaDocument> scopedDocuments)
    {
        ArgumentNullException.ThrowIfNull(scopedDocuments);

        double minimum = 1d;
        bool found = false;

        for (int scopedIndex = 0; scopedIndex < scopedDocuments.Count; scopedIndex++)
        {
            Models.ScopedCoberturaDocument scoped = scopedDocuments[scopedIndex];
            double documentMinimum = _GetMinimumScopedBranchRate(scoped.Document, scoped.Scope);
            found = true;
            if (documentMinimum < minimum)
            {
                minimum = documentMinimum;
            }
        }

        if (found)
        {
            return minimum;
        }

        return 1d;
    }

    private static double _GetMinimumScopedBranchRate(Models.CoberturaDocument document, BranchGapScope scope)
    {
        double minimum = 1d;
        bool found = false;

        foreach (KeyValuePair<string, double> packageEntry in document.PackageBranchRates)
        {
            if (!scope.IncludesPackage(packageEntry.Key))
            {
                continue;
            }

            found = true;
            if (packageEntry.Value < minimum)
            {
                minimum = packageEntry.Value;
            }
        }

        foreach (KeyValuePair<string, double> classEntry in document.ClassBranchRates)
        {
            string packageName = _ResolvePackageName(classEntry.Key, document);
            if (!scope.IncludesPackage(packageName))
            {
                continue;
            }

            found = true;
            if (classEntry.Value < minimum)
            {
                minimum = classEntry.Value;
            }
        }

        if (found)
        {
            return minimum;
        }

        return 0d;
    }

    private static bool _PathContainsPackageSegment(string filePath, string packageName)
    {
        string normalized = filePath.Replace('\\', '/');
        string segment = "/" + packageName + "/";
        if (normalized.Contains(segment, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalized.EndsWith("/" + packageName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalized.StartsWith(packageName + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string _ResolvePackageName(string filePath, Models.CoberturaDocument document)
    {
        string? bestMatch = null;
        foreach (KeyValuePair<string, double> packageEntry in document.PackageBranchRates)
        {
            if (!_PathContainsPackageSegment(filePath, packageEntry.Key))
            {
                continue;
            }

            if (bestMatch is null || packageEntry.Key.Length > bestMatch.Length)
            {
                bestMatch = packageEntry.Key;
            }
        }

        if (bestMatch is not null)
        {
            return bestMatch;
        }

        string fileName = Path.GetFileName(filePath);
        int dotIndex = fileName.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex > 0)
        {
            return fileName[..dotIndex];
        }

        return fileName;
    }
}
