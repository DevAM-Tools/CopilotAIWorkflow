// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Compares what is there with the state a person accepted, so nothing can disappear or change unnoticed.</summary>
internal static class ChangeChecker
{
    /// <summary>The lock file, named in findings about entries that no longer have a file of their own.</summary>
    private const string _LockFile = "spec/trace.lock";

    /// <summary>Adds change findings.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <param name="accepted">The accepted state, or null, in which case nothing is compared.</param>
    /// <param name="findings">Receives the findings.</param>
    public static void Check(TraceSnapshot snapshot, Dictionary<string, LockEntry>? accepted, List<Finding> findings)
    {
        if (accepted is null)
        {
            return;
        }

        Dictionary<string, Requirement> requirements = RequirementsById(snapshot);
        Dictionary<string, TestCard> cards = new(StringComparer.Ordinal);
        foreach (TestCard card in snapshot.Cards)
        {
            cards.TryAdd(card.Id, card);
        }

        foreach (LockEntry entry in accepted.Values)
        {
            if (!requirements.ContainsKey(entry.Id) && !cards.ContainsKey(entry.Id))
            {
                findings.Add(Rules.Create(Rules.Vanished, Severity.Error, _LockFile, 0, entry.Id, string.Empty));
            }
        }

        Dictionary<string, int> markers = CountMarkers(snapshot);
        foreach (TestCard card in cards.Values)
        {
            if (!accepted.TryGetValue(card.Id, out LockEntry? entry) || card.State == EntryState.Retired)
            {
                continue;
            }

            if (entry.Marked > 0 && markers.GetValueOrDefault(card.Id) == 0)
            {
                findings.Add(Rules.Create(Rules.MarkerRemoved, Severity.Error, card.File, card.Line, card.Id, string.Empty));
            }

            _CheckConfirmed(card, entry, requirements, findings);
        }
    }

    /// <summary>Reports a card that proves a requirement changed since the card was last confirmed.</summary>
    /// <param name="card">The card.</param>
    /// <param name="entry">Its accepted entry.</param>
    /// <param name="requirements">Requirements by ID.</param>
    /// <param name="findings">Receives findings.</param>
    private static void _CheckConfirmed(
        TestCard card,
        LockEntry entry,
        Dictionary<string, Requirement> requirements,
        List<Finding> findings)
    {
        if (!TryStamp(card, requirements, out string stamp)
            || string.Equals(stamp, entry.RequirementStamp, StringComparison.Ordinal))
        {
            return;
        }

        string proved = string.Join(", ", card.Proves);
        findings.Add(Rules.Create(Rules.Unconfirmed, Severity.Error, card.File, card.Line, card.Id, proved));
    }

    /// <summary>Builds the fingerprint list of what a card proves.</summary>
    /// <param name="card">The card.</param>
    /// <param name="requirements">Requirements by ID.</param>
    /// <param name="stamp">Receives the stamp.</param>
    /// <returns>False when a proved requirement is missing, which the link check reports.</returns>
    public static bool TryStamp(TestCard card, Dictionary<string, Requirement> requirements, out string stamp)
    {
        stamp = string.Empty;
        for (int index = 0; index < card.Proves.Count; index++)
        {
            if (!requirements.TryGetValue(card.Proves[index], out Requirement? requirement))
            {
                return false;
            }

            stamp = index == 0
                ? SpecText.Fingerprint(requirement)
                : Invariant($"{stamp}+{SpecText.Fingerprint(requirement)}");
        }

        return true;
    }

    /// <summary>Indexes requirements by ID, keeping the first of a duplicate.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <returns>The map.</returns>
    public static Dictionary<string, Requirement> RequirementsById(TraceSnapshot snapshot)
    {
        Dictionary<string, Requirement> requirements = new(snapshot.Requirements.Count, StringComparer.Ordinal);
        foreach (Requirement requirement in snapshot.Requirements)
        {
            requirements.TryAdd(requirement.Id, requirement);
        }

        return requirements;
    }

    /// <summary>Counts markers per card ID.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <returns>The counts.</returns>
    public static Dictionary<string, int> CountMarkers(TraceSnapshot snapshot)
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (Marker marker in snapshot.Markers)
        {
            counts[marker.CardId] = counts.GetValueOrDefault(marker.CardId) + 1;
        }

        return counts;
    }
}
