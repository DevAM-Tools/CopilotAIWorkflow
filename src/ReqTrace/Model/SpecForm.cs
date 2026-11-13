// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>
/// The one home in code for the spec line forms and closed value sets: <see cref="SpecReader"/> and
/// <see cref="IgnoreList"/> accept exactly these, the syntax command prints them, and form fixes quote them.
/// </summary>
/// <remarks>
/// Immutable; safe from any thread.
/// Sync: .github/skills/tech-spec.md § Requirement entry, § Test card, § Trace (marker forms).
/// </remarks>
internal static class SpecForm
{
    #region Separators and words

    /// <summary>
    /// Separates an ID from its text, a proof kind or <c>none</c> from its reason, and an ignore path from its reason.
    /// </summary>
    public const string ReasonSeparator = " — ";

    /// <summary>Separates the fields that share a state line.</summary>
    public const string StateSeparator = " · ";

    /// <summary>State until a person approves the entry.</summary>
    public const string StateProposed = "proposed";

    /// <summary>State of an approved entry in force.</summary>
    public const string StateActive = "active";

    /// <summary>State of an entry kept only as history.</summary>
    public const string StateRetired = "retired";

    /// <summary>Met or Status: not proven yet.</summary>
    public const string MarkOpen = "⬜";

    /// <summary>Met: the Done-when ran and held. Status: the Content exists as a test.</summary>
    public const string MarkDone = "✅";

    /// <summary>Met: the Done-when ran and failed.</summary>
    public const string MarkFailed = "❌";

    /// <summary>Proof by a marked test; the only proof kind without a reason.</summary>
    public const string ProofTest = "test";

    /// <summary>Proof by a person watching it work.</summary>
    public const string ProofDemo = "demo";

    /// <summary>Proof by reading the result.</summary>
    public const string ProofReview = "review";

    /// <summary>No proof, by decision.</summary>
    public const string ProofWaived = "waived";

    /// <summary>Start of a Proves value that names no requirement; a reason follows.</summary>
    public const string ProvesNone = "none — ";

    #endregion

    #region Closed sets

    /// <summary>Values of State on requirements and cards, in lifecycle order.</summary>
    public static readonly ImmutableArray<string> States = [StateProposed, StateActive, StateRetired];

    /// <summary>Values of Met on a requirement.</summary>
    public static readonly ImmutableArray<string> MetMarks = [MarkOpen, MarkDone, MarkFailed];

    /// <summary>Values of Status on a test card.</summary>
    public static readonly ImmutableArray<string> StatusMarks = [MarkOpen, MarkDone];

    /// <summary>Proof kinds that need a reason after <see cref="ReasonSeparator"/>.</summary>
    public static readonly ImmutableArray<string> ReasonProofKinds = [ProofDemo, ProofReview, ProofWaived];

    /// <summary>Values of the optional Class field of a card.</summary>
    public static readonly ImmutableArray<string> Classes =
        ["happy", "error", "boundary", "collection", "absence", "concurrency", "trust-boundary"];

    /// <summary>Values of the optional Layer field of a card.</summary>
    public static readonly ImmutableArray<string> Layers = ["unit", "integration", "UI"];

    /// <summary>Returns whether <paramref name="value"/> equals one entry of <paramref name="set"/>, ordinal.</summary>
    /// <param name="set">A closed set of this class; always initialized, so no default check is needed.</param>
    /// <param name="value">The value as read; not trimmed here.</param>
    /// <returns>Whether the value is allowed.</returns>
    /// <remarks>Perf: span comparison over an immutable array, so the reader allocates nothing per field.</remarks>
    public static bool IsIn(ImmutableArray<string> set, ReadOnlySpan<char> value)
    {
        for (int index = 0; index < set.Length; index++)
        {
            if (value.SequenceEqual(set[index]))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Field names

    /// <summary>Fields a requirement may hold.</summary>
    /// <remarks>Invariant: <see cref="SpecReader"/> addresses them by index; append only.</remarks>
    public static readonly ImmutableArray<string> RequirementFields = ["Done when", "State", "Proof", "Built in", "Replaced by"];

    /// <summary>Fields a test card may hold. Code stays readable so that older cards do not fail; it is never printed.</summary>
    /// <remarks>Invariant: <see cref="SpecReader"/> addresses them by index; append only.</remarks>
    public static readonly ImmutableArray<string> CardFields = ["Proves", "Class", "Layer", "Content", "Out", "Code", "State"];

    #endregion

    #region Examples and field lines

    /// <summary>Start line of the example requirement.</summary>
    public const string RequirementStartLine = "- **REQ-AREA-TOPIC** — The system shall {obligation}.";

    /// <summary>Done-when line of a requirement, indented two spaces.</summary>
    public const string DoneWhenLine = "  - **Done when:** {actor} {boundary action} and sees {observable}.";

    /// <summary>State line of a requirement with its Met mark, indented two spaces.</summary>
    public const string RequirementStateLine = "  - **State:** proposed · **Met:** ⬜";

    /// <summary>Proof line of a requirement, indented two spaces.</summary>
    public const string ProofLine = "  - **Proof:** test";

    /// <summary>A requirement entry the reader accepts unchanged below an area heading; lines end with <c>\n</c>.</summary>
    public const string RequirementExample =
        RequirementStartLine + "\n" + DoneWhenLine + "\n" + RequirementStateLine + "\n" + ProofLine;

    /// <summary>Heading line of the example card.</summary>
    public const string CardHeadingLine = "### TEST-AREA-TOPIC";

    /// <summary>Proves line of a card, at column 0.</summary>
    public const string ProvesLine = "- **Proves:** REQ-AREA-TOPIC";

    /// <summary>Content line of a card, at column 0.</summary>
    public const string ContentLine = "- **Content:** {setup}. {action}. {observable}. Catches {defect}.";

    /// <summary>State line of a card with its Status mark, at column 0.</summary>
    public const string CardStateLine = "- **State:** proposed · **Status:** ⬜";

    /// <summary>
    /// A test card the reader accepts unchanged; it proves <see cref="RequirementExample"/>. Lines end with <c>\n</c>.
    /// </summary>
    public const string CardExample =
        CardHeadingLine + "\n\n" + ProvesLine + "\n- **Class:** happy\n- **Layer:** unit\n" + ContentLine + "\n" + CardStateLine;

    /// <summary>An ignore line with a path inside the repository and a reason.</summary>
    public const string IgnoreExample = "tests/fixtures/markers — sample files with marker text, not tests";

    /// <summary>C# marker line naming the example card.</summary>
    /// <remarks>
    /// The escaped quotes keep <see cref="MarkerScanner"/> from reading this source line as a marker: in the file,
    /// <c>Property(</c> is followed by a backslash. Sync: MarkerScanner (C#, Python, and Rust marker readers).
    /// </remarks>
    public const string CSharpMarker = "[Test, Property(\"Card\", \"TEST-AREA-TOPIC\")]";

    /// <summary>Python marker line naming the example card.</summary>
    public const string PythonMarker = "@pytest.mark.card(\"TEST-AREA-TOPIC\")";

    /// <summary>Rust marker line naming the example card.</summary>
    public const string RustMarker = "// card: TEST-AREA-TOPIC";

    /// <summary>Allowed values of State, as printed and quoted in fixes.</summary>
    /// <remarks>Invariant: declared after the sets it joins, so static initialization has filled them.</remarks>
    public static readonly string StateValues = string.Join(" | ", States);

    /// <summary>Allowed values of Met.</summary>
    public static readonly string MetValues = string.Join(" | ", MetMarks);

    /// <summary>Allowed values of Status.</summary>
    public static readonly string StatusValues = string.Join(" | ", StatusMarks);

    /// <summary>Allowed values of Proof: <see cref="ProofTest"/>, then each reason kind with its placeholder.</summary>
    public static readonly string ProofValues = _JoinProofValues();

    /// <summary>Allowed values of Class.</summary>
    public static readonly string ClassValues = string.Join(" | ", Classes);

    /// <summary>Allowed values of Layer.</summary>
    public static readonly string LayerValues = string.Join(" | ", Layers);

    /// <summary>Builds <see cref="ProofValues"/> from <see cref="ReasonProofKinds"/>, so a new kind is printed at once.</summary>
    /// <returns>For example <c>test | demo — {reason} | review — {reason} | waived — {reason}</c>.</returns>
    /// <remarks>Runs once, in static initialization; <see cref="ReasonProofKinds"/> is declared above, so it is set.</remarks>
    private static string _JoinProofValues()
    {
        StringBuilder text = new(ProofTest, 80);
        foreach (string kind in ReasonProofKinds)
        {
            text.Append(" | ").Append(kind).Append(ReasonSeparator).Append("{reason}");
        }

        return text.ToString();
    }

    #endregion

    #region Fix texts

    /// <summary>Builds the F3 fix: the line to add, quoted with its indent, and the syntax call for the whole form.</summary>
    /// <param name="id">The entry ID; the prefix <c>TEST-</c> selects the card form, anything else the requirement form.</param>
    /// <param name="field">The field name as the reader reports it (<c>Done when</c>, <c>State</c>, <c>Met</c>, …).</param>
    /// <returns>The fix; for a field without a known line, a generic sentence with the syntax call.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static string FixForMissing(string id, string field)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(field);

        // The quoted line keeps its indent: requirement fields sit at 2 spaces, card fields at column 0.
        bool isCard = id.StartsWith("TEST-", StringComparison.Ordinal);
        string line = isCard
            ? _CardFieldLine(field)
            : _RequirementFieldLine(field);
        return line.Length > 0
            ? Invariant($"Add the line \"{line}\" · Full form: {_SyntaxCall(isCard)}")
            : Invariant($"Add the {field} field. Full form: {_SyntaxCall(isCard)}");
    }

    /// <summary>Builds the F4 fix: every allowed value of the field and the syntax call for the whole form.</summary>
    /// <param name="id">The entry ID; the prefix <c>TEST-</c> selects the card form.</param>
    /// <param name="field">The field name as the reader reports it.</param>
    /// <returns>The fix; for a field without a closed set, a generic sentence with the syntax call.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static string FixForValue(string id, string field)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(field);

        bool isCard = id.StartsWith("TEST-", StringComparison.Ordinal);
        string values = field switch
        {
            "State" => StateValues,
            "Met" => MetValues,
            "Status" => StatusValues,
            "Proof" => ProofValues,
            "Class" => ClassValues,
            "Layer" => LayerValues,
            _ => string.Empty,
        };
        return values.Length > 0
            ? Invariant($"Use one of: {values} · Full form: {_SyntaxCall(isCard)}")
            : Invariant($"Use a value allowed for {field}. Full form: {_SyntaxCall(isCard)}");
    }

    /// <summary>Returns the requirement line for a missing field.</summary>
    /// <param name="field">The field name.</param>
    /// <returns>The line with its 2-space indent; empty when no line is known for the field.</returns>
    private static string _RequirementFieldLine(string field)
    {
        return field switch
        {
            "Done when" => DoneWhenLine,
            "State" or "Met" => RequirementStateLine,
            "Proof" => ProofLine,
            _ => string.Empty,
        };
    }

    /// <summary>Returns the card line for a missing field.</summary>
    /// <param name="field">The field name.</param>
    /// <returns>The line at column 0; empty when no line is known for the field.</returns>
    private static string _CardFieldLine(string field)
    {
        return field switch
        {
            "Proves" => ProvesLine,
            "Content" => ContentLine,
            "State" or "Status" => CardStateLine,
            _ => string.Empty,
        };
    }

    /// <summary>Returns the syntax call that prints the whole form of the entry kind.</summary>
    /// <param name="isCard">Whether the entry is a test card.</param>
    /// <returns>The call text.</returns>
    private static string _SyntaxCall(bool isCard)
    {
        return isCard
            ? "reqtrace syntax card"
            : "reqtrace syntax requirement";
    }

    #endregion
}
