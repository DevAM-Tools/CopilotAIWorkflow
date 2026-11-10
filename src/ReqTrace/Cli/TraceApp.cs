// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Runs one command. Separate from the process entry so that tests drive the whole tool without a process.</summary>
internal static class TraceApp
{
    #region Entry

    /// <summary>Runs the tool.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="workingDirectory">Directory the search for the repository starts from.</param>
    /// <param name="output">Standard output.</param>
    /// <param name="error">Standard error.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args, string workingDirectory, TextWriter output, TextWriter error)
    {
        if (args.Length > 0 && args[0] is "--help" or "-h" or "help")
        {
            output.WriteLine(CliOptions.Usage);
            return CliOptions.ExitOk;
        }

        if (!CliOptions.TryParse(args, out CliOptions? options, out string parseError))
        {
            error.WriteLine(parseError);
            error.WriteLine(CliOptions.Usage);
            return CliOptions.ExitUsage;
        }

        if (!RepositoryLocator.TryFind(workingDirectory, out string root))
        {
            error.WriteLine("No spec/index.md found here or in any parent folder.");
            return CliOptions.ExitUsage;
        }

        // Expected input failures end as exit code 2 with the reason; anything else is a bug and stays an exception.
        try
        {
            return _RunCommand(options!, root, output, error);
        }
        catch (IOException exception)
        {
            error.WriteLine(Invariant($"Cannot read input: {exception.Message}"));
            return CliOptions.ExitUsage;
        }
        catch (InvalidDataException exception)
        {
            error.WriteLine(exception.Message);
            return CliOptions.ExitUsage;
        }
        catch (UnauthorizedAccessException exception)
        {
            error.WriteLine(Invariant($"Cannot read input: {exception.Message}"));
            return CliOptions.ExitUsage;
        }
    }

    /// <summary>Dispatches a parsed command.</summary>
    /// <param name="options">The options.</param>
    /// <param name="root">Repository root.</param>
    /// <param name="output">Standard output.</param>
    /// <param name="error">Standard error.</param>
    /// <returns>The process exit code.</returns>
    private static int _RunCommand(CliOptions options, string root, TextWriter output, TextWriter error)
    {
        return options.Command switch
        {
            TraceCommand.Show => _Show(options.Id!, root, output, error),
            TraceCommand.Report => _Report(root, output),
            TraceCommand.Accept => _Accept(options.AllowGapReason, root, output),
            TraceCommand.Confirm => _Confirm(options.Id!, options.Note!, root, output, error),
            _ => _Check(options, root, output),
        };
    }

    #endregion

    #region Commands

    /// <summary>Runs check.</summary>
    /// <param name="options">The options.</param>
    /// <param name="root">Repository root.</param>
    /// <param name="output">Standard output.</param>
    /// <returns>The process exit code.</returns>
    private static int _Check(CliOptions options, string root, TextWriter output)
    {
        TraceOutcome outcome = TraceChecker.Run(root, options.ResultsPath);
        TraceSnapshot snapshot = outcome.Snapshot;
        CheckResult result = new(
            outcome.Findings,
            snapshot.Requirements.Count,
            snapshot.Cards.Count,
            snapshot.Markers.Count,
            snapshot.IgnoredPaths.Count,
            outcome.EvidenceChecked,
            outcome.Accepted is not null);
        if (options.Json)
        {
            FindingWriter.WriteJson(output, result);
        }
        else
        {
            FindingWriter.WriteText(output, result);
        }

        return _HasError(result.Findings)
            ? CliOptions.ExitFindings
            : CliOptions.ExitOk;
    }

    /// <summary>Runs show.</summary>
    /// <param name="id">The ID.</param>
    /// <param name="root">Repository root.</param>
    /// <param name="output">Standard output.</param>
    /// <param name="error">Standard error.</param>
    /// <returns>The process exit code.</returns>
    private static int _Show(string id, string root, TextWriter output, TextWriter error)
    {
        TraceSnapshot snapshot = SnapshotReader.Read(root);
        if (ReportWriter.WriteShow(output, snapshot, id))
        {
            return CliOptions.ExitOk;
        }

        List<string> known = [];
        foreach (Requirement requirement in snapshot.Requirements)
        {
            known.Add(requirement.Id);
        }

        foreach (TestCard card in snapshot.Cards)
        {
            known.Add(card.Id);
        }

        string closest = LinkChecker.Closest(id, known);
        error.WriteLine(closest.Length > 0
            ? Invariant($"Unknown ID: {id}. Did you mean {closest}?")
            : Invariant($"Unknown ID: {id}."));
        return CliOptions.ExitUsage;
    }

    /// <summary>Runs report.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="output">Standard output.</param>
    /// <returns>The process exit code.</returns>
    private static int _Report(string root, TextWriter output)
    {
        ReportWriter.WriteReport(output, TraceChecker.Run(root, null));
        return CliOptions.ExitOk;
    }

    /// <summary>Runs accept: refuses while anything is wrong, and records a new gap only with a stated reason.</summary>
    /// <param name="allowGapReason">The reason, or null.</param>
    /// <param name="root">Repository root.</param>
    /// <param name="output">Standard output.</param>
    /// <returns>The process exit code.</returns>
    private static int _Accept(string? allowGapReason, string root, TextWriter output)
    {
        TraceOutcome outcome = TraceChecker.Run(root, null);
        HashSet<string> newGapMessages = new(StringComparer.Ordinal);
        List<Finding> refused = [];
        foreach (Finding finding in outcome.Findings)
        {
            if (finding.Severity != Severity.Error)
            {
                continue;
            }

            if (allowGapReason is not null && _IsGapRule(finding.Rule))
            {
                newGapMessages.Add(finding.Message);
                continue;
            }

            refused.Add(finding);
        }

        if (refused.Count > 0)
        {
            output.WriteLine("Not accepted. Resolve these first:");
            TraceSnapshot read = outcome.Snapshot;
            CheckResult result = new(refused, read.Requirements.Count, read.Cards.Count, read.Markers.Count, 0, true, true);
            FindingWriter.WriteText(output, result);
            return CliOptions.ExitFindings;
        }

        List<LockEntry> entries = _BuildEntries(outcome, allowGapReason, newGapMessages);
        TraceLock.Write(root, entries);
        output.WriteLine(Invariant($"Accepted {entries.Count} entries."));
        if (outcome.Accepted is null)
        {
            output.WriteLine("This is the starting point: gaps present now stay open and do not block.");
        }

        return CliOptions.ExitOk;
    }

    /// <summary>Runs confirm.</summary>
    /// <param name="id">The card ID.</param>
    /// <param name="note">What was checked.</param>
    /// <param name="root">Repository root.</param>
    /// <param name="output">Standard output.</param>
    /// <param name="error">Standard error.</param>
    /// <returns>The process exit code.</returns>
    private static int _Confirm(string id, string note, string root, TextWriter output, TextWriter error)
    {
        TraceSnapshot snapshot = SnapshotReader.Read(root);
        Dictionary<string, LockEntry>? accepted = TraceLock.Read(root);
        if (accepted is null || !accepted.TryGetValue(id, out LockEntry? entry))
        {
            error.WriteLine(Invariant($"{id} is not in the accepted state."));
            return CliOptions.ExitUsage;
        }

        TestCard? card = null;
        foreach (TestCard candidate in snapshot.Cards)
        {
            if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
            {
                card = candidate;
            }
        }

        if (card is null || !ChangeChecker.TryStamp(card, ChangeChecker.RequirementsById(snapshot), out string stamp))
        {
            error.WriteLine(Invariant($"{id} is not a test card whose requirements exist."));
            return CliOptions.ExitUsage;
        }

        accepted[id] = entry with { RequirementStamp = stamp, Note = note };
        TraceLock.Write(root, accepted.Values);
        output.WriteLine(Invariant($"Confirmed {id}."));
        return CliOptions.ExitOk;
    }

    #endregion

    #region Helpers

    /// <summary>Builds the accepted state from the current one.</summary>
    /// <param name="outcome">The check outcome.</param>
    /// <param name="reason">Reason for new gaps, or null.</param>
    /// <param name="newGapMessages">Messages of the gaps that reason covers.</param>
    /// <returns>One entry per requirement and card.</returns>
    private static List<LockEntry> _BuildEntries(TraceOutcome outcome, string? reason, HashSet<string> newGapMessages)
    {
        TraceSnapshot snapshot = outcome.Snapshot;
        Dictionary<string, LockEntry>? accepted = outcome.Accepted;
        Dictionary<string, Requirement> requirements = ChangeChecker.RequirementsById(snapshot);
        Dictionary<string, int> markers = ChangeChecker.CountMarkers(snapshot);
        List<LockEntry> entries = [];

        foreach (Requirement requirement in requirements.Values)
        {
            string id = requirement.Id;
            string gap = _GapReason(accepted, id, Invariant($"{id} has no active test card."), reason, newGapMessages);
            string text = SpecText.Obligation(requirement);
            string hash = SpecText.Fingerprint(requirement);
            entries.Add(new(id, hash, _StateWord(requirement.State), gap, 0, string.Empty, string.Empty, text));
        }

        foreach (TestCard card in snapshot.Cards)
        {
            string stamp = ChangeChecker.TryStamp(card, requirements, out string current)
                ? current
                : string.Empty;
            string note = accepted is not null && accepted.TryGetValue(card.Id, out LockEntry? previous)
                ? previous.Note
                : string.Empty;
            string gap = _GapReason(accepted, card.Id, Invariant($"{card.Id} has no marked test."), reason, newGapMessages);
            int marked = markers.GetValueOrDefault(card.Id);
            entries.Add(new(card.Id, SpecText.Fingerprint(card), _StateWord(card.State), gap, marked, stamp, note, string.Empty));
        }

        return entries;
    }

    /// <summary>Returns the gap reason to store for an entry.</summary>
    /// <param name="accepted">The accepted state, or null.</param>
    /// <param name="id">The entry ID.</param>
    /// <param name="gapMessage">The message that gap would have.</param>
    /// <param name="reason">Reason for new gaps, or null.</param>
    /// <param name="newGapMessages">Messages of the new gaps.</param>
    /// <returns>The reason, or an empty string.</returns>
    private static string _GapReason(
        Dictionary<string, LockEntry>? accepted,
        string id,
        string gapMessage,
        string? reason,
        HashSet<string> newGapMessages)
    {
        if (reason is not null && newGapMessages.Contains(gapMessage))
        {
            return reason;
        }

        return accepted is not null && accepted.TryGetValue(id, out LockEntry? previous)
            ? previous.GapReason
            : string.Empty;
    }

    /// <summary>Returns the lock word for a state.</summary>
    /// <param name="state">The state.</param>
    /// <returns>The word.</returns>
    private static string _StateWord(EntryState state)
    {
        return state switch
        {
            EntryState.Proposed => "proposed",
            EntryState.Retired => "retired",
            _ => "active",
        };
    }

    /// <summary>Returns whether a rule is a gap that a reason may accept.</summary>
    /// <param name="rule">The rule.</param>
    /// <returns>Whether it is.</returns>
    private static bool _IsGapRule(string rule)
    {
        return string.Equals(rule, Rules.NoCard, StringComparison.Ordinal)
            || string.Equals(rule, Rules.NoMarker, StringComparison.Ordinal);
    }

    /// <summary>Returns whether any finding is an error.</summary>
    /// <param name="findings">The findings.</param>
    /// <returns>Whether the run fails.</returns>
    private static bool _HasError(IReadOnlyList<Finding> findings)
    {
        for (int index = 0; index < findings.Count; index++)
        {
            if (findings[index].Severity == Severity.Error)
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}
