// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

internal static class ReportCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (!CommandLineOptions.TryParseProjectCommand(args, out string? projectPath, out CommandLineFlags flags, out string? parseError))
        {
            await Console.Error.WriteLineAsync(parseError).ConfigureAwait(false);
            return 1;
        }

        if (!CommandLineOptions.IsValidReportFormat(flags.Format))
        {
            await Console.Error.WriteLineAsync($"Unknown report format: {flags.Format}. Use agent, text, or compact.").ConfigureAwait(false);
            return 1;
        }

        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            await Console.Error.WriteLineAsync($"Project file not found: {projectPath}").ConfigureAwait(false);
            return 1;
        }

        if (!CoberturaLoader.TryLoadScopedDocuments(
                projectPath,
                flags,
                out List<CoberturaDocument> documents,
                out List<ScopedCoberturaDocument> scopedDocuments,
                out string? coberturaError))
        {
            await Console.Error.WriteLineAsync(coberturaError).ConfigureAwait(false);
            return 1;
        }

        (Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)? loaded =
            await ProjectExitLoader.TryLoadAsync(projectPath, flags, cancellationToken).ConfigureAwait(false);
        if (loaded is null)
        {
            return 1;
        }

        string repositoryRoot = Path.GetFullPath(flags.RepositoryRoot);

        CoverageGapReport report = CoverageGapReportBuilder.Build(
            loaded.Value.Exits,
            documents,
            scopedDocuments,
            repositoryRoot,
            flags.IncludeSnippets);

        string output = _FormatReport(report, flags.Format);
        await OutputWriter.WriteAsync(output, flags.OutputPath, appendNewLine: false, cancellationToken).ConfigureAwait(false);

        if (!flags.NoFail && !report.Summary.GatePassed)
        {
            return 1;
        }

        return 0;
    }

    private static string _FormatReport(CoverageGapReport report, string format)
    {
        if (format.Equals("compact", StringComparison.OrdinalIgnoreCase))
        {
            return CoverageGapReportFormatter.ToCompact(report);
        }

        if (format.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            return CoverageGapReportFormatter.ToText(report);
        }

        return CoverageGapReportFormatter.ToAgentJson(report);
    }
}

