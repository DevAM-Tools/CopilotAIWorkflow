// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Parses CLI arguments into <see cref="CliOptions"/>.</summary>
[ExcludeFromCodeCoverage]
internal static class CliParser
{
    public static bool TryParse(string[] args, out CliOptions? options, out string? error)
    {
        options = new();
        error = null;

        int index = 0;
        if (args.Length > index && _IsCommandToken(args[index]))
        {
            options.Command = args[index];
            index++;
        }

        if (!_IsKnownCommand(options.Command, out error))
        {
            return false;
        }

        if (index >= args.Length)
        {
            options.TargetKind = RunTargetKind.SolutionAuto;
            return _TryParseTrailingFlags(args, index, options, out error);
        }

        if (args[index].StartsWith('-'))
        {
            options.TargetKind = RunTargetKind.SolutionAuto;
            return _TryParseTrailingFlags(args, index, options, out error);
        }

        if (string.Equals(args[index], CliConstants.SolutionTarget, StringComparison.OrdinalIgnoreCase))
        {
            index++;
            if (index < args.Length && !args[index].StartsWith('-'))
            {
                options.TargetKind = RunTargetKind.SolutionPath;
                options.SolutionPath = args[index];
                index++;
            }
            else
            {
                options.TargetKind = RunTargetKind.SolutionAuto;
            }

            return _TryParseTrailingFlags(args, index, options, out error);
        }

        if (string.Equals(args[index], CliConstants.ProjectTarget, StringComparison.OrdinalIgnoreCase))
        {
            index++;
            while (index < args.Length && !args[index].StartsWith('-'))
            {
                options.ProjectPaths.Add(args[index]);
                index++;
            }

            if (options.ProjectPaths.Count == 0)
            {
                error = "Expected at least one project path after 'project'.";
                return false;
            }

            options.TargetKind = RunTargetKind.Projects;
            return _TryParseTrailingFlags(args, index, options, out error);
        }

        error = $"Unknown target: {args[index]}. Use solution or project.";
        return false;
    }

    private static bool _IsCommandToken(string token)
    {
        return string.Equals(token, CliConstants.RunCommand, StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, CliConstants.PlanCommand, StringComparison.OrdinalIgnoreCase);
    }

    private static bool _IsKnownCommand(string command, out string? error)
    {
        error = null;
        if (string.Equals(command, CliConstants.RunCommand, StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, CliConstants.PlanCommand, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        error = $"Unknown command: {command}. Use run or plan.";
        return false;
    }

    private static bool _TryParseTrailingFlags(string[] args, int startIndex, CliOptions options, out string? error)
    {
        error = null;
        for (int index = startIndex; index < args.Length; index++)
        {
            if (!_TryParseFlag(args, ref index, options, out error))
            {
                return false;
            }
        }

        return true;
    }

    private static bool _TryParseFlag(string[] args, ref int index, CliOptions options, out string? error)
    {
        error = null;
        string token = args[index];
        switch (token)
        {
            case "--repo-root" when index + 1 < args.Length:
                options.RepositoryRoot = args[++index];
                return true;
            case "--configuration" when index + 1 < args.Length:
                options.Configuration = args[++index];
                return true;
            case "--format" when index + 1 < args.Length:
                options.Format = args[++index];
                return true;
            case "-o" when index + 1 < args.Length:
                options.OutputPath = args[++index];
                return true;
            case "--work-dir" when index + 1 < args.Length:
                options.WorkDirectory = args[++index];
                return true;
            case "--test-project" when index + 1 < args.Length:
                options.TestProjectOverride = args[++index];
                return true;
            case "--cobertura" when index + 1 < args.Length:
                options.CoberturaPaths.Add(args[++index]);
                return true;
            case "--include-snippet":
                options.IncludeSnippets = true;
                return true;
            case "--no-fail":
                options.NoFail = true;
                return true;
            case "--no-build":
                options.NoBuild = true;
                return true;
            case "--skip-no-tests":
                options.SkipNoTests = true;
                return true;
            case "--allow-empty-coverage":
                options.AllowEmptyCoverage = true;
                return true;
            case "--keep-work-dir":
                options.KeepWorkDir = true;
                return true;
            case "--max-parallelism" when index + 1 < args.Length:
                if (!int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parallelism)
                    || parallelism < 1)
                {
                    error = "--max-parallelism requires an integer >= 1.";
                    return false;
                }

                options.MaxParallelism = parallelism;
                index++;
                return true;
            case "--stream":
                options.Stream = true;
                return true;
            case "--no-stream":
                options.NoStream = true;
                return true;
            case "--help":
            case "-h":
                options.ShowHelp = true;
                return true;
            default:
                error = $"Unknown or incomplete option: {token}";
                return false;
        }
    }

    public static bool IsValidReportFormat(string format)
    {
        return string.Equals(format, "agent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "compact", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "text", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidPlanFormat(string format)
    {
        return string.Equals(format, "agent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "text", StringComparison.OrdinalIgnoreCase);
    }
}
