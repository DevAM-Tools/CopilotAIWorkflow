// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Prints the report at a gate and one entry with its links, generated from the files.</summary>
/// <remarks>Sync: docs/agents.md § Commands; .github/skills/tech-reporting.md § Spec delta.</remarks>
internal static class ReportWriter
{
    #region Report

    /// <summary>Writes the report.</summary>
    /// <param name="output">Receives the text.</param>
    /// <param name="outcome">The check outcome.</param>
    public static void WriteReport(TextWriter output, TraceOutcome outcome)
    {
        TraceSnapshot snapshot = outcome.Snapshot;
        IReadOnlyDictionary<string, LockEntry> accepted = outcome.Accepted ?? new Dictionary<string, LockEntry>();

        List<string> changed = [];
        List<string> weakened = [];
        _CollectChanges(snapshot, accepted, changed, weakened);

        _Section(output, "Changed", changed);
        _Section(output, "Possibly weakened", weakened);
        _Section(output, "Vanished", _WithRule(outcome.Findings, Rules.Vanished));
        _Section(output, "Retired", _NewlyRetired(snapshot, accepted));
        _Section(output, "Not confirmed yet", _WithRule(outcome.Findings, Rules.Unconfirmed));
        _Section(output, "Confirmations", _Notes(accepted));
        _Section(output, "Accepted gaps", _AcceptedGaps(accepted));
        _Section(output, "Open gaps", _OpenGaps(outcome.Findings));
        _Section(output, "Proof other than test", _ProofOtherThanTest(snapshot));
        _Section(output, "Ignored paths", _Ignored(snapshot));

        output.WriteLine(Invariant($"Counts: {snapshot.Requirements.Count} requirements · {snapshot.Cards.Count} test cards · {snapshot.Markers.Count} markers"));
    }

    /// <summary>Finds requirements whose text differs from the accepted one.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <param name="accepted">The accepted state.</param>
    /// <param name="changed">Receives one line per change.</param>
    /// <param name="weakened">Receives the IDs that may be weakened.</param>
    private static void _CollectChanges(
        TraceSnapshot snapshot,
        IReadOnlyDictionary<string, LockEntry> accepted,
        List<string> changed,
        List<string> weakened)
    {
        foreach (Requirement requirement in snapshot.Requirements)
        {
            string text = SpecText.Obligation(requirement);
            if (!accepted.TryGetValue(requirement.Id, out LockEntry? entry)
                || string.Equals(entry.Text, text, StringComparison.Ordinal))
            {
                continue;
            }

            List<string> removed = [];
            List<string> added = [];
            WordDiff.Compare(entry.Text, text, removed, added);
            changed.Add(Invariant($"{requirement.Id}: removed \"{string.Join(' ', removed)}\", added \"{string.Join(' ', added)}\""));
            if (WordDiff.IsPossiblyWeakened(removed, added))
            {
                weakened.Add(requirement.Id);
            }
        }
    }

    /// <summary>Lists findings of one rule.</summary>
    /// <param name="findings">The findings.</param>
    /// <param name="rule">The rule.</param>
    /// <returns>One line each.</returns>
    private static List<string> _WithRule(List<Finding> findings, string rule)
    {
        List<string> lines = [];
        foreach (Finding finding in findings)
        {
            if (string.Equals(finding.Rule, rule, StringComparison.Ordinal))
            {
                lines.Add(finding.Message);
            }
        }

        return lines;
    }

    /// <summary>Lists entries retired since acceptance.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <param name="accepted">The accepted state.</param>
    /// <returns>Their IDs.</returns>
    private static List<string> _NewlyRetired(TraceSnapshot snapshot, IReadOnlyDictionary<string, LockEntry> accepted)
    {
        List<string> lines = [];
        foreach (Requirement requirement in snapshot.Requirements)
        {
            if (requirement.State == EntryState.Retired
                && accepted.TryGetValue(requirement.Id, out LockEntry? entry)
                && !string.Equals(entry.State, "retired", StringComparison.Ordinal))
            {
                lines.Add(requirement.Id);
            }
        }

        return lines;
    }

    /// <summary>Lists confirmation notes.</summary>
    /// <param name="accepted">The accepted state.</param>
    /// <returns>One line per note.</returns>
    private static List<string> _Notes(IReadOnlyDictionary<string, LockEntry> accepted)
    {
        List<string> lines = [];
        foreach (LockEntry entry in accepted.Values)
        {
            if (entry.Note.Length > 0)
            {
                lines.Add(Invariant($"{entry.Id}: {entry.Note}"));
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    /// <summary>Lists gaps accepted with a reason.</summary>
    /// <param name="accepted">The accepted state.</param>
    /// <returns>One line per gap.</returns>
    private static List<string> _AcceptedGaps(IReadOnlyDictionary<string, LockEntry> accepted)
    {
        List<string> lines = [];
        foreach (LockEntry entry in accepted.Values)
        {
            if (entry.GapReason.Length > 0)
            {
                lines.Add(Invariant($"{entry.Id}: {entry.GapReason}"));
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    /// <summary>Lists gaps that do not block.</summary>
    /// <param name="findings">The findings.</param>
    /// <returns>One line each.</returns>
    private static List<string> _OpenGaps(List<Finding> findings)
    {
        List<string> lines = [];
        foreach (Finding finding in findings)
        {
            if (finding.Severity == Severity.OpenGap)
            {
                lines.Add(finding.Message);
            }
        }

        return lines;
    }

    /// <summary>Lists requirements not proven by a test.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <returns>One line each.</returns>
    private static List<string> _ProofOtherThanTest(TraceSnapshot snapshot)
    {
        List<string> lines = [];
        foreach (Requirement requirement in snapshot.Requirements)
        {
            if (requirement.State != EntryState.Retired && requirement.Proof != ProofKind.Test)
            {
                lines.Add(Invariant($"{requirement.Id}: {requirement.ProofText}"));
            }
        }

        return lines;
    }

    /// <summary>Lists ignored paths.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <returns>One line each.</returns>
    private static List<string> _Ignored(TraceSnapshot snapshot)
    {
        List<string> lines = [];
        foreach (KeyValuePair<string, string> entry in snapshot.IgnoredPaths)
        {
            lines.Add(Invariant($"{entry.Key}: {entry.Value}"));
        }

        return lines;
    }

    /// <summary>Writes one report section.</summary>
    /// <param name="output">Receives the text.</param>
    /// <param name="title">The title.</param>
    /// <param name="lines">Its items; empty prints <c>none</c>.</param>
    private static void _Section(TextWriter output, string title, List<string> lines)
    {
        output.WriteLine(Invariant($"{title}:"));
        if (lines.Count == 0)
        {
            output.WriteLine("  none");
        }

        foreach (string line in lines)
        {
            output.WriteLine(Invariant($"  {line}"));
        }
    }

    #endregion

    #region Show

    /// <summary>Writes one entry and its links.</summary>
    /// <param name="output">Receives the text.</param>
    /// <param name="snapshot">What was read.</param>
    /// <param name="id">The ID.</param>
    /// <returns>Whether the ID exists.</returns>
    public static bool WriteShow(TextWriter output, TraceSnapshot snapshot, string id)
    {
        foreach (Requirement requirement in snapshot.Requirements)
        {
            if (string.Equals(requirement.Id, id, StringComparison.Ordinal))
            {
                _ShowRequirement(output, snapshot, requirement);
                return true;
            }
        }

        foreach (TestCard card in snapshot.Cards)
        {
            if (string.Equals(card.Id, id, StringComparison.Ordinal))
            {
                _ShowCard(output, snapshot, card);
                return true;
            }
        }

        return false;
    }

    /// <summary>Writes a requirement, its cards, and their tests.</summary>
    /// <param name="output">Receives the text.</param>
    /// <param name="snapshot">What was read.</param>
    /// <param name="requirement">The requirement.</param>
    private static void _ShowRequirement(TextWriter output, TraceSnapshot snapshot, Requirement requirement)
    {
        output.WriteLine(Invariant($"{requirement.Id} — {requirement.Text}"));
        output.WriteLine(Invariant($"  Done when: {requirement.DoneWhen}"));
        output.WriteLine(Invariant($"  Proof: {requirement.ProofText}"));
        output.WriteLine(Invariant($"  At: {requirement.File}:{requirement.Line}"));
        foreach (TestCard card in snapshot.Cards)
        {
            if (card.Proves.Contains(requirement.Id))
            {
                output.WriteLine(Invariant($"  Card: {card.Id}"));
                _ShowMarkers(output, snapshot, card.Id);
            }
        }
    }

    /// <summary>Writes a card, what it proves, and its tests.</summary>
    /// <param name="output">Receives the text.</param>
    /// <param name="snapshot">What was read.</param>
    /// <param name="card">The card.</param>
    private static void _ShowCard(TextWriter output, TraceSnapshot snapshot, TestCard card)
    {
        output.WriteLine(Invariant($"{card.Id} — {card.Content}"));
        output.WriteLine(Invariant($"  Proves: {string.Join(", ", card.Proves)}"));
        output.WriteLine(Invariant($"  At: {card.File}:{card.Line}"));
        _ShowMarkers(output, snapshot, card.Id);
    }

    /// <summary>Writes the marked tests of a card.</summary>
    /// <param name="output">Receives the text.</param>
    /// <param name="snapshot">What was read.</param>
    /// <param name="cardId">The card ID.</param>
    private static void _ShowMarkers(TextWriter output, TraceSnapshot snapshot, string cardId)
    {
        foreach (Marker marker in snapshot.Markers)
        {
            if (string.Equals(marker.CardId, cardId, StringComparison.Ordinal))
            {
                output.WriteLine(Invariant($"    Test: {marker.File}:{marker.Line} {marker.Method}"));
            }
        }
    }

    #endregion
}
