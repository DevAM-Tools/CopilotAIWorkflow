// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>CLI contract constants.</summary>
internal static class CliConstants
{
    public const string RunCommand = "run";

    public const string PlanCommand = "plan";

    public const string SolutionTarget = "solution";

    public const string ProjectTarget = "project";

    public const string DefaultConfiguration = "Release";

    public const string DefaultReportFormat = "agent";

    public const string WorkDirFolderName = "coveragegap";

    public const string SummaryFileName = "summary.json";

    public const int ExitSuccess = 0;

    public const int ExitGateOrToolFailure = 1;

    public const int ExitUsageError = 2;
}
