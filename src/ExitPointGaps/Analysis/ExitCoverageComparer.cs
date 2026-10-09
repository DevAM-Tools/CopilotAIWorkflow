// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis;

/// <summary>Compares exit points against Cobertura line hits.
/// Thread-safe; all members are stateless.
/// </summary>
internal static class ExitCoverageComparer
{
    #region Fields

    private const int _ExitPriority = 1;

    #endregion

    #region Public API

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
        List<Models.ExitCoverageGap> gaps = [];
        Dictionary<string, string[]?>? snippetCache = includeSnippets
            ? new Dictionary<string, string[]?>(StringComparer.OrdinalIgnoreCase)
            : null;

        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPoints.ExitPointEntry exit = exits[exitIndex];
            string relativeFile = PathNormalizer.ToRepositoryRelative(exit.FilePath, repositoryRoot);
            if (!_TryGetLineHits(mergedHits, relativeFile, exit.Line, out int hits))
            {
                continue;
            }

            if (hits > 0)
            {
                continue;
            }

            string? snippet = null;
            if (snippetCache is not null)
            {
                snippet = PathNormalizer.TryReadSnippetCached(
                    repositoryRoot,
                    relativeFile,
                    exit.Line,
                    snippetCache);
            }

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

        gaps.Sort(static (left, right) =>
        {
            int fileCompare = StringComparer.Ordinal.Compare(left.FilePath, right.FilePath);
            if (fileCompare != 0)
            {
                return fileCompare;
            }

            int lineCompare = left.Line.CompareTo(right.Line);
            if (lineCompare != 0)
            {
                return lineCompare;
            }

            return left.Column.CompareTo(right.Column);
        });

        return gaps;
    }

    #endregion

    #region Private helpers

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
                    lineHits = [];
                    merged[relativeFile] = lineHits;
                }

                foreach (KeyValuePair<int, Models.CoberturaLineInfo> lineEntry in fileEntry.Value)
                {
                    if (!lineHits.TryGetValue(lineEntry.Key, out int existingHits) || lineEntry.Value.Hits > existingHits)
                    {
                        lineHits[lineEntry.Key] = lineEntry.Value.Hits;
                    }
                }
            }
        }

        return merged;
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

    #endregion
}
