// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>States whether a finding fails the run.</summary>
internal enum Severity
{
    /// <summary>Fails the run with exit code 1.</summary>
    Error,

    /// <summary>A known gap in an entry that did not change; counted and listed, never failing.</summary>
    OpenGap,
}

/// <summary>One problem the tool found, with everything needed to fix it.</summary>
/// <param name="Rule">Rule identifier from <see cref="Rules"/>.</param>
/// <param name="Severity">Whether the finding fails the run.</param>
/// <param name="File">Repository-relative path with forward slashes.</param>
/// <param name="Line">One-based line number; 0 when the finding concerns a whole entry or file.</param>
/// <param name="Message">What is wrong, naming the concrete ID or line.</param>
/// <param name="Fix">What to do.</param>
internal readonly record struct Finding(string Rule, Severity Severity, string File, int Line, string Message, string Fix);

/// <summary>One rule: how its message and fix are worded.</summary>
/// <param name="Id">Rule identifier.</param>
/// <param name="Message">Builds the message from the subject and a detail.</param>
/// <param name="Fix">Builds the fix from the subject and a detail.</param>
internal sealed record RuleTemplate(string Id, Func<string, string, string> Message, Func<string, string, string> Fix);

/// <summary>The closed table of rules. Every finding is created here, so none can lack a location, message, or fix.</summary>
/// <remarks>Sync: docs/agents.md § Rules.</remarks>
internal static class Rules
{
    #region Rule identifiers

    /// <summary>A line matches no known form.</summary>
    public const string Unreadable = "F1";

    /// <summary>Text resembles an ID but breaks the form.</summary>
    public const string NearMiss = "F2";

    /// <summary>A required field is missing.</summary>
    public const string MissingField = "F3";

    /// <summary>A field holds a value outside its closed set.</summary>
    public const string BadValue = "F4";

    /// <summary>A spec file holds text but no readable entry.</summary>
    public const string NoEntries = "F5";

    /// <summary>One ID heads two entries.</summary>
    public const string Duplicate = "L1";

    /// <summary>A card proves a requirement that does not exist or is retired.</summary>
    public const string UnknownRequirement = "L2";

    /// <summary>A marker names a card that does not exist or is retired.</summary>
    public const string UnknownCard = "L3";

    /// <summary>An active requirement with proof kind test has no active card.</summary>
    public const string NoCard = "L4";

    /// <summary>A marker has no test method after it.</summary>
    public const string MarkerWithoutMethod = "L5";

    /// <summary>An active card has no marked test.</summary>
    public const string NoMarker = "L6";

    /// <summary>An ignore-file line is invalid.</summary>
    public const string BadIgnore = "L7";

    /// <summary>An accepted entry no longer exists.</summary>
    public const string Vanished = "C1";

    /// <summary>A card lost its only marked test.</summary>
    public const string MarkerRemoved = "C2";

    /// <summary>A card proves a requirement that changed and was not confirmed.</summary>
    public const string Unconfirmed = "C3";

    /// <summary>A ticked requirement's test is missing from the results.</summary>
    public const string NotRun = "E1";

    /// <summary>A ticked requirement's test did not pass.</summary>
    public const string Failed = "E2";

    /// <summary>The result file is older than a marked test file.</summary>
    public const string Stale = "E3";

    /// <summary>A test method name maps to markers in two files.</summary>
    public const string Ambiguous = "E4";

    #endregion

    #region Rule table

    /// <summary>Every rule. A new rule must be added here, which is what forces it to have a fix text.</summary>
    public static readonly RuleTemplate[] All =
    [
        new(Unreadable, static (_, _) => "This line matches no known form.", static (_, detail) => detail),
        new(
            NearMiss,
            static (subject, _) => Invariant($"\"{subject}\" looks like an ID but breaks the form."),
            static (_, detail) => detail),
        new(
            MissingField,
            static (subject, detail) => Invariant($"{subject} has no {detail}."),
            static (subject, detail) => SpecForm.FixForMissing(subject, detail)),
        new(
            BadValue,
            static (subject, detail) => Invariant($"{subject}: invalid value in {detail}."),
            static (subject, detail) => SpecForm.FixForValue(subject, detail)),
        new(
            NoEntries,
            static (_, _) => "This file holds text but no readable entry.",
            static (_, detail) => Invariant($"Restore the entry format: {detail} · Full form: reqtrace syntax")),
        new(
            Duplicate,
            static (subject, detail) => Invariant($"{subject} heads two entries, also at {detail}."),
            static (_, _) => "Keep one entry and give the other a new ID."),
        new(
            UnknownRequirement,
            static (subject, detail) => Invariant($"{subject} proves {detail}, which does not exist or is retired."),
            static (_, _) => "Point it at an active requirement, or retire the card."),
        new(
            UnknownCard,
            static (subject, _) => Invariant($"A marker names {subject}, which does not exist or is retired."),
            static (_, detail) => detail.Length > 0
                ? Invariant($"Did you mean {detail}?")
                : "Name an active test card."),
        new(
            NoCard,
            static (subject, _) => Invariant($"{subject} has no active test card."),
            static (_, _) => "Add a card, or set Proof to demo, review, or waived with a reason."),
        new(
            MarkerWithoutMethod,
            static (subject, _) => Invariant($"The marker {subject} has no test method within 8 lines."),
            static (_, _) => "Place the marker directly above the test method."),
        new(
            NoMarker,
            static (subject, _) => Invariant($"{subject} has no marked test."),
            static (_, _) => "Add the marker to the test that proves it."),
        new(
            BadIgnore,
            static (subject, detail) => Invariant($"Invalid ignore entry \"{subject}\": {detail}."),
            static (_, _) => "Write: path/inside/repo — reason"),
        new(
            Vanished,
            static (subject, _) => Invariant($"{subject} was accepted and no longer exists."),
            static (_, _) => "Restore it, or retire it under ## Retired with a reason."),
        new(
            MarkerRemoved,
            static (subject, _) => Invariant($"{subject} had a marked test and has none now."),
            static (_, _) => "Restore the test, or retire the card."),
        new(
            Unconfirmed,
            static (subject, detail) => Invariant($"{subject} proves {detail}, which changed. Not confirmed yet."),
            static (subject, _) => Invariant($"Check that it still proves it, then run: reqtrace confirm {subject} --note \"what you checked\"")),
        new(
            NotRun,
            static (subject, detail) => Invariant($"{subject} is ticked as met, but {detail} is not in the results."),
            static (_, _) => "Run the test, or untick Met."),
        new(
            Failed,
            static (subject, detail) => Invariant($"{subject} is ticked as met, but {detail} did not pass."),
            static (_, _) => "Fix the test or the code, or untick Met."),
        new(
            Stale,
            static (subject, _) => Invariant($"The result file is older than the test file of {subject}."),
            static (_, _) => "Run the tests again and pass the new result file."),
        new(
            Ambiguous,
            static (subject, _) => Invariant($"The method {subject} is marked in two files, so its result is ambiguous."),
            static (_, _) => "Rename one of the methods."),
    ];

    /// <summary>Creates a finding from the rule table.</summary>
    /// <param name="ruleId">A rule identifier of this class.</param>
    /// <param name="severity">Whether the finding fails the run.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="line">One-based line, or 0.</param>
    /// <param name="subject">The concrete ID or text the finding is about.</param>
    /// <param name="detail">Rule-specific detail.</param>
    /// <returns>The finding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The rule is not in the table; a bug in the caller.</exception>
    public static Finding Create(string ruleId, Severity severity, string file, int line, string subject, string detail)
    {
        // Findings are rare, so a linear scan costs nothing on a clean run and avoids building a map at start-up.
        for (int index = 0; index < All.Length; index++)
        {
            RuleTemplate rule = All[index];
            if (string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
            {
                return new(ruleId, severity, file, line, rule.Message(subject, detail), rule.Fix(subject, detail));
            }
        }

        throw new ArgumentOutOfRangeException(nameof(ruleId));
    }

    #endregion
}
