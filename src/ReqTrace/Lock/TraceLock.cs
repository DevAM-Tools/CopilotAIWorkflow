// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>What was accepted for one entry.</summary>
/// <param name="Id">The entry ID.</param>
/// <param name="Hash">Fingerprint of its text.</param>
/// <param name="State">Life-cycle state word.</param>
/// <param name="GapReason">Why a gap was accepted; empty when none was.</param>
/// <param name="Marked">Number of marked tests; cards only.</param>
/// <param name="RequirementStamp">Fingerprints of the proved requirements when last confirmed; cards only.</param>
/// <param name="Note">What a person or reviewer checked; empty when never confirmed.</param>
/// <param name="Text">The accepted text, which lets the report show removed and added words; requirements only.</param>
internal sealed record LockEntry(
    string Id,
    string Hash,
    string State,
    string GapReason,
    int Marked,
    string RequirementStamp,
    string Note,
    string Text);

/// <summary>Reads and writes <c>spec/trace.lock</c>, the accepted state, as sorted text that reviews well in git.</summary>
/// <remarks>Sync: docs/agents.md § Accepted state.</remarks>
internal static class TraceLock
{
    /// <summary>The first line of a lock file.</summary>
    private const string _Header = "# reqtrace lock v1";

    /// <summary>The field that stands for an empty value.</summary>
    private const string _Empty = "-";

    /// <summary>Number of tab-separated columns.</summary>
    private const int _Columns = 8;

    /// <summary>Reads the accepted state.</summary>
    /// <param name="root">Repository root.</param>
    /// <returns>Entries by ID, or null when nothing was accepted yet.</returns>
    /// <exception cref="InvalidDataException">The file is malformed.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static Dictionary<string, LockEntry>? Read(string root)
    {
        string path = _Path(root);
        if (!File.Exists(path))
        {
            return null;
        }

        Dictionary<string, LockEntry> entries = new(StringComparer.Ordinal);
        using StreamReader reader = new(path, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            string[] parts = line.Split('\t');
            if (parts.Length != _Columns
                || !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int marked))
            {
                throw new InvalidDataException(Invariant($"Malformed line in spec/trace.lock: {line}"));
            }

            entries[parts[0]] = new(
                parts[0],
                parts[1],
                parts[2],
                _FromField(parts[3]),
                marked,
                _FromField(parts[5]),
                _FromField(parts[6]),
                _FromField(parts[7]));
        }

        return entries;
    }

    /// <summary>Writes the accepted state atomically, so a crash leaves the previous file intact.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="entries">The entries.</param>
    /// <exception cref="IOException">The file cannot be written.</exception>
    public static void Write(string root, IEnumerable<LockEntry> entries)
    {
        List<LockEntry> sorted = [.. entries];
        sorted.Sort(static (left, right) => string.CompareOrdinal(left.Id, right.Id));

        string path = _Path(root);
        string temporary = path + ".tmp";
        using (StreamWriter writer = new(temporary, false, new UTF8Encoding(false)))
        {
            writer.NewLine = "\n";
            writer.WriteLine(_Header);
            for (int index = 0; index < sorted.Count; index++)
            {
                LockEntry entry = sorted[index];
                string gap = _ToField(entry.GapReason);
                string stamp = _ToField(entry.RequirementStamp);
                string note = _ToField(entry.Note);
                string text = _ToField(entry.Text);
                writer.WriteLine(Invariant($"{entry.Id}\t{entry.Hash}\t{entry.State}\t{gap}\t{entry.Marked}\t{stamp}\t{note}\t{text}"));
            }
        }

        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>Returns the path of the lock file.</summary>
    /// <param name="root">Repository root.</param>
    /// <returns>The path.</returns>
    private static string _Path(string root)
    {
        return Path.Join(root, "spec", "trace.lock");
    }

    /// <summary>Encodes a value as one column; collapsing also removes any tab that would break the format.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The field.</returns>
    private static string _ToField(string value)
    {
        string collapsed = SpecText.Collapse(value);
        return collapsed.Length == 0
            ? _Empty
            : collapsed;
    }

    /// <summary>Decodes a column.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The value.</returns>
    private static string _FromField(string field)
    {
        return string.Equals(field, _Empty, StringComparison.Ordinal)
            ? string.Empty
            : field;
    }
}
