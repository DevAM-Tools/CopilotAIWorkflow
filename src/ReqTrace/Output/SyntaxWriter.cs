// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Prints the spec forms of <see cref="SpecForm"/> for the syntax command.</summary>
/// <remarks>
/// Every example and value comes from <see cref="SpecForm"/>, so the output is what the reader accepts.
/// Stateless; safe from any thread.
/// Sync: docs/agents.md § Commands · .github/skills/tech-reqtrace.md § When to run what (syntax row).
/// </remarks>
/// <seealso cref="SpecForm"/>
internal static class SyntaxWriter
{
    #region Sections

    /// <summary>
    /// Writes the sections of <paramref name="kind"/>; <see cref="SyntaxKind.All"/> writes all four,
    /// one blank line between.
    /// </summary>
    /// <param name="output">Standard output.</param>
    /// <param name="kind">The sections to write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="output"/> is null.</exception>
    public static void Write(TextWriter output, SyntaxKind kind)
    {
        ArgumentNullException.ThrowIfNull(output);

        // Order follows the work: the requirement, then its card, then the test marker, then the ignore list.
        bool all = kind == SyntaxKind.All;
        bool written = false;
        if (all || kind == SyntaxKind.Requirement)
        {
            _WriteRequirement(output);
            written = true;
        }

        if (all || kind == SyntaxKind.Card)
        {
            _Separate(output, written);
            _WriteCard(output);
            written = true;
        }

        if (all || kind == SyntaxKind.Marker)
        {
            _Separate(output, written);
            _WriteMarker(output);
            written = true;
        }

        if (all || kind == SyntaxKind.Ignore)
        {
            _Separate(output, written);
            _WriteIgnore(output);
        }
    }

    /// <summary>Writes the requirement section: where it lives, the example, required fields, and every value.</summary>
    /// <param name="output">Standard output.</param>
    private static void _WriteRequirement(TextWriter output)
    {
        output.WriteLine("requirement — spec/requirements/<area>.md, below a \"### <Area>\" heading");
        _WriteLines(output, SpecForm.RequirementExample);
        output.WriteLine("Required: Done when, State with Met, Proof (a retired entry needs only State)");
        output.WriteLine("Optional: Built in, Replaced by");
        output.WriteLine(Invariant($"State: {SpecForm.StateValues}"));
        output.WriteLine(Invariant($"Met: {SpecForm.MetValues}"));
        output.WriteLine(Invariant($"Proof: {SpecForm.ProofValues}"));
        output.WriteLine("ID: REQ- and 2 to 4 tokens of A-Z and 0-9, each with a letter, joined by single hyphens");
        output.WriteLine("Fields are indented 2 spaces; a wrapped line is indented 4 or more.");
        output.WriteLine(
            "Prose may stand above the first ### heading and under \"## User stories\"; "
            + "retired entries go under \"## Retired\".");
    }

    /// <summary>Writes the card section: where it lives, the example, required fields, and every value.</summary>
    /// <param name="output">Standard output.</param>
    private static void _WriteCard(TextWriter output)
    {
        output.WriteLine("card — spec/tests/<area>.md");
        _WriteLines(output, SpecForm.CardExample);
        output.WriteLine("Required: Proves, Content, State with Status (a retired card needs only State)");
        output.WriteLine("Optional: Class, Layer, Out");
        output.WriteLine(Invariant($"Proves: one or more REQ- IDs separated by \", \", or {SpecForm.ProvesNone}{{reason}}"));
        output.WriteLine(Invariant($"State: {SpecForm.StateValues}"));
        output.WriteLine(Invariant($"Status: {SpecForm.StatusValues}"));
        output.WriteLine(Invariant($"Class: {SpecForm.ClassValues}"));
        output.WriteLine(Invariant($"Layer: {SpecForm.LayerValues}"));
        output.WriteLine("ID: TEST- and 2 to 4 tokens, as for REQ-. Fields start at column 0.");
    }

    /// <summary>Writes the marker section: one line per language.</summary>
    /// <param name="output">Standard output.</param>
    /// <remarks>Sync: MarkerScanner._MethodWindow (the 8 lines in the text).</remarks>
    private static void _WriteMarker(TextWriter output)
    {
        output.WriteLine(
            "marker — one per test that proves a card, at most 8 lines above the test method; "
            + "never in production code");
        output.WriteLine(Invariant($"C#:     {SpecForm.CSharpMarker}"));
        output.WriteLine(Invariant($"Python: {SpecForm.PythonMarker}"));
        output.WriteLine(Invariant($"Rust:   {SpecForm.RustMarker}"));
    }

    /// <summary>Writes the ignore section: the file, its rules, and one example line.</summary>
    /// <param name="output">Standard output.</param>
    private static void _WriteIgnore(TextWriter output)
    {
        output.WriteLine("ignore — spec/trace.ignore, one path per line, reason required, path inside the repository");
        output.WriteLine(SpecForm.IgnoreExample);
    }

    /// <summary>Writes a multi-line example line by line, so every line ends with the writer's own line ending.</summary>
    /// <param name="output">Standard output.</param>
    /// <param name="text">Lines joined by <c>\n</c>.</param>
    /// <remarks>Perf: enumerates spans, so no line string is allocated.</remarks>
    private static void _WriteLines(TextWriter output, string text)
    {
        foreach (ReadOnlySpan<char> line in text.AsSpan().EnumerateLines())
        {
            output.WriteLine(line);
        }
    }

    /// <summary>Writes the blank line between two sections.</summary>    /// <param name="output">Standard output.</param>
    /// <param name="written">Whether a section precedes this one.</param>
    private static void _Separate(TextWriter output, bool written)
    {
        if (written)
        {
            output.WriteLine();
        }
    }

    #endregion
}
