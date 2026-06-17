// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis;

/// <summary>Compares exit points against Cobertura line hits. 
/// Thread-safe; all members are stateless.
/// </summary>
public static class ExitCoverageComparer
{
    private const int _ExitPriority = 1;

    /// <summary>Finds exit points without line coverage.</summary>
    /// <remarks>
    /// Exits on lines absent from Cobertura are skipped intentionally — compiler-elided code,
    /// mapping mismatches, or missing symbols produce false negatives unless line instrumentation exists.
    /// </remarks>
    /// <param name="exits">Collected exit points.</param>
    /// <param name="documents">Parsed Cobertura documents.</param>
    /// <param name="repositoryRoot">Repository root for path normalization.</param>
    /// <param name="includeSnippets">Whether to read source snippets.</param>
    /// <returns>Uncovered exit gaps.</returns>
    public static IReadOnlyList<Models.ExitCoverageGap> Compare(
        IReadOnlyList<ExitPoints.ExitPointEntry> exits,
        IReadOnlyList<Models.CoberturaDocument> documents,
        string repositoryRoot,
        bool includeSnippets)
    {
        ArgumentNullException.ThrowIfNull(exits);
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentException.ThrowIfNullOrEmpty(repositoryRoot);

        Dictionary<string, Dictionary<int, int>> mergedHits = _MergeLineHits(documents, repositoryRoot);
        List<Models.ExitCoverageGap> gaps = new List<Models.ExitCoverageGap>();

        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPoints.ExitPointEntry exit = exits[exitIndex];
            string relativeFile = PathNormalizer.ToRepositoryRelative(exit.FilePath, repositoryRoot);
            if (!_IsLineInstrumented(mergedHits, relativeFile, exit.FilePath, exit.Line))
            {
                continue;
            }

            int hits = _GetHits(mergedHits, relativeFile, exit.FilePath, exit.Line);
            if (hits > 0)
            {
                continue;
            }

            string? snippet = includeSnippets
                ? PathNormalizer.TryReadSnippet(repositoryRoot, relativeFile, exit.Line)
                : null;

            gaps.Add(new Models.ExitCoverageGap(
                _ExitPriority,
                exit.ExitPointId,
                relativeFile,
                exit.Line,
                exit.Column,
                exit.Kind.ToString(),
                exit.MethodDisplayName,
                hits,
                snippet));
        }

        return gaps
            .OrderBy(static gap => gap.FilePath, StringComparer.Ordinal)
            .ThenBy(static gap => gap.Line)
            .ThenBy(static gap => gap.Column)
            .ToList();
    }

    private static Dictionary<string, Dictionary<int, int>> _MergeLineHits(
        IReadOnlyList<Models.CoberturaDocument> documents,
        string repositoryRoot)
    {
        Dictionary<string, Dictionary<int, int>> merged =
            new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);

        for (int documentIndex = 0; documentIndex < documents.Count; documentIndex++)
        {
            Models.CoberturaDocument document = documents[documentIndex];
            foreach (KeyValuePair<string, IReadOnlyDictionary<int, Models.CoberturaLineInfo>> fileEntry in document.Lines)
            {
                string relativeFile = PathNormalizer.ToRepositoryRelative(fileEntry.Key, repositoryRoot);
                if (!merged.TryGetValue(relativeFile, out Dictionary<int, int>? lineHits))
                {
                    lineHits = new Dictionary<int, int>();
                    merged[relativeFile] = lineHits;
                }

                foreach (KeyValuePair<int, Models.CoberturaLineInfo> lineEntry in fileEntry.Value)
                {
                    if (!lineHits.TryGetValue(lineEntry.Key, out int existingHits) || lineEntry.Value.Hits > existingHits)
                    {
                        lineHits[lineEntry.Key] = lineEntry.Value.Hits;
                    }
                }

                string absoluteKey = fileEntry.Key.Replace('\\', '/');
                if (!merged.ContainsKey(absoluteKey))
                {
                    Dictionary<int, int> absoluteHits = new Dictionary<int, int>();
                    foreach (KeyValuePair<int, Models.CoberturaLineInfo> lineEntry in fileEntry.Value)
                    {
                        absoluteHits[lineEntry.Key] = lineEntry.Value.Hits;
                    }

                    merged[absoluteKey] = absoluteHits;
                }
            }
        }

        return merged;
    }

    private static int _GetHits(
        Dictionary<string, Dictionary<int, int>> mergedHits,
        string relativeFile,
        string originalFile,
        int line)
    {
        if (_TryGetLineHits(mergedHits, relativeFile, line, out int count))
        {
            return count;
        }

        string normalizedOriginal = originalFile.Replace('\\', '/');
        _TryGetLineHits(mergedHits, normalizedOriginal, line, out count);
        return count;
    }

    /// <remarks>Skips exits when the line is not present in any Cobertura document.</remarks>
    private static bool _IsLineInstrumented(
        Dictionary<string, Dictionary<int, int>> mergedHits,
        string relativeFile,
        string originalFile,
        int line)
    {
        if (_TryGetLineHits(mergedHits, relativeFile, line, out int _))
        {
            return true;
        }

        string normalizedOriginal = originalFile.Replace('\\', '/');
        return _TryGetLineHits(mergedHits, normalizedOriginal, line, out int _);
    }

    private static bool _TryGetLineHits(
        Dictionary<string, Dictionary<int, int>> mergedHits,
        string fileKey,
        int line,
        out int hits)
    {
        hits = 0;
        return mergedHits.TryGetValue(fileKey, out Dictionary<int, int>? lineHits)
            && lineHits.TryGetValue(line, out hits);
    }
}
