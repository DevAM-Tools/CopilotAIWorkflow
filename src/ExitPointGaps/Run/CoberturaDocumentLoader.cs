// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Loads Cobertura documents from explicit file paths.</summary>
internal static class CoberturaDocumentLoader
{
    /// <summary>Loads Cobertura documents and scoped views for one production project.</summary>
    /// <param name="coberturaPaths">Explicit Cobertura file paths.</param>
    /// <param name="productionProjectName">Production project name used for branch scope.</param>
    /// <param name="allowEmptyCoverage">Whether empty Cobertura is allowed.</param>
    /// <param name="allDocuments">Loaded documents.</param>
    /// <param name="scopedDocuments">Scoped documents for branch analysis.</param>
    /// <param name="error">Error message when loading fails.</param>
    /// <returns><see langword="true"/> when at least one document was loaded.</returns>
    public static bool TryLoad(
        IReadOnlyList<string> coberturaPaths,
        string productionProjectName,
        bool allowEmptyCoverage,
        out List<CoberturaDocument> allDocuments,
        out List<ScopedCoberturaDocument> scopedDocuments,
        out string? error)
    {
        allDocuments = [];
        scopedDocuments = [];
        error = null;

        if (coberturaPaths.Count == 0)
        {
            error = "No Cobertura files were provided.";
            return false;
        }

        BranchGapScope scope = new BranchGapScope([productionProjectName]);
        for (int pathIndex = 0; pathIndex < coberturaPaths.Count; pathIndex++)
        {
            string path = coberturaPaths[pathIndex];
            if (!CoberturaReader.TryReadScoped(path, scope, out CoberturaDocument? document, out string? readError))
            {
                if (allowEmptyCoverage && readError?.Contains("no coverage data", StringComparison.OrdinalIgnoreCase) == true)
                {
                    continue;
                }

                error = readError;
                return false;
            }

            if (document is null)
            {
                continue;
            }

            allDocuments.Add(document);
            scopedDocuments.Add(new ScopedCoberturaDocument(document, scope));
        }

        if (allDocuments.Count == 0)
        {
            error = allowEmptyCoverage
                ? "No usable Cobertura documents were loaded."
                : "Cobertura file contains no coverage data.";
            return false;
        }

        return true;
    }
}
