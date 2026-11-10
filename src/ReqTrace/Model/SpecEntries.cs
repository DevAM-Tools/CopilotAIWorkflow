// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Life-cycle state of a requirement or test card.</summary>
internal enum EntryState
{
    /// <summary>Written, not yet approved at a gate.</summary>
    Proposed,

    /// <summary>In force.</summary>
    Active,

    /// <summary>No longer in force; the ID stays reserved.</summary>
    Retired,
}

/// <summary>How a requirement is shown to hold.</summary>
internal enum ProofKind
{
    /// <summary>An automated test.</summary>
    Test,

    /// <summary>A person runs it once and observes the result.</summary>
    Demo,

    /// <summary>A person judges it by reading or looking.</summary>
    Review,

    /// <summary>Deliberately not proven.</summary>
    Waived,
}

/// <summary>One requirement as read from the spec.</summary>
/// <param name="Id">The stable identifier.</param>
/// <param name="Text">The obligation, whitespace collapsed.</param>
/// <param name="DoneWhen">The runnable check, whitespace collapsed; empty when the entry is retired.</param>
/// <param name="State">Life-cycle state.</param>
/// <param name="IsMet">Whether Done-when is ticked as having held.</param>
/// <param name="Proof">How the requirement is shown to hold.</param>
/// <param name="ProofText">The Proof field as written, including any reason.</param>
/// <param name="File">Repository-relative path.</param>
/// <param name="Line">One-based line of the entry start.</param>
internal sealed record Requirement(
    string Id,
    string Text,
    string DoneWhen,
    EntryState State,
    bool IsMet,
    ProofKind Proof,
    string ProofText,
    string File,
    int Line);

/// <summary>One test card as read from the spec.</summary>
/// <param name="Id">The stable identifier.</param>
/// <param name="Proves">Requirement IDs the card proves; empty for <c>none — reason</c>.</param>
/// <param name="Content">The scenario, whitespace collapsed.</param>
/// <param name="State">Life-cycle state.</param>
/// <param name="File">Repository-relative path.</param>
/// <param name="Line">One-based line of the card heading.</param>
internal sealed record TestCard(string Id, IReadOnlyList<string> Proves, string Content, EntryState State, string File, int Line);

/// <summary>One marker found on a test.</summary>
/// <param name="CardId">The ID the marker names, exactly as written.</param>
/// <param name="File">Repository-relative path.</param>
/// <param name="Line">One-based line of the marker.</param>
/// <param name="Method">The test method it precedes; null when none was found within the window.</param>
internal sealed record Marker(string CardId, string File, int Line, string? Method);

/// <summary>Everything read from the repository in one run.</summary>
/// <param name="Requirements">All requirements, in file order.</param>
/// <param name="Cards">All test cards, in file order.</param>
/// <param name="Markers">All markers, ordered by path and line.</param>
/// <param name="IgnoredPaths">Ignore-file entries as path and reason.</param>
/// <param name="Findings">Findings raised while reading.</param>
internal sealed record TraceSnapshot(
    IReadOnlyList<Requirement> Requirements,
    IReadOnlyList<TestCard> Cards,
    IReadOnlyList<Marker> Markers,
    IReadOnlyList<KeyValuePair<string, string>> IgnoredPaths,
    IReadOnlyList<Finding> Findings);

/// <summary>Text helpers shared by readers and the accepted state.</summary>
internal static class SpecText
{
    /// <summary>Collapses runs of whitespace to one space and trims, so re-wrapping a line is not a change.</summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    public static string Collapse(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Returns the fingerprint of already-collapsed text.</summary>
    /// <param name="text">The collapsed text.</param>
    /// <returns>The first 12 hexadecimal characters of its SHA-256, lowercase.</returns>
    public static string Fingerprint(string text)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(text), digest);
        return Convert.ToHexStringLower(digest[..6]);
    }

    /// <summary>Returns what a requirement obliges, as the one line the accepted state stores and the report compares.</summary>
    /// <param name="requirement">The requirement.</param>
    /// <returns>Obligation and Done-when.</returns>
    public static string Obligation(Requirement requirement)
    {
        return Invariant($"{requirement.Text} | {requirement.DoneWhen}");
    }

    /// <summary>Returns the fingerprint of what a requirement obliges.</summary>
    /// <param name="requirement">The requirement.</param>
    /// <returns>The fingerprint of its text and Done-when.</returns>
    public static string Fingerprint(Requirement requirement)
    {
        return Fingerprint(Obligation(requirement));
    }

    /// <summary>Returns the fingerprint of what a card claims.</summary>
    /// <param name="card">The card.</param>
    /// <returns>The fingerprint of its requirement list and content.</returns>
    public static string Fingerprint(TestCard card)
    {
        return Fingerprint(Invariant($"{string.Join(',', card.Proves)} | {card.Content}"));
    }
}

/// <summary>Validates the strict ID form.</summary>
internal static class SpecId
{
    /// <summary>Returns whether <paramref name="text"/> is a prefix plus two to four tokens.</summary>
    /// <param name="text">The candidate.</param>
    /// <param name="prefix">The expected prefix, such as <c>REQ</c>.</param>
    /// <returns><see langword="true"/> when every token is uppercase letters and digits and holds a letter.</returns>
    public static bool IsValid(ReadOnlySpan<char> text, ReadOnlySpan<char> prefix)
    {
        if (!text.StartsWith(prefix, StringComparison.Ordinal) || text.Length <= prefix.Length || text[prefix.Length] != '-')
        {
            return false;
        }

        ReadOnlySpan<char> rest = text[(prefix.Length + 1)..];
        int tokens = 0;
        while (true)
        {
            int dash = rest.IndexOf('-');
            ReadOnlySpan<char> token = dash < 0
                ? rest
                : rest[..dash];
            if (!_IsToken(token))
            {
                return false;
            }

            tokens++;
            if (dash < 0)
            {
                break;
            }

            rest = rest[(dash + 1)..];
        }

        return tokens is >= 2 and <= 4;
    }

    /// <summary>Returns whether a token is non-empty, uppercase letters and digits only, with at least one letter.</summary>
    /// <param name="token">The token.</param>
    /// <returns>Whether the token is valid.</returns>
    private static bool _IsToken(ReadOnlySpan<char> token)
    {
        bool hasLetter = false;
        for (int index = 0; index < token.Length; index++)
        {
            char character = token[index];
            if (character is >= 'A' and <= 'Z')
            {
                hasLetter = true;
            }
            else if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return hasLetter;
    }
}
