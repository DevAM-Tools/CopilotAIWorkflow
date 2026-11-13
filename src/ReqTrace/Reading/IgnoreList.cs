// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Paths the scan skips, each with a stated reason, so an exception is visible instead of hidden.</summary>
/// <remarks>Sync: docs/agents.md § Ignoring a path.</remarks>
internal sealed class IgnoreList
{
    /// <summary>The ignored path prefixes, normalized to forward slashes.</summary>
    private readonly string[] _Prefixes;

    /// <summary>Creates the list.</summary>
    /// <param name="entries">Path and reason per ignored path.</param>
    private IgnoreList(List<KeyValuePair<string, string>> entries)
    {
        Entries = entries;
        _Prefixes = new string[entries.Count];
        for (int index = 0; index < entries.Count; index++)
        {
            _Prefixes[index] = entries[index].Key;
        }
    }

    /// <summary>Gets path and reason per ignored path.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Entries { get; }

    /// <summary>Gets a value indicating whether nothing is ignored, so callers can skip building relative paths.</summary>
    public bool IsEmpty => _Prefixes.Length == 0;

    /// <summary>Reads <c>spec/trace.ignore</c>; a missing file means nothing is ignored.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="findings">Receives a finding per invalid line.</param>
    /// <returns>The list.</returns>
    /// <exception cref="IOException">The file exists and cannot be read.</exception>
    public static IgnoreList Read(string root, List<Finding> findings)
    {
        const string Source = "spec/trace.ignore";
        List<KeyValuePair<string, string>> entries = [];
        string path = Path.Join(root, "spec", "trace.ignore");
        if (!File.Exists(path))
        {
            return new(entries);
        }

        using StreamReader reader = new(path, Encoding.UTF8);
        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            int separator = trimmed.IndexOf(SpecForm.ReasonSeparator, StringComparison.Ordinal);
            string reason = separator < 0
                ? string.Empty
                : trimmed[(separator + SpecForm.ReasonSeparator.Length)..].Trim();
            if (reason.Length == 0)
            {
                findings.Add(Rules.Create(Rules.BadIgnore, Severity.Error, Source, lineNumber, trimmed, "a reason is required"));
                continue;
            }

            string normalized = trimmed[..separator].Trim().Replace('\\', '/').TrimEnd('/');

            // Security: the list only narrows a scan of the repository. A path that leaves the root could widen
            // what later tooling trusts, so rooted paths and parent segments are refused rather than resolved.
            if (normalized.Length == 0 || Path.IsPathRooted(normalized) || _HasParentSegment(normalized))
            {
                findings.Add(Rules.Create(Rules.BadIgnore, Severity.Error, Source, lineNumber, trimmed, "the path must stay inside the repository"));
                continue;
            }

            entries.Add(new(normalized, reason));
        }

        return new(entries);
    }

    /// <summary>Returns whether a repository-relative path is at or below an ignored path.</summary>
    /// <param name="relativePath">Path with forward slashes.</param>
    /// <returns>Whether the path is ignored.</returns>
    public bool IsIgnored(ReadOnlySpan<char> relativePath)
    {
        for (int index = 0; index < _Prefixes.Length; index++)
        {
            string prefix = _Prefixes[index];
            if (relativePath.StartsWith(prefix, StringComparison.Ordinal)
                && (relativePath.Length == prefix.Length || relativePath[prefix.Length] == '/'))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns whether a path holds a <c>..</c> segment.</summary>
    /// <param name="path">Path with forward slashes.</param>
    /// <returns>Whether it does.</returns>
    private static bool _HasParentSegment(string path)
    {
        foreach (Range segment in path.AsSpan().Split('/'))
        {
            if (path.AsSpan(segment).SequenceEqual(".."))
            {
                return true;
            }
        }

        return false;
    }
}
