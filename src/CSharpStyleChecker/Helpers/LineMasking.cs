// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace CSharpStyleChecker.Helpers;

/// <summary>
/// Kind of string or character literal left open at the end of a line.
/// No mutable state; safe to read from concurrent analyzer actions.
/// Use a fresh <see cref="LineMaskState"/> per scan; do not share one mutable holder across threads.
/// </summary>
internal enum LineStringKind
{
    /// <summary>No open literal.</summary>
    None = 0,

    /// <summary>Regular <c>"..."</c> (not interpolated).</summary>
    Regular = 1,

    /// <summary>Verbatim <c>@"..."</c> (not interpolated).</summary>
    Verbatim = 2,

    /// <summary>Raw string with three or more quotes.</summary>
    Raw = 3,

    /// <summary>Character literal <c>'...'</c>.</summary>
    CharacterLiteral = 4,

    /// <summary>Interpolated <c>$"..."</c>; holes count toward length.</summary>
    Interpolated = 5,

    /// <summary>Interpolated verbatim <c>@$"..."</c> / <c>$@"..."</c>; holes count toward length.</summary>
    InterpolatedVerbatim = 6,
}

/// <summary>
/// Cross-line state for line-length masking.
/// Value type; each caller keeps its own copy. No shared mutable state; concurrent calls with distinct states are safe.
/// </summary>
internal readonly struct LineMaskState
{
    /// <summary>Creates mask state for the next line.</summary>
    public LineMaskState(bool inBlockComment, LineStringKind openString, int rawDelimiterLength, int interpolationDepth = 0)
    {
        InBlockComment = inBlockComment;
        OpenString = openString;
        RawDelimiterLength = rawDelimiterLength;
        InterpolationDepth = interpolationDepth;
    }

    /// <summary>Whether a <c>/*</c> block comment continues into the next line.</summary>
    public bool InBlockComment { get; }

    /// <summary>Open string or character literal kind, if any.</summary>
    public LineStringKind OpenString { get; }

    /// <summary>Quote count that opened a raw string; used to find the matching close.</summary>
    public int RawDelimiterLength { get; }

    /// <summary>
    /// Brace depth inside an interpolated string hole. Zero means literal payload (not counted);
    /// greater than zero means hole code (counted).
    /// </summary>
    public int InterpolationDepth { get; }
}

/// <summary>
/// Masks string and character literal content for line-length measurement.
/// Comments count toward length; quotes inside comments do not open strings.
/// Interpolation holes count as code. No mutable state; safe to read from concurrent analyzer actions.
/// </summary>
internal static class LineMasking
{
    #region Public API

    /// <summary>
    /// Masks string and character literal content; comments and interpolation holes remain and count toward length.
    /// </summary>
    /// <param name="line">The source line.</param>
    /// <param name="state">State carried from the previous line.</param>
    /// <returns>Masked line and updated state for the next line.</returns>
    public static (string Masked, LineMaskState State) MaskLine(string line, LineMaskState state)
    {
        if (line is null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        StringBuilder masked = new StringBuilder(line.Length);
        LineMaskState updated = state;
        SourceReader reader = new SourceReader(line);
        _ = Scan(reader, line.Length, ref updated, masked);
        return (masked.ToString(), updated);
    }

    #endregion

    #region Private helpers

    /// <summary>
    /// Counts visible length for one <see cref="TextLine"/> without allocating a masked string.
    /// String and character content does not count; comments, code, and interpolation holes do. Trailing whitespace is trimmed.
    /// </summary>
    internal static int MeasureVisibleLength(SourceText text, TextLine textLine, ref LineMaskState state)
    {
        int length = textLine.End - textLine.Start;
        SourceReader reader = new SourceReader(text, textLine.Start);
        return Scan(reader, length, ref state, builder: null);
    }

    /// <summary>
    /// Shared scan: <paramref name="builder"/> null means count only.
    /// Append means: write when building, always advance length, track last non-whitespace for TrimEnd.
    /// </summary>
    private static int Scan(SourceReader reader, int length, ref LineMaskState state, StringBuilder builder)
    {
        int counted = 0;
        int trimmed = 0;

        for (int i = 0; i < length; i++)
        {
            if (state.OpenString == LineStringKind.Raw)
            {
                i = ScanRawContent(reader, length, i, ref state, builder, ref counted, ref trimmed);
                continue;
            }

            if (state.OpenString == LineStringKind.Verbatim)
            {
                i = ScanVerbatimContent(reader, length, i, ref state, builder, ref counted, ref trimmed);
                continue;
            }

            if (state.OpenString == LineStringKind.Interpolated || state.OpenString == LineStringKind.InterpolatedVerbatim)
            {
                i = ScanInterpolatedContent(reader, length, i, ref state, builder, ref counted, ref trimmed);
                continue;
            }

            if (state.OpenString == LineStringKind.Regular || state.OpenString == LineStringKind.CharacterLiteral)
            {
                i = ScanRegularOrCharContent(reader, length, i, ref state, builder, ref counted, ref trimmed);
                continue;
            }

            if (state.InBlockComment)
            {
                i = ScanBlockCommentContent(reader, length, i, ref state, builder, ref counted, ref trimmed);
                continue;
            }

            char current = reader[i];
            if (current == '/' && i + 1 < length && reader[i + 1] == '/')
            {
                // Line comments count; do not reopen strings from comment text.
                AppendRange(reader, i, length - i, builder, ref counted, ref trimmed);
                break;
            }

            if (current == '/' && i + 1 < length && reader[i + 1] == '*')
            {
                AppendChar('/', builder, ref counted, ref trimmed);
                AppendChar('*', builder, ref counted, ref trimmed);
                state = new LineMaskState(inBlockComment: true, LineStringKind.None, rawDelimiterLength: 0);
                i++;
                continue;
            }

            if (TryOpenString(reader, length, ref i, ref state, builder, ref counted, ref trimmed))
            {
                continue;
            }

            AppendChar(current, builder, ref counted, ref trimmed);
        }

        return trimmed;
    }

    private static int ScanRawContent(
        SourceReader reader,
        int length,
        int index,
        ref LineMaskState state,
        StringBuilder builder,
        ref int counted,
        ref int trimmed)
    {
        int delimiter = state.RawDelimiterLength;
        int quoteRun = 0;
        while (index + quoteRun < length && reader[index + quoteRun] == '"')
        {
            quoteRun++;
        }

        if (quoteRun == delimiter)
        {
            AppendRange(reader, index, delimiter, builder, ref counted, ref trimmed);
            state = new LineMaskState(state.InBlockComment, LineStringKind.None, rawDelimiterLength: 0);
            return index + delimiter - 1;
        }

        // Shorter or longer quote runs inside content do not close; content is not counted.
        return index;
    }

    private static int ScanVerbatimContent(
        SourceReader reader,
        int length,
        int index,
        ref LineMaskState state,
        StringBuilder builder,
        ref int counted,
        ref int trimmed)
    {
        char ch = reader[index];
        if (ch == '"' && index + 1 < length && reader[index + 1] == '"')
        {
            // Escaped doubled quote stays inside the literal.
            return index + 1;
        }

        if (ch == '"')
        {
            AppendChar('"', builder, ref counted, ref trimmed);
            state = new LineMaskState(state.InBlockComment, LineStringKind.None, rawDelimiterLength: 0);
            return index;
        }

        return index;
    }

    private static int ScanInterpolatedContent(
        SourceReader reader,
        int length,
        int index,
        ref LineMaskState state,
        StringBuilder builder,
        ref int counted,
        ref int trimmed)
    {
        char ch = reader[index];
        bool verbatim = state.OpenString == LineStringKind.InterpolatedVerbatim;
        int depth = state.InterpolationDepth;

        if (depth > 0)
        {
            // Hole code counts toward visible length.
            AppendChar(ch, builder, ref counted, ref trimmed);
            if (ch == '{')
            {
                state = new LineMaskState(state.InBlockComment, state.OpenString, rawDelimiterLength: 0, depth + 1);
            }
            else if (ch == '}')
            {
                state = new LineMaskState(state.InBlockComment, state.OpenString, rawDelimiterLength: 0, depth - 1);
            }

            return index;
        }

        // Literal payload of an interpolated string: not counted, except hole delimiters we keep for mask shape.
        if (ch == '{' && index + 1 < length && reader[index + 1] == '{')
        {
            return index + 1;
        }

        if (ch == '}' && index + 1 < length && reader[index + 1] == '}')
        {
            return index + 1;
        }

        if (ch == '{')
        {
            AppendChar('{', builder, ref counted, ref trimmed);
            state = new LineMaskState(state.InBlockComment, state.OpenString, rawDelimiterLength: 0, interpolationDepth: 1);
            return index;
        }

        if (verbatim)
        {
            if (ch == '"' && index + 1 < length && reader[index + 1] == '"')
            {
                return index + 1;
            }

            if (ch == '"')
            {
                AppendChar('"', builder, ref counted, ref trimmed);
                state = new LineMaskState(state.InBlockComment, LineStringKind.None, rawDelimiterLength: 0);
                return index;
            }

            return index;
        }

        if (ch == '\\')
        {
            return index + 1 < length ? index + 1 : index;
        }

        if (ch == '"')
        {
            AppendChar('"', builder, ref counted, ref trimmed);
            state = new LineMaskState(state.InBlockComment, LineStringKind.None, rawDelimiterLength: 0);
            return index;
        }

        return index;
    }

    private static int ScanRegularOrCharContent(
        SourceReader reader,
        int length,
        int index,
        ref LineMaskState state,
        StringBuilder builder,
        ref int counted,
        ref int trimmed)
    {
        char ch = reader[index];
        char closer = state.OpenString == LineStringKind.CharacterLiteral ? '\'' : '"';

        if (ch == '\\')
        {
            // Same as before: a trailing backslash consumes nothing further on this line.
            return index + 1 < length ? index + 1 : index;
        }

        if (ch == closer)
        {
            AppendChar(ch, builder, ref counted, ref trimmed);
            state = new LineMaskState(state.InBlockComment, LineStringKind.None, rawDelimiterLength: 0);
            return index;
        }

        return index;
    }

    private static int ScanBlockCommentContent(
        SourceReader reader,
        int length,
        int index,
        ref LineMaskState state,
        StringBuilder builder,
        ref int counted,
        ref int trimmed)
    {
        char ch = reader[index];
        if (ch == '*' && index + 1 < length && reader[index + 1] == '/')
        {
            AppendChar('*', builder, ref counted, ref trimmed);
            AppendChar('/', builder, ref counted, ref trimmed);
            state = new LineMaskState(inBlockComment: false, LineStringKind.None, rawDelimiterLength: 0);
            return index + 1;
        }

        AppendChar(ch, builder, ref counted, ref trimmed);
        return index;
    }

    /// <summary>
    /// Opens verbatim, raw, regular, interpolated, or character literals. Leaves <paramref name="index"/> on the last consumed char.
    /// </summary>
    private static bool TryOpenString(
        SourceReader reader,
        int length,
        ref int index,
        ref LineMaskState state,
        StringBuilder builder,
        ref int counted,
        ref int trimmed)
    {
        char current = reader[index];

        if (current == '@')
        {
            char next = index + 1 < length ? reader[index + 1] : '\0';
            if (next == '"')
            {
                // @"..." including @""" stays verbatim, not raw.
                AppendChar('@', builder, ref counted, ref trimmed);
                AppendChar('"', builder, ref counted, ref trimmed);
                state = new LineMaskState(inBlockComment: false, LineStringKind.Verbatim, rawDelimiterLength: 0);
                index++;
                return true;
            }

            if (next == '$' && index + 2 < length && reader[index + 2] == '"')
            {
                AppendChar('@', builder, ref counted, ref trimmed);
                AppendChar('$', builder, ref counted, ref trimmed);
                AppendChar('"', builder, ref counted, ref trimmed);
                state = new LineMaskState(inBlockComment: false, LineStringKind.InterpolatedVerbatim, rawDelimiterLength: 0);
                index += 2;
                return true;
            }
        }

        if (current == '$')
        {
            int dollarCount = 0;
            while (index + dollarCount < length && reader[index + dollarCount] == '$')
            {
                dollarCount++;
            }

            int quoteStart = index + dollarCount;
            int quoteCount = CountQuotes(reader, length, quoteStart);
            if (quoteCount >= 3)
            {
                AppendRange(reader, index, dollarCount + quoteCount, builder, ref counted, ref trimmed);
                state = new LineMaskState(inBlockComment: false, LineStringKind.Raw, quoteCount);
                index = quoteStart + quoteCount - 1;
                return true;
            }

            if (dollarCount == 1 && quoteCount == 1)
            {
                // $"..." — interpolation holes count as code.
                AppendChar('$', builder, ref counted, ref trimmed);
                AppendChar('"', builder, ref counted, ref trimmed);
                state = new LineMaskState(inBlockComment: false, LineStringKind.Interpolated, rawDelimiterLength: 0);
                index++;
                return true;
            }

            if (dollarCount == 1
                && quoteStart + 1 < length
                && reader[quoteStart] == '@'
                && reader[quoteStart + 1] == '"')
            {
                // $@"..."
                AppendChar('$', builder, ref counted, ref trimmed);
                AppendChar('@', builder, ref counted, ref trimmed);
                AppendChar('"', builder, ref counted, ref trimmed);
                state = new LineMaskState(inBlockComment: false, LineStringKind.InterpolatedVerbatim, rawDelimiterLength: 0);
                index = quoteStart + 1;
                return true;
            }
        }

        if (current == '"')
        {
            int quoteCount = CountQuotes(reader, length, index);
            if (quoteCount >= 3)
            {
                AppendRange(reader, index, quoteCount, builder, ref counted, ref trimmed);
                state = new LineMaskState(inBlockComment: false, LineStringKind.Raw, quoteCount);
                index += quoteCount - 1;
                return true;
            }

            AppendChar('"', builder, ref counted, ref trimmed);
            state = new LineMaskState(inBlockComment: false, LineStringKind.Regular, rawDelimiterLength: 0);
            return true;
        }

        if (current == '\'')
        {
            AppendChar('\'', builder, ref counted, ref trimmed);
            state = new LineMaskState(inBlockComment: false, LineStringKind.CharacterLiteral, rawDelimiterLength: 0);
            return true;
        }

        return false;
    }

    private static int CountQuotes(SourceReader reader, int length, int start)
    {
        int count = 0;
        while (start + count < length && reader[start + count] == '"')
        {
            count++;
        }

        return count;
    }

    private static void AppendChar(char ch, StringBuilder builder, ref int counted, ref int trimmed)
    {
        if (builder is not null)
        {
            builder.Append(ch);
        }

        counted++;
        if (!char.IsWhiteSpace(ch))
        {
            trimmed = counted;
        }
    }

    private static void AppendRange(
        SourceReader reader,
        int start,
        int count,
        StringBuilder builder,
        ref int counted,
        ref int trimmed)
    {
        for (int offset = 0; offset < count; offset++)
        {
            AppendChar(reader[start + offset], builder, ref counted, ref trimmed);
        }
    }

    /// <summary>Reads either a string line or a slice of <see cref="SourceText"/> without copying.</summary>
    private readonly struct SourceReader
    {
        private readonly string _line;
        private readonly SourceText _text;
        private readonly int _baseIndex;

        public SourceReader(string line)
        {
            _line = line;
            _text = null;
            _baseIndex = 0;
        }

        public SourceReader(SourceText text, int start)
        {
            _line = null;
            _text = text;
            _baseIndex = start;
        }

        public char this[int index] => _line is not null ? _line[index] : _text[_baseIndex + index];
    }

    #endregion
}
