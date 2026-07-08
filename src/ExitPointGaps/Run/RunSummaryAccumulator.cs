// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Thread-safe rolling summary for multi-project runs.</summary>
internal sealed class RunSummaryAccumulator
{
    private readonly object _EntriesLock = new();
    private readonly List<ProjectSummaryEntry> _Entries = [];
    private int _ExitGapCount;
    private int _BranchGapCount;
    private int _CompletedProjectCount;
    private int _HasFailure;

    /// <summary>Records a completed project summary entry.</summary>
    /// <param name="entry">Per-project summary.</param>
    public void AddProject(ProjectSummaryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_EntriesLock)
        {
            _Entries.Add(entry);
        }

        Interlocked.Add(ref _ExitGapCount, entry.ExitGapCount);
        Interlocked.Add(ref _BranchGapCount, entry.BranchGapCount);
        Interlocked.Increment(ref _CompletedProjectCount);

        if (!entry.GatePassed || entry.ToolFailed)
        {
            Interlocked.Exchange(ref _HasFailure, 1);
        }
    }

    /// <summary>Returns ordered project entries.</summary>
    /// <returns>Snapshot of recorded entries.</returns>
    public IReadOnlyList<ProjectSummaryEntry> GetEntries()
    {
        lock (_EntriesLock)
        {
            return _Entries.ToList();
        }
    }

    /// <summary>Builds the aggregated summary.</summary>
    /// <param name="expectedProjectCount">Total projects in scope.</param>
    /// <returns>Aggregated summary metrics.</returns>
    public AggregatedGapSummary ToSummary(int expectedProjectCount)
    {
        bool gatePassed = Volatile.Read(ref _HasFailure) == 0
            && Volatile.Read(ref _CompletedProjectCount) == expectedProjectCount;

        return new AggregatedGapSummary
        {
            ProjectCount = expectedProjectCount,
            CompletedProjectCount = Volatile.Read(ref _CompletedProjectCount),
            GatePassed = gatePassed,
            ExitGapCount = Volatile.Read(ref _ExitGapCount),
            BranchGapCount = Volatile.Read(ref _BranchGapCount),
        };
    }

    /// <summary>Whether any project failed tool execution.</summary>
    public bool HasToolFailure => Volatile.Read(ref _HasFailure) == 1;
}

/// <summary>Per-project summary metadata for streaming output.</summary>
/// <param name="ProjectPath">Production project path.</param>
/// <param name="TestProjectPath">Paired test project path, if any.</param>
/// <param name="GatePassed">Whether the project gate passed.</param>
/// <param name="ExitGapCount">Exit gap count.</param>
/// <param name="BranchGapCount">Branch gap count.</param>
/// <param name="TestExitCode">Test process exit code.</param>
/// <param name="ReportFile">Relative report file name when written to a directory.</param>
/// <param name="ToolFailed">Whether tool execution failed for this project.</param>
internal sealed record ProjectSummaryEntry(
    string ProjectPath,
    string? TestProjectPath,
    bool GatePassed,
    int ExitGapCount,
    int BranchGapCount,
    int TestExitCode,
    string? ReportFile,
    bool ToolFailed);

/// <summary>Outcome of one project task for the parallel coordinator.</summary>
/// <param name="Entry">Summary entry when successful.</param>
/// <param name="Error">Error message when the project task failed.</param>
internal sealed record ProjectRunOutcome(ProjectSummaryEntry? Entry, string? Error);
