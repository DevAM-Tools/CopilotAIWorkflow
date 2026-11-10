// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Checks that every reference resolves and that active entries are proven.</summary>
internal static class LinkChecker
{
    /// <summary>Longest ID compared for a spelling suggestion; longer text cannot be a slip of one ID.</summary>
    private const int _MaxSuggestionLength = 128;

    /// <summary>Largest edit distance still offered as a suggestion.</summary>
    private const int _MaxSuggestionDistance = 3;

    #region Checks

    /// <summary>Adds link findings.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <param name="accepted">The accepted state, or null.</param>
    /// <param name="findings">Receives the findings.</param>
    public static void Check(TraceSnapshot snapshot, Dictionary<string, LockEntry>? accepted, List<Finding> findings)
    {
        Dictionary<string, Requirement> requirements = new(snapshot.Requirements.Count, StringComparer.Ordinal);
        foreach (Requirement requirement in snapshot.Requirements)
        {
            if (!requirements.TryAdd(requirement.Id, requirement))
            {
                Requirement first = requirements[requirement.Id];
                string location = Invariant($"{first.File}:{first.Line}");
                string id = requirement.Id;
                findings.Add(Rules.Create(Rules.Duplicate, Severity.Error, requirement.File, requirement.Line, id, location));
            }
        }

        Dictionary<string, TestCard> cards = new(snapshot.Cards.Count, StringComparer.Ordinal);
        HashSet<string> proven = new(StringComparer.Ordinal);
        foreach (TestCard card in snapshot.Cards)
        {
            if (!cards.TryAdd(card.Id, card))
            {
                string location = Invariant($"{cards[card.Id].File}:{cards[card.Id].Line}");
                findings.Add(Rules.Create(Rules.Duplicate, Severity.Error, card.File, card.Line, card.Id, location));
            }

            if (card.State == EntryState.Retired)
            {
                continue;
            }

            _CheckProves(card, requirements, proven, findings);
        }

        HashSet<string> marked = new(StringComparer.Ordinal);
        foreach (Marker marker in snapshot.Markers)
        {
            _CheckMarker(marker, cards, marked, findings);
        }

        _ReportGaps(snapshot, accepted, proven, marked, findings);
    }

    /// <summary>Checks the requirement references of one card and records what it proves.</summary>
    /// <param name="card">The card.</param>
    /// <param name="requirements">Requirements by ID.</param>
    /// <param name="proven">Receives the IDs the card proves.</param>
    /// <param name="findings">Receives findings.</param>
    private static void _CheckProves(
        TestCard card,
        Dictionary<string, Requirement> requirements,
        HashSet<string> proven,
        List<Finding> findings)
    {
        foreach (string id in card.Proves)
        {
            if (requirements.TryGetValue(id, out Requirement? requirement) && requirement.State != EntryState.Retired)
            {
                proven.Add(id);
                continue;
            }

            findings.Add(Rules.Create(Rules.UnknownRequirement, Severity.Error, card.File, card.Line, card.Id, id));
        }
    }

    /// <summary>Checks one marker.</summary>
    /// <param name="marker">The marker.</param>
    /// <param name="cards">Cards by ID.</param>
    /// <param name="marked">Receives the IDs of cards with a marker.</param>
    /// <param name="findings">Receives findings.</param>
    private static void _CheckMarker(
        Marker marker,
        Dictionary<string, TestCard> cards,
        HashSet<string> marked,
        List<Finding> findings)
    {
        string id = marker.CardId;
        if (!cards.TryGetValue(id, out TestCard? card) || card.State == EntryState.Retired)
        {
            findings.Add(Rules.Create(Rules.UnknownCard, Severity.Error, marker.File, marker.Line, id, Closest(id, cards.Keys)));
            return;
        }

        marked.Add(id);
        if (marker.Method is null)
        {
            findings.Add(Rules.Create(Rules.MarkerWithoutMethod, Severity.Error, marker.File, marker.Line, id, string.Empty));
        }
    }

    /// <summary>Reports entries that nothing proves. Whether a gap fails the run depends on whether the entry changed.</summary>
    /// <param name="snapshot">What was read.</param>
    /// <param name="accepted">The accepted state, or null.</param>
    /// <param name="proven">Requirements with an active card.</param>
    /// <param name="marked">Cards with a marker.</param>
    /// <param name="findings">Receives findings.</param>
    private static void _ReportGaps(
        TraceSnapshot snapshot,
        Dictionary<string, LockEntry>? accepted,
        HashSet<string> proven,
        HashSet<string> marked,
        List<Finding> findings)
    {
        foreach (Requirement requirement in snapshot.Requirements)
        {
            if (requirement.State == EntryState.Active && requirement.Proof == ProofKind.Test && !proven.Contains(requirement.Id))
            {
                string id = requirement.Id;
                Severity severity = _GapSeverity(accepted, id, SpecText.Fingerprint(requirement));
                findings.Add(Rules.Create(Rules.NoCard, severity, requirement.File, requirement.Line, id, string.Empty));
            }
        }

        foreach (TestCard card in snapshot.Cards)
        {
            if (card.State == EntryState.Active && !marked.Contains(card.Id))
            {
                Severity severity = _GapSeverity(accepted, card.Id, SpecText.Fingerprint(card));
                findings.Add(Rules.Create(Rules.NoMarker, severity, card.File, card.Line, card.Id, string.Empty));
            }
        }
    }

    /// <summary>Decides whether a gap blocks: only when its entry is new or changed since the accepted state.</summary>
    /// <param name="accepted">The accepted state, or null.</param>
    /// <param name="id">The entry ID.</param>
    /// <param name="hash">Its current fingerprint.</param>
    /// <returns>The severity.</returns>
    private static Severity _GapSeverity(Dictionary<string, LockEntry>? accepted, string id, string hash)
    {
        if (accepted is null)
        {
            return Severity.OpenGap;
        }

        return accepted.TryGetValue(id, out LockEntry? entry) && string.Equals(entry.Hash, hash, StringComparison.Ordinal)
            ? Severity.OpenGap
            : Severity.Error;
    }

    #endregion

    #region Suggestion

    /// <summary>Returns the closest known ID within the suggestion distance.</summary>
    /// <param name="id">The unknown ID.</param>
    /// <param name="known">The known IDs.</param>
    /// <returns>The closest ID, or an empty string.</returns>
    public static string Closest(string id, IEnumerable<string> known)
    {
        string best = string.Empty;
        int bestDistance = _MaxSuggestionDistance + 1;
        if (id.Length > _MaxSuggestionLength)
        {
            return best;
        }

        foreach (string candidate in known)
        {
            int distance = _EditDistance(id, candidate);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>Computes the Levenshtein distance.</summary>
    /// <param name="left">First string; bounded by the caller.</param>
    /// <param name="right">Second string.</param>
    /// <returns>The distance, or a value past the limit when the lengths alone rule it out.</returns>
    private static int _EditDistance(string left, string right)
    {
        if (Math.Abs(left.Length - right.Length) > _MaxSuggestionDistance || right.Length > _MaxSuggestionLength)
        {
            return int.MaxValue;
        }

        Span<int> previous = stackalloc int[right.Length + 1];
        Span<int> current = stackalloc int[right.Length + 1];
        for (int column = 0; column <= right.Length; column++)
        {
            previous[column] = column;
        }

        for (int row = 1; row <= left.Length; row++)
        {
            current[0] = row;
            for (int column = 1; column <= right.Length; column++)
            {
                int substitution = previous[column - 1] + (left[row - 1] == right[column - 1] ? 0 : 1);
                current[column] = Math.Min(substitution, Math.Min(previous[column], current[column - 1]) + 1);
            }

            current.CopyTo(previous);
        }

        return previous[right.Length];
    }

    #endregion
}
