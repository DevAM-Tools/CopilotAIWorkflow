// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Compares claims with a test result file: a tick is easy to write, a passing result is not.</summary>
internal static class EvidenceChecker
{
    /// <summary>The name reported in findings about results.</summary>
    private const string _ResultsLabel = "test results";

    /// <summary>Adds evidence findings.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="snapshot">What was read.</param>
    /// <param name="resultsPath">Result file or folder.</param>
    /// <param name="findings">Receives the findings.</param>
    /// <exception cref="IOException">The results cannot be read.</exception>
    /// <exception cref="InvalidDataException">The results are malformed.</exception>
    public static void Check(string root, TraceSnapshot snapshot, string resultsPath, List<Finding> findings)
    {
        Dictionary<string, (bool Passed, DateTime Written)> outcomes = TrxReader.ReadAll(resultsPath);

        Dictionary<string, string> methodFiles = new(StringComparer.Ordinal);
        HashSet<string> ambiguous = new(StringComparer.Ordinal);
        foreach (Marker marker in snapshot.Markers)
        {
            if (marker.Method is not null
                && !methodFiles.TryAdd(marker.Method, marker.File)
                && !string.Equals(methodFiles[marker.Method], marker.File, StringComparison.Ordinal))
            {
                ambiguous.Add(marker.Method);
            }
        }

        foreach (Requirement requirement in snapshot.Requirements)
        {
            if (requirement.State != EntryState.Active || !requirement.IsMet || requirement.Proof != ProofKind.Test)
            {
                continue;
            }

            foreach (TestCard card in snapshot.Cards)
            {
                if (card.State != EntryState.Retired && _Proves(card, requirement.Id))
                {
                    _CheckCard(root, snapshot, requirement, card, outcomes, ambiguous, findings);
                }
            }
        }
    }

    /// <summary>Checks every marked test of one card.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="snapshot">What was read.</param>
    /// <param name="requirement">The ticked requirement.</param>
    /// <param name="card">A card proving it.</param>
    /// <param name="outcomes">Outcomes per method.</param>
    /// <param name="ambiguous">Methods marked in two files.</param>
    /// <param name="findings">Receives findings.</param>
    private static void _CheckCard(
        string root,
        TraceSnapshot snapshot,
        Requirement requirement,
        TestCard card,
        Dictionary<string, (bool Passed, DateTime Written)> outcomes,
        HashSet<string> ambiguous,
        List<Finding> findings)
    {
        foreach (Marker marker in snapshot.Markers)
        {
            if (!string.Equals(marker.CardId, card.Id, StringComparison.Ordinal) || marker.Method is null)
            {
                continue;
            }

            string method = marker.Method;
            if (ambiguous.Contains(method))
            {
                findings.Add(Rules.Create(Rules.Ambiguous, Severity.Error, marker.File, marker.Line, method, string.Empty));
            }
            else if (!outcomes.TryGetValue(method, out (bool Passed, DateTime Written) outcome))
            {
                findings.Add(Rules.Create(Rules.NotRun, Severity.Error, _ResultsLabel, 0, requirement.Id, method));
            }
            else if (File.GetLastWriteTimeUtc(Path.Join(root, marker.File)) > outcome.Written)
            {
                findings.Add(Rules.Create(Rules.Stale, Severity.Error, marker.File, marker.Line, method, string.Empty));
            }
            else if (!outcome.Passed)
            {
                findings.Add(Rules.Create(Rules.Failed, Severity.Error, _ResultsLabel, 0, requirement.Id, method));
            }
        }
    }

    /// <summary>Returns whether a card proves a requirement.</summary>
    /// <param name="card">The card.</param>
    /// <param name="requirementId">The requirement ID.</param>
    /// <returns>Whether it does.</returns>
    private static bool _Proves(TestCard card, string requirementId)
    {
        for (int index = 0; index < card.Proves.Count; index++)
        {
            if (string.Equals(card.Proves[index], requirementId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
