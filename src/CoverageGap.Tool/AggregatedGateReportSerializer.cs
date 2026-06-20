// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Serializes aggregated gate reports.</summary>
internal static class AggregatedGateReportSerializer
{
    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string ToAgentJson(AggregatedGateReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        AggregatedGateReportDto dto = new AggregatedGateReportDto
        {
            SchemaVersion = 2,
            Summary = new AggregatedGateSummaryDto
            {
                ProjectCount = report.Summary.ProjectCount,
                GatePassed = report.Summary.GatePassed,
                ExitGapCount = report.Summary.ExitGapCount,
                BranchGapCount = report.Summary.BranchGapCount,
            },
            Projects = report.Projects.Select(static project => new AggregatedProjectDto
            {
                Project = project.ProjectPath,
                TestProject = project.TestProjectPath,
                GatePassed = project.GatePassed,
                ExitGapCount = project.Report.Summary.ExitGapCount,
                BranchGapCount = project.Report.Summary.BranchGapCount,
                Report = JsonSerializer.Deserialize<JsonElement>(GateReportFormatter.Format(project.Report, "agent")),
            }).ToList(),
        };

        return JsonSerializer.Serialize(dto, _JsonOptions);
    }

    private sealed class AggregatedGateReportDto
    {
        public int SchemaVersion { get; init; }

        public AggregatedGateSummaryDto Summary { get; init; } = new();

        public List<AggregatedProjectDto> Projects { get; init; } = [];
    }

    private sealed class AggregatedGateSummaryDto
    {
        public int ProjectCount { get; init; }

        public bool GatePassed { get; init; }

        public int ExitGapCount { get; init; }

        public int BranchGapCount { get; init; }
    }

    private sealed class AggregatedProjectDto
    {
        public string Project { get; init; } = string.Empty;

        public string? TestProject { get; init; }

        public bool GatePassed { get; init; }

        public int ExitGapCount { get; init; }

        public int BranchGapCount { get; init; }

        public JsonElement Report { get; init; }
    }
}
