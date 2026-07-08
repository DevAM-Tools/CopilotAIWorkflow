// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Serializes aggregated gate reports.</summary>
internal static class AggregatedGapReportSerializer
{
    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Serializes schema v3 summary with external per-project report files.</summary>
    /// <param name="accumulator">Thread-safe summary accumulator.</param>
    /// <param name="expectedProjectCount">Expected project count.</param>
    /// <returns>JSON text.</returns>
    public static string ToAgentJsonV3(RunSummaryAccumulator accumulator, int expectedProjectCount)
    {
        ArgumentNullException.ThrowIfNull(accumulator);

        AggregatedGapSummary summary = accumulator.ToSummary(expectedProjectCount);
        AggregatedGapReportV3Dto dto = new AggregatedGapReportV3Dto
        {
            SchemaVersion = 3,
            Summary = new AggregatedGapSummaryV3Dto
            {
                ProjectCount = summary.ProjectCount,
                CompletedProjectCount = summary.CompletedProjectCount,
                GatePassed = summary.GatePassed,
                ExitGapCount = summary.ExitGapCount,
                BranchGapCount = summary.BranchGapCount,
            },
            Projects = accumulator.GetEntries().Select(static entry => new AggregatedProjectV3Dto
            {
                Project = entry.ProjectPath,
                TestProject = entry.TestProjectPath,
                GatePassed = entry.GatePassed,
                ExitGapCount = entry.ExitGapCount,
                BranchGapCount = entry.BranchGapCount,
                TestExitCode = entry.TestExitCode,
                ReportFile = entry.ReportFile,
            }).ToList(),
        };

        return JsonSerializer.Serialize(dto, _JsonOptions);
    }

    /// <summary>Builds an NDJSON project completion line for stdout streaming.</summary>
    /// <param name="projectPath">Production project path.</param>
    /// <param name="projectPayload">Serialized per-project report payload.</param>
    /// <returns>Single NDJSON line.</returns>
    public static string ToStdoutProjectLine(string projectPath, string projectPayload)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectPath);
        ArgumentException.ThrowIfNullOrEmpty(projectPayload);

        using JsonDocument reportDocument = JsonDocument.Parse(projectPayload);
        StdoutProjectEventDto dto = new StdoutProjectEventDto
        {
            Event = "project",
            Project = projectPath,
            Report = reportDocument.RootElement.Clone(),
        };

        return JsonSerializer.Serialize(dto, _JsonOptions);
    }

    /// <summary>Builds the final NDJSON summary line for stdout streaming.</summary>
    /// <param name="accumulator">Summary accumulator.</param>
    /// <param name="expectedProjectCount">Expected project count.</param>
    /// <returns>Single NDJSON line.</returns>
    public static string ToStdoutSummaryLine(RunSummaryAccumulator accumulator, int expectedProjectCount)
    {
        ArgumentNullException.ThrowIfNull(accumulator);

        AggregatedGapSummary summary = accumulator.ToSummary(expectedProjectCount);
        StdoutSummaryEventDto dto = new StdoutSummaryEventDto
        {
            Event = "summary",
            SchemaVersion = 3,
            Summary = new AggregatedGapSummaryV3Dto
            {
                ProjectCount = summary.ProjectCount,
                CompletedProjectCount = summary.CompletedProjectCount,
                GatePassed = summary.GatePassed,
                ExitGapCount = summary.ExitGapCount,
                BranchGapCount = summary.BranchGapCount,
            },
        };

        return JsonSerializer.Serialize(dto, _JsonOptions);
    }

    private sealed class AggregatedGapReportV3Dto
    {
        public int SchemaVersion { get; init; }

        public AggregatedGapSummaryV3Dto Summary { get; init; } = new();

        public List<AggregatedProjectV3Dto> Projects { get; init; } = [];
    }

    private sealed class AggregatedGapSummaryV3Dto
    {
        public int ProjectCount { get; init; }

        public int CompletedProjectCount { get; init; }

        public bool GatePassed { get; init; }

        public int ExitGapCount { get; init; }

        public int BranchGapCount { get; init; }
    }

    private sealed class AggregatedProjectV3Dto
    {
        public string Project { get; init; } = string.Empty;

        public string? TestProject { get; init; }

        public bool GatePassed { get; init; }

        public int ExitGapCount { get; init; }

        public int BranchGapCount { get; init; }

        public int TestExitCode { get; init; }

        public string? ReportFile { get; init; }
    }

    private sealed class StdoutProjectEventDto
    {
        public string Event { get; init; } = string.Empty;

        public string Project { get; init; } = string.Empty;

        public JsonElement Report { get; init; }
    }

    private sealed class StdoutSummaryEventDto
    {
        public string Event { get; init; } = string.Empty;

        public int SchemaVersion { get; init; }

        public AggregatedGapSummaryV3Dto Summary { get; init; } = new();
    }
}
