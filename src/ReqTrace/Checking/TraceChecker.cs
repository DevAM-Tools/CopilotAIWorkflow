// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Everything one check found.</summary>
/// <param name="Snapshot">What was read.</param>
/// <param name="Accepted">The accepted state, or null when there is none.</param>
/// <param name="Findings">All findings, reading first.</param>
/// <param name="EvidenceChecked">Whether a result file was compared.</param>
internal sealed record TraceOutcome(
    TraceSnapshot Snapshot,
    Dictionary<string, LockEntry>? Accepted,
    List<Finding> Findings,
    bool EvidenceChecked);

/// <summary>Runs the checks in the order of the verification layers: form, links, change, evidence.</summary>
internal static class TraceChecker
{
    /// <summary>Checks a repository.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="resultsPath">Result file or folder; null to skip evidence.</param>
    /// <returns>The outcome.</returns>
    /// <exception cref="IOException">An input cannot be read.</exception>
    /// <exception cref="InvalidDataException">An input is malformed.</exception>
    public static TraceOutcome Run(string root, string? resultsPath)
    {
        TraceSnapshot snapshot = SnapshotReader.Read(root);
        Dictionary<string, LockEntry>? accepted = TraceLock.Read(root);
        List<Finding> findings = [.. snapshot.Findings];

        LinkChecker.Check(snapshot, accepted, findings);
        ChangeChecker.Check(snapshot, accepted, findings);
        if (resultsPath is not null)
        {
            EvidenceChecker.Check(root, snapshot, Path.GetFullPath(resultsPath, root), findings);
        }

        return new(snapshot, accepted, findings, resultsPath is not null);
    }
}
