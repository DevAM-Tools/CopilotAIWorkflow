// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Parsed CLI options shared by <c>run</c> and <c>plan</c>.</summary>
internal sealed class CliOptions
{
    public string Command { get; set; } = CliConstants.RunCommand;

    public RunTargetKind TargetKind { get; set; } = RunTargetKind.SolutionAuto;

    public List<string> ProjectPaths { get; } = [];

    public string? SolutionPath { get; set; }

    public string RepositoryRoot { get; set; } = Directory.GetCurrentDirectory();

    public string Configuration { get; set; } = CliConstants.DefaultConfiguration;

    public string Format { get; set; } = CliConstants.DefaultReportFormat;

    public string? OutputPath { get; set; }

    public string? WorkDirectory { get; set; }

    public string? TestProjectOverride { get; set; }

    public List<string> CoberturaPaths { get; } = [];

    public bool IncludeSnippets { get; set; }

    public bool NoFail { get; set; }

    public bool NoBuild { get; set; }

    public bool SkipNoTests { get; set; }

    public bool AllowEmptyCoverage { get; set; }

    public bool KeepWorkDir { get; set; }

    public bool IsRunCommand => string.Equals(Command, CliConstants.RunCommand, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Scope target for <c>run</c> and <c>plan</c>.</summary>
internal enum RunTargetKind
{
    SolutionAuto,

    SolutionPath,

    Projects,
}
