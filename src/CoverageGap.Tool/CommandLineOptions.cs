// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Parses CLI flags for project commands.</summary>
/// <remarks>Flag parsing verified indirectly through CLI integration tests.</remarks>
[ExcludeFromCodeCoverage]
internal static class CommandLineOptions
{
    private static readonly HashSet<string> _ReportFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "agent",
        "text",
        "compact",
    };

    private static readonly HashSet<string> _ManifestFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "agent",
        "text",
    };

    public static bool TryParseProjectCommand(
        string[] args,
        out string? projectPath,
        out CommandLineFlags flags,
        out string? error)
    {
        projectPath = null;
        flags = new CommandLineFlags();
        error = null;

        if (args.Length < 2 || !string.Equals(args[0], "project", StringComparison.OrdinalIgnoreCase))
        {
            error = "Expected: <command> project <path.csproj> [options]";
            return false;
        }

        projectPath = args[1];
        for (int index = 2; index < args.Length; index++)
        {
            if (!_TryParseFlag(args, ref index, flags, out error))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsValidReportFormat(string format)
    {
        return _ReportFormats.Contains(format);
    }

    public static bool IsValidManifestFormat(string format)
    {
        return _ManifestFormats.Contains(format);
    }

    private static bool _TryParseFlag(string[] args, ref int index, CommandLineFlags flags, out string? error)
    {
        error = null;
        string token = args[index];
        switch (token)
        {
            case "--search-root" when index + 1 < args.Length:
                flags.SearchRoots.Add(args[++index]);
                return true;
            case "--cobertura" when index + 1 < args.Length:
                flags.CoberturaPaths.Add(args[++index]);
                return true;
            case "--repo-root" when index + 1 < args.Length:
                flags.RepositoryRoot = args[++index];
                return true;
            case "--scope" when index + 1 < args.Length:
                flags.ScopeSuffixes.Add(args[++index]);
                return true;
            case "--configuration" when index + 1 < args.Length:
                flags.Configuration = args[++index];
                return true;
            case "-o" when index + 1 < args.Length:
                flags.OutputPath = args[++index];
                return true;
            case "--format" when index + 1 < args.Length:
                flags.Format = args[++index];
                return true;
            case "--include-snippet":
                flags.IncludeSnippets = true;
                return true;
            case "--no-fail":
                flags.NoFail = true;
                return true;
            case "--no-build":
                flags.NoBuild = true;
                return true;
            default:
                error = $"Unknown or incomplete option: {token}";
                return false;
        }
    }
}

internal sealed class CommandLineFlags
{
    public List<string> SearchRoots { get; } = [];

    public List<string> CoberturaPaths { get; } = [];

    public string RepositoryRoot { get; set; } = Directory.GetCurrentDirectory();

    public List<string> ScopeSuffixes { get; } = [];

    public string Configuration { get; set; } = ProjectCompilationLoader.ResolveConfiguration(null);

    public string? OutputPath { get; set; }

    public string Format { get; set; } = "agent";

    public bool IncludeSnippets { get; set; }

    public bool NoFail { get; set; }

    public bool NoBuild { get; set; }
}

