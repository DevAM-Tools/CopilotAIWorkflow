// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>
/// Reads spec files in closed-world mode: every line in an entry area must match a known form,
/// so a malformed entry cannot vanish unnoticed.
/// </summary>
/// <remarks>
/// Perf: hand-written span parsing instead of regular expressions. Building a regular expression
/// costs start-up time on every run, and this tool runs after every agent edit.
/// Sync: .github/skills/tech-spec.md § Requirement entry and § Test card.
/// </remarks>
internal static class SpecReader
{
    #region Field names

    /// <summary>Index of Done when in <see cref="_RequirementFields"/>.</summary>
    private const int _DoneWhen = 0;

    /// <summary>Index of State in <see cref="_RequirementFields"/>.</summary>
    private const int _RequirementState = 1;

    /// <summary>Index of Proof in <see cref="_RequirementFields"/>.</summary>
    private const int _Proof = 2;

    /// <summary>Index of Proves in <see cref="_CardFields"/>.</summary>
    private const int _Proves = 0;

    /// <summary>Index of Class in <see cref="_CardFields"/>.</summary>
    private const int _Class = 1;

    /// <summary>Index of Layer in <see cref="_CardFields"/>.</summary>
    private const int _Layer = 2;

    /// <summary>Index of Content in <see cref="_CardFields"/>.</summary>
    private const int _Content = 3;

    /// <summary>Index of State in <see cref="_CardFields"/>.</summary>
    private const int _CardState = 6;

    /// <summary>Fields a requirement may hold.</summary>
    private static readonly string[] _RequirementFields = ["Done when", "State", "Proof", "Built in", "Replaced by"];

    /// <summary>Fields a test card may hold. Code remains readable so that older cards do not fail.</summary>
    private static readonly string[] _CardFields = ["Proves", "Class", "Layer", "Content", "Out", "Code", "State"];

    /// <summary>Separator between the fields that share a state line.</summary>
    private const string _StateSeparator = " · ";

    #endregion

    #region Requirements

    /// <summary>Reads one requirement file.</summary>
    /// <param name="path">Absolute path.</param>
    /// <param name="relativePath">Repository-relative path used in findings.</param>
    /// <param name="requirements">Receives the entries.</param>
    /// <param name="findings">Receives reading findings.</param>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static void ReadRequirements(string path, string relativePath, List<Requirement> requirements, List<Finding> findings)
    {
        using StreamReader reader = _Open(path);

        bool inArea = false;
        bool inStories = false;
        bool hasText = false;
        int entriesBefore = requirements.Count;
        _Draft? draft = null;
        int lineNumber = 0;
        string? line;

        // Walk the file once. Each line is one class; the final else is what turns an unknown line into a finding.
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            ReadOnlySpan<char> span = line;
            if (span.IsWhiteSpace())
            {
                continue;
            }

            hasText = true;
            if (span.StartsWith("# ", StringComparison.Ordinal) && !inArea)
            {
                continue;
            }

            if (span.SequenceEqual("## User stories") || span.SequenceEqual("## Retired"))
            {
                _FinishRequirement(draft, relativePath, requirements, findings);
                draft = null;
                inStories = span.SequenceEqual("## User stories");
                inArea = true;
                continue;
            }

            if (span.StartsWith("### ", StringComparison.Ordinal))
            {
                _FinishRequirement(draft, relativePath, requirements, findings);
                draft = null;
                inArea = true;
                inStories = false;
                continue;
            }

            // Invariant: an entry counts wherever it stands. Restricting this to areas would let a well-formed
            // entry above the first heading vanish without a finding.
            if (!inStories && _TryReadRequirementStart(span, out string id, out string text))
            {
                _FinishRequirement(draft, relativePath, requirements, findings);
                draft = new(id, lineNumber, _RequirementFields.Length) { Text = text };
                continue;
            }

            if (draft is not null && _TryReadField(span, 2, _RequirementFields, out int field, out string value))
            {
                draft.Set(field, value);
                continue;
            }

            if (draft is not null && _IsContinuation(span))
            {
                draft.AppendToLast(span.Trim().ToString());
                continue;
            }

            // Prose is allowed before the first area and in the stories, except for what looks like a broken entry.
            bool isBrokenEntry = !inStories && span.StartsWith("- **", StringComparison.Ordinal) && _IsNearMiss(span);
            if ((!inArea || inStories) && !isBrokenEntry)
            {
                continue;
            }

            findings.Add(_LineFinding(
                span,
                relativePath,
                lineNumber,
                "Write: - **REQ-AREA-TOPIC** — The system shall …",
                "Write a field such as \"  - **Done when:** …\", or move this text above the first ### heading."));
        }

        _FinishRequirement(draft, relativePath, requirements, findings);

        if (hasText && requirements.Count == entriesBefore)
        {
            findings.Add(Rules.Create(Rules.NoEntries, Severity.Error, relativePath, 1, string.Empty, "- **REQ-AREA-TOPIC** — The system shall …"));
        }
    }

    /// <summary>Validates a finished draft and adds it to the result.</summary>
    /// <param name="draft">The draft, or null when no entry is open.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="requirements">Receives the entry.</param>
    /// <param name="findings">Receives field findings.</param>
    private static void _FinishRequirement(_Draft? draft, string file, List<Requirement> requirements, List<Finding> findings)
    {
        if (draft is null)
        {
            return;
        }

        // A retired entry keeps its text and needs only its state; the other fields apply while it is in force.
        EntryState state = _ReadState(draft, _RequirementState, file, findings, out ReadOnlySpan<char> remainder);
        bool isMet = false;
        ProofKind proof = ProofKind.Test;
        if (state != EntryState.Retired)
        {
            if (draft.Get(_DoneWhen).Length == 0)
            {
                findings.Add(Rules.Create(Rules.MissingField, Severity.Error, file, draft.Line, draft.Id, "Done when"));
            }

            isMet = _ReadMet(draft, remainder, file, findings);
            proof = _ReadProof(draft, file, findings);
        }

        string doneWhen = SpecText.Collapse(draft.Get(_DoneWhen));
        string text = SpecText.Collapse(draft.Text);
        requirements.Add(new(draft.Id, text, doneWhen, state, isMet, proof, draft.Get(_Proof), file, draft.Line));
    }

    /// <summary>Reads the Met mark from the part of the state line after the state.</summary>
    /// <param name="draft">The entry.</param>
    /// <param name="remainder">The rest of the state line.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="findings">Receives findings.</param>
    /// <returns>Whether the entry is ticked as met.</returns>
    private static bool _ReadMet(_Draft draft, ReadOnlySpan<char> remainder, string file, List<Finding> findings)
    {
        const string Prefix = "**Met:** ";
        if (draft.Get(_RequirementState).Length == 0)
        {
            return false;
        }

        if (!remainder.StartsWith(Prefix, StringComparison.Ordinal))
        {
            findings.Add(Rules.Create(Rules.MissingField, Severity.Error, file, draft.Line, draft.Id, "Met"));
            return false;
        }

        ReadOnlySpan<char> mark = remainder[Prefix.Length..].Trim();
        if (!mark.SequenceEqual("⬜") && !mark.SequenceEqual("✅") && !mark.SequenceEqual("❌"))
        {
            findings.Add(Rules.Create(Rules.BadValue, Severity.Error, file, draft.Line, draft.Id, "Met"));
            return false;
        }

        return mark.SequenceEqual("✅");
    }

    /// <summary>Reads the Proof field.</summary>
    /// <param name="draft">The entry.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="findings">Receives findings.</param>
    /// <returns>The proof kind; <see cref="ProofKind.Test"/> when the field is unusable, so a gap is not hidden.</returns>
    private static ProofKind _ReadProof(_Draft draft, string file, List<Finding> findings)
    {
        string raw = draft.Get(_Proof);
        if (raw.Length == 0)
        {
            findings.Add(Rules.Create(Rules.MissingField, Severity.Error, file, draft.Line, draft.Id, "Proof"));
            return ProofKind.Test;
        }

        ReadOnlySpan<char> value = raw;
        if (value.SequenceEqual("test"))
        {
            return ProofKind.Test;
        }

        int separator = value.IndexOf(" — ", StringComparison.Ordinal);
        ReadOnlySpan<char> kind = separator < 0
            ? value
            : value[..separator];
        bool hasReason = separator >= 0 && !value[(separator + 3)..].IsWhiteSpace();
        if (hasReason && kind.SequenceEqual("demo"))
        {
            return ProofKind.Demo;
        }

        if (hasReason && kind.SequenceEqual("review"))
        {
            return ProofKind.Review;
        }

        if (hasReason && kind.SequenceEqual("waived"))
        {
            return ProofKind.Waived;
        }

        findings.Add(Rules.Create(Rules.BadValue, Severity.Error, file, draft.Line, draft.Id, "Proof"));
        return ProofKind.Test;
    }

    /// <summary>Reads <c>- **REQ-…** — text</c>.</summary>
    /// <param name="line">The line.</param>
    /// <param name="id">Receives the ID.</param>
    /// <param name="text">Receives the obligation.</param>
    /// <returns>Whether the line is a well-formed entry start.</returns>
    private static bool _TryReadRequirementStart(ReadOnlySpan<char> line, out string id, out string text)
    {
        id = string.Empty;
        text = string.Empty;
        if (!line.StartsWith("- **", StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> rest = line[4..];
        int close = rest.IndexOf("** — ", StringComparison.Ordinal);
        if (close <= 0 || !SpecId.IsValid(rest[..close], "REQ"))
        {
            return false;
        }

        id = rest[..close].ToString();
        text = rest[(close + 5)..].Trim().ToString();
        return text.Length > 0;
    }

    #endregion

    #region Test cards

    /// <summary>Reads one test-card file.</summary>
    /// <param name="path">Absolute path.</param>
    /// <param name="relativePath">Repository-relative path used in findings.</param>
    /// <param name="cards">Receives the cards.</param>
    /// <param name="findings">Receives reading findings.</param>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static void ReadCards(string path, string relativePath, List<TestCard> cards, List<Finding> findings)
    {
        using StreamReader reader = _Open(path);

        bool hasText = false;
        int cardsBefore = cards.Count;
        _Draft? draft = null;
        int lineNumber = 0;
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            ReadOnlySpan<char> span = line;
            if (span.IsWhiteSpace())
            {
                continue;
            }

            hasText = true;
            if (draft is null && span.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            if (span.StartsWith("### ", StringComparison.Ordinal) && SpecId.IsValid(span[4..].TrimEnd(), "TEST"))
            {
                _FinishCard(draft, relativePath, cards, findings);
                draft = new(span[4..].TrimEnd().ToString(), lineNumber, _CardFields.Length);
                continue;
            }

            if (draft is not null && _TryReadField(span, 0, _CardFields, out int field, out string value))
            {
                draft.Set(field, value);
                continue;
            }

            if (draft is not null && _IsContinuation(span))
            {
                draft.AppendToLast(span.Trim().ToString());
                continue;
            }

            // Text before the first card is an introduction. A malformed card heading still counts there.
            if (draft is null && !_IsNearMiss(span))
            {
                continue;
            }

            findings.Add(_LineFinding(
                span,
                relativePath,
                lineNumber,
                "Write: ### TEST-AREA-TOPIC",
                "Write a field such as \"- **Content:** …\", or move this text above the first card."));
        }

        _FinishCard(draft, relativePath, cards, findings);

        if (hasText && cards.Count == cardsBefore)
        {
            findings.Add(Rules.Create(Rules.NoEntries, Severity.Error, relativePath, 1, string.Empty, "### TEST-AREA-TOPIC"));
        }
    }

    /// <summary>Validates a finished card draft and adds it to the result.</summary>
    /// <param name="draft">The draft, or null when no card is open.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="cards">Receives the card.</param>
    /// <param name="findings">Receives field findings.</param>
    private static void _FinishCard(_Draft? draft, string file, List<TestCard> cards, List<Finding> findings)
    {
        if (draft is null)
        {
            return;
        }

        EntryState state = _ReadState(draft, _CardState, file, findings, out ReadOnlySpan<char> remainder);
        if (state != EntryState.Retired)
        {
            if (draft.Get(_Proves).Length == 0)
            {
                findings.Add(Rules.Create(Rules.MissingField, Severity.Error, file, draft.Line, draft.Id, "Proves"));
            }

            if (draft.Get(_Content).Length == 0)
            {
                findings.Add(Rules.Create(Rules.MissingField, Severity.Error, file, draft.Line, draft.Id, "Content"));
            }

            _CheckStatus(draft, remainder, file, findings);
            _CheckClosedValue(draft, _Class, "Class", "happy|error|boundary|collection|absence|concurrency|trust-boundary", file, findings);
            _CheckClosedValue(draft, _Layer, "Layer", "unit|integration|UI", file, findings);
        }

        string content = SpecText.Collapse(draft.Get(_Content));
        cards.Add(new(draft.Id, _ReadProves(draft.Get(_Proves)), content, state, file, draft.Line));
    }

    /// <summary>Checks the Status mark on the state line of a card.</summary>
    /// <param name="draft">The card.</param>
    /// <param name="remainder">The rest of the state line.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="findings">Receives findings.</param>
    private static void _CheckStatus(_Draft draft, ReadOnlySpan<char> remainder, string file, List<Finding> findings)
    {
        const string Prefix = "**Status:** ";
        if (draft.Get(_CardState).Length == 0)
        {
            return;
        }

        if (!remainder.StartsWith(Prefix, StringComparison.Ordinal))
        {
            findings.Add(Rules.Create(Rules.MissingField, Severity.Error, file, draft.Line, draft.Id, "Status"));
            return;
        }

        ReadOnlySpan<char> mark = remainder[Prefix.Length..].Trim();
        if (!mark.SequenceEqual("⬜") && !mark.SequenceEqual("✅"))
        {
            findings.Add(Rules.Create(Rules.BadValue, Severity.Error, file, draft.Line, draft.Id, "Status"));
        }
    }

    /// <summary>Checks an optional field against a closed set.</summary>
    /// <param name="draft">The card.</param>
    /// <param name="field">Field index.</param>
    /// <param name="name">Field name for the finding.</param>
    /// <param name="allowed">Allowed values separated by <c>|</c>.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="findings">Receives findings.</param>
    private static void _CheckClosedValue(
        _Draft draft,
        int field,
        string name,
        string allowed,
        string file,
        List<Finding> findings)
    {
        string value = draft.Get(field);
        if (value.Length > 0 && Array.IndexOf(allowed.Split('|'), value) < 0)
        {
            findings.Add(Rules.Create(Rules.BadValue, Severity.Error, file, draft.Line, draft.Id, name));
        }
    }

    /// <summary>Splits the Proves field.</summary>
    /// <param name="raw">The field value.</param>
    /// <returns>The requirement IDs; empty for <c>none — reason</c>.</returns>
    private static string[] _ReadProves(string raw)
    {
        if (raw.StartsWith("none — ", StringComparison.Ordinal))
        {
            return [];
        }

        return raw.Split([",", _StateSeparator], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    #endregion

    #region Shared line forms

    /// <summary>Creates the finding for a line that matched no known form.</summary>
    /// <param name="line">The line.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="number">One-based line number.</param>
    /// <param name="idFix">The fix when the line resembles an ID.</param>
    /// <param name="lineFix">The fix otherwise.</param>
    /// <returns>The finding.</returns>
    private static Finding _LineFinding(ReadOnlySpan<char> line, string file, int number, string idFix, string lineFix)
    {
        if (_IsNearMiss(line))
        {
            return Rules.Create(Rules.NearMiss, Severity.Error, file, number, line.Trim().ToString(), idFix);
        }

        return Rules.Create(Rules.Unreadable, Severity.Error, file, number, string.Empty, lineFix);
    }

    /// <summary>Opens a spec file for one forward pass.</summary>
    /// <param name="path">Absolute path.</param>
    /// <returns>A reader over the file.</returns>
    private static StreamReader _Open(string path)
    {
        const FileShare Share = FileShare.ReadWrite | FileShare.Delete;
        FileStream stream = new(path, FileMode.Open, FileAccess.Read, Share, 4096, FileOptions.SequentialScan);
        return new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 16384, leaveOpen: false);
    }

    /// <summary>Reads the state word of a state line.</summary>
    /// <param name="draft">The entry.</param>
    /// <param name="field">Index of the state field.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="findings">Receives findings.</param>
    /// <param name="remainder">Receives the text after the first separator.</param>
    /// <returns>The state; <see cref="EntryState.Active"/> when unusable, so its gaps stay visible.</returns>
    private static EntryState _ReadState(
        _Draft draft,
        int field,
        string file,
        List<Finding> findings,
        out ReadOnlySpan<char> remainder)
    {
        remainder = default;
        ReadOnlySpan<char> value = draft.Get(field);
        if (value.Length == 0)
        {
            findings.Add(Rules.Create(Rules.MissingField, Severity.Error, file, draft.Line, draft.Id, "State"));
            return EntryState.Active;
        }

        int separator = value.IndexOf(_StateSeparator, StringComparison.Ordinal);
        ReadOnlySpan<char> word = separator < 0
            ? value
            : value[..separator];
        if (separator >= 0)
        {
            remainder = value[(separator + _StateSeparator.Length)..];
        }

        if (word.SequenceEqual("active"))
        {
            return EntryState.Active;
        }

        if (word.SequenceEqual("proposed"))
        {
            return EntryState.Proposed;
        }

        if (word.SequenceEqual("retired"))
        {
            return EntryState.Retired;
        }

        findings.Add(Rules.Create(Rules.BadValue, Severity.Error, file, draft.Line, draft.Id, "State"));
        return EntryState.Active;
    }

    /// <summary>Reads <c>- **Name:** value</c> at a given indent.</summary>
    /// <param name="line">The line.</param>
    /// <param name="indent">Required number of leading spaces.</param>
    /// <param name="names">Allowed field names.</param>
    /// <param name="field">Receives the index into <paramref name="names"/>.</param>
    /// <param name="value">Receives the value.</param>
    /// <returns>Whether the line is a known field.</returns>
    private static bool _TryReadField(ReadOnlySpan<char> line, int indent, string[] names, out int field, out string value)
    {
        field = -1;
        value = string.Empty;
        if (line.Length <= indent || line[..indent].IndexOfAnyExcept(' ') >= 0)
        {
            return false;
        }

        ReadOnlySpan<char> body = line[indent..];
        if (!body.StartsWith("- **", StringComparison.Ordinal))
        {
            return false;
        }

        body = body[4..];
        int end = body.IndexOf(":**", StringComparison.Ordinal);
        if (end <= 0)
        {
            return false;
        }

        ReadOnlySpan<char> name = body[..end];
        for (int index = 0; index < names.Length; index++)
        {
            if (name.SequenceEqual(names[index]))
            {
                field = index;
                value = body[(end + 3)..].Trim().ToString();
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns whether a line continues the last field: indented 4 or more spaces, not a bullet.</summary>
    /// <param name="line">The line.</param>
    /// <returns>Whether it is a continuation.</returns>
    private static bool _IsContinuation(ReadOnlySpan<char> line)
    {
        return line.StartsWith("    ", StringComparison.Ordinal) && !line.TrimStart().StartsWith("- ", StringComparison.Ordinal);
    }

    /// <summary>Returns whether a line at an entry position looks like an ID that broke the form.</summary>
    /// <param name="line">The line.</param>
    /// <returns><see langword="true"/> for a bullet or heading that starts with req or test in any case.</returns>
    private static bool _IsNearMiss(ReadOnlySpan<char> line)
    {
        ReadOnlySpan<char> rest = line.TrimStart();
        if (rest.IsEmpty || (rest[0] is not '-' and not '*' and not '#'))
        {
            return false;
        }

        rest = rest.TrimStart("-*#").TrimStart().TrimStart("*_`");
        ReadOnlySpan<char> word = rest.StartsWith("req", StringComparison.OrdinalIgnoreCase)
            ? rest[3..]
            : rest.StartsWith("test", StringComparison.OrdinalIgnoreCase)
                ? rest[4..]
                : default;
        if (word.IsEmpty)
        {
            return false;
        }

        ReadOnlySpan<char> afterSeparator = word[0] is '-' or '_' or ' '
            ? word[1..]
            : word;
        return !afterSeparator.IsEmpty && char.IsAsciiLetterOrDigit(afterSeparator[0]);
    }

    #endregion

    #region Draft

    /// <summary>An entry while it is being read.</summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="line">One-based line of its start.</param>
    /// <param name="fieldCount">Number of fields the entry kind has.</param>
    private sealed class _Draft(string id, int line, int fieldCount)
    {
        /// <summary>Field values by index; null until set.</summary>
        private readonly string?[] _Fields = new string?[fieldCount];

        /// <summary>Index of the field set last; -1 when continuation text belongs to the entry text.</summary>
        private int _Last = -1;

        /// <summary>Gets the entry ID.</summary>
        public string Id { get; } = id;

        /// <summary>Gets the one-based line of the entry start.</summary>
        public int Line { get; } = line;

        /// <summary>Gets or sets the obligation text.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Sets a field.</summary>
        /// <param name="field">Field index.</param>
        /// <param name="value">Value.</param>
        public void Set(int field, string value)
        {
            _Fields[field] = value;
            _Last = field;
        }

        /// <summary>Returns a field value.</summary>
        /// <param name="field">Field index.</param>
        /// <returns>The value, or an empty string when unset.</returns>
        public string Get(int field)
        {
            string? value = _Fields[field];
            return value is null
                ? string.Empty
                : value;
        }

        /// <summary>Appends a wrapped line to the last field, or to the text when no field was set yet.</summary>
        /// <param name="text">The continuation text.</param>
        public void AppendToLast(string text)
        {
            if (_Last < 0)
            {
                Text = Invariant($"{Text} {text}");
                return;
            }

            _Fields[_Last] = Invariant($"{_Fields[_Last]} {text}");
        }
    }

    #endregion
}
