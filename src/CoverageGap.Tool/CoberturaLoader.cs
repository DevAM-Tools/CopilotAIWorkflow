// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Loads Cobertura documents for report commands.</summary>
/// <remarks>CLI wiring; behavior verified by <see cref="ReportCommand"/> integration tests and <see cref="CoberturaDiscovery"/> unit tests.</remarks>
[ExcludeFromCodeCoverage]
internal static class CoberturaLoader
{
    public static bool TryLoadScopedDocuments(
        string projectPath,
        CommandLineFlags flags,
        out List<CoberturaDocument> allDocuments,
        out List<ScopedCoberturaDocument> scopedDocuments,
        out string? error)
    {
        allDocuments = new List<CoberturaDocument>();
        scopedDocuments = new List<ScopedCoberturaDocument>();
        error = null;

        string targetPackage = Path.GetFileNameWithoutExtension(projectPath);

        if (flags.CoberturaPaths.Count > 0)
        {
            for (int pathIndex = 0; pathIndex < flags.CoberturaPaths.Count; pathIndex++)
            {
                string path = flags.CoberturaPaths[pathIndex];
                if (!CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? readError))
                {
                    error = readError;
                    return false;
                }

                if (document is null)
                {
                    continue;
                }

                allDocuments.Add(document);
                scopedDocuments.Add(new ScopedCoberturaDocument(document, _ResolveScope(flags, BranchGapScope.Default.PackageSuffixes)));
            }

            return allDocuments.Count > 0;
        }

        IReadOnlyList<string> searchRoots = flags.SearchRoots.Count == 0
            ? ["src"]
            : flags.SearchRoots;

        IReadOnlyDictionary<string, string> latest = CoberturaDiscovery.FindLatestForTargetPackage(
            searchRoots,
            CoberturaDiscovery.DefaultTestProjectPackages,
            targetPackage);

        if (latest.Count == 0)
        {
            error = "No Cobertura files found. Run 'dotnet test -- --coverage --coverage-output-format cobertura' first.";
            return false;
        }

        foreach (KeyValuePair<string, string> entry in latest)
        {
            if (!CoberturaDiscovery.DefaultTestProjectPackages.TryGetValue(entry.Key, out string[]? packageSuffixes))
            {
                continue;
            }

            if (!CoberturaReader.TryRead(entry.Value, out CoberturaDocument? document, out string? readError))
            {
                error = readError;
                return false;
            }

            if (document is null)
            {
                continue;
            }

            allDocuments.Add(document);
            scopedDocuments.Add(new ScopedCoberturaDocument(document, _ResolveScope(flags, packageSuffixes)));
        }

        return allDocuments.Count > 0;
    }

    private static BranchGapScope _ResolveScope(CommandLineFlags flags, IReadOnlyList<string> defaultPackageSuffixes)
    {
        return flags.ScopeSuffixes.Count == 0
            ? new BranchGapScope(defaultPackageSuffixes)
            : new BranchGapScope(flags.ScopeSuffixes);
    }
}

