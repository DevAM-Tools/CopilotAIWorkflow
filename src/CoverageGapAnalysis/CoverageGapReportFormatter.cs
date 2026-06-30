// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis;

/// <summary>Formats coverage gap reports for agents and humans. 
/// Thread-safe; all members are stateless.
/// </summary>
public static class CoverageGapReportFormatter
{
    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Serializes a report as agent JSON.</summary>
    /// <param name="report">Coverage gap report.</param>
    /// <returns>JSON text.</returns>
    public static string ToAgentJson(Models.CoverageGapReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        AgentReportDto dto = new AgentReportDto
        {
            SchemaVersion = 1,
            Summary = new AgentSummaryDto
            {
                BranchRate = report.Summary.BranchRate,
                ExitGapCount = report.Summary.ExitGapCount,
                BranchGapCount = report.Summary.BranchGapCount,
                TotalGapCount = report.Summary.TotalGapCount,
                GatePassed = report.Summary.GatePassed,
                BranchGatePassed = report.Summary.BranchGatePassed,
            },
            ExitGaps = report.ExitGaps.Select(static gap => new AgentExitGapDto
            {
                Priority = gap.Priority,
                ExitPointId = gap.ExitPointId,
                File = gap.FilePath,
                Line = gap.Line,
                Column = gap.Column,
                Kind = gap.Kind,
                MethodDisplayName = gap.MethodDisplayName,
                Hits = gap.Hits,
                Snippet = gap.Snippet,
            }).ToList(),
            BranchGaps = report.BranchGaps.Select(static gap => new AgentBranchGapDto
            {
                Priority = gap.Priority,
                File = gap.FilePath,
                Line = gap.Line,
                ConditionIndex = gap.ConditionIndex,
                ConditionCoverage = gap.ConditionCoverage,
                Method = gap.Method,
                Snippet = gap.Snippet,
            }).ToList(),
        };

        return JsonSerializer.Serialize(dto, _JsonOptions);
    }

    /// <summary>Serializes a report as compact lines with exit gaps first.</summary>
    /// <param name="report">Coverage gap report.</param>
    /// <returns>Compact text.</returns>
    public static string ToCompact(Models.CoverageGapReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder builder = new();
        for (int exitIndex = 0; exitIndex < report.ExitGaps.Count; exitIndex++)
        {
            Models.ExitCoverageGap gap = report.ExitGaps[exitIndex];
            builder.Append("exit:")
                .Append(gap.FilePath)
                .Append(':')
                .Append(gap.Line)
                .Append(":col=")
                .Append(gap.Column)
                .Append(":kind=")
                .Append(gap.Kind)
                .Append(":method=")
                .Append(gap.MethodDisplayName)
                .Append(":id=")
                .Append(gap.ExitPointId)
                .AppendLine();
        }

        for (int branchIndex = 0; branchIndex < report.BranchGaps.Count; branchIndex++)
        {
            Models.BranchGap gap = report.BranchGaps[branchIndex];
            int coveragePercent = checked((int)Math.Round(gap.ConditionCoverage * 100d, MidpointRounding.AwayFromZero));
            builder.Append("branch:")
                .Append(gap.FilePath)
                .Append(':')
                .Append(gap.Line)
                .Append(":cond=")
                .Append(gap.ConditionIndex)
                .Append(":cov=")
                .Append(coveragePercent)
                .Append('%');
            if (!string.IsNullOrEmpty(gap.Method))
            {
                builder.Append(":method=").Append(gap.Method);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Serializes a report as human-readable text.</summary>
    /// <param name="report">Coverage gap report.</param>
    /// <returns>Text output.</returns>
    public static string ToText(Models.CoverageGapReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder builder = new();
        builder.Append("Branch rate: ")
            .Append(report.Summary.BranchRate.ToString("P1", CultureInfo.InvariantCulture))
            .Append(", exit gaps: ")
            .Append(report.Summary.ExitGapCount)
            .Append(", branch gaps: ")
            .Append(report.Summary.BranchGapCount)
            .Append(", exit gate: ")
            .Append(report.Summary.GatePassed ? "passed" : "failed")
            .Append(", branch gate: ")
            .Append(report.Summary.BranchGatePassed ? "passed" : "failed")
            .AppendLine();

        if (report.ExitGaps.Count > 0)
        {
            builder.AppendLine("Exit gaps:");
            for (int exitIndex = 0; exitIndex < report.ExitGaps.Count; exitIndex++)
            {
                Models.ExitCoverageGap gap = report.ExitGaps[exitIndex];
                builder.Append("  - ")
                    .Append(gap.FilePath)
                    .Append(':')
                    .Append(gap.Line)
                    .Append(" [")
                    .Append(gap.Kind)
                    .Append("] ")
                    .Append(gap.MethodDisplayName)
                    .AppendLine();
            }
        }

        if (report.BranchGaps.Count > 0)
        {
            builder.AppendLine("Branch gaps:");
            for (int branchIndex = 0; branchIndex < report.BranchGaps.Count; branchIndex++)
            {
                Models.BranchGap gap = report.BranchGaps[branchIndex];
                builder.Append("  - ")
                    .Append(gap.FilePath)
                    .Append(':')
                    .Append(gap.Line)
                    .Append(" cond=")
                    .Append(gap.ConditionIndex)
                    .Append(" cov=")
                    .Append(gap.ConditionCoverage.ToString("P0", CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(gap.Method))
                {
                    builder.Append(' ').Append(gap.Method);
                }

                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private sealed class AgentReportDto
    {
        public int SchemaVersion { get; init; }

        public AgentSummaryDto Summary { get; init; } = new();

        public List<AgentExitGapDto> ExitGaps { get; init; } = [];

        public List<AgentBranchGapDto> BranchGaps { get; init; } = [];
    }

    private sealed class AgentSummaryDto
    {
        public double BranchRate { get; init; }

        public int ExitGapCount { get; init; }

        public int BranchGapCount { get; init; }

        public int TotalGapCount { get; init; }

        public bool GatePassed { get; init; }

        public bool BranchGatePassed { get; init; }
    }

    private sealed class AgentExitGapDto
    {
        public int Priority { get; init; }

        public string ExitPointId { get; init; } = string.Empty;

        public string File { get; init; } = string.Empty;

        public int Line { get; init; }

        public int Column { get; init; }

        public string Kind { get; init; } = string.Empty;

        public string MethodDisplayName { get; init; } = string.Empty;

        public int Hits { get; init; }

        public string? Snippet { get; init; }
    }

    private sealed class AgentBranchGapDto
    {
        public int Priority { get; init; }

        public string File { get; init; } = string.Empty;

        public int Line { get; init; }

        public int ConditionIndex { get; init; }

        public double ConditionCoverage { get; init; }

        public string? Method { get; init; }

        public string? Snippet { get; init; }
    }
}
