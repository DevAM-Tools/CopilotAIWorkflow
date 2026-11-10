// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Finds test markers in C#, Python, and Rust source files.</summary>
/// <remarks>
/// Perf: this is the hot path, because it visits every source file of the repository on every run. Nearly all files hold
/// no marker, so a vectorized byte search over a pooled buffer rejects them before any string or line exists.
/// Sync: .github/skills/tech-spec.md § Trace (the three marker forms).
/// </remarks>
internal static class MarkerScanner
{
    #region Configuration

    /// <summary>How many lines after a marker may hold its method; other attributes can sit between them.</summary>
    private const int _MethodWindow = 8;

    /// <summary>Size of the pooled read buffer.</summary>
    private const int _BufferSize = 65536;

    /// <summary>Below this many files, starting worker threads costs more than it saves.</summary>
    private const int _ParallelThreshold = 128;

    #endregion

    #region Repository walk

    /// <summary>Finds every marker below a root.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="ignore">Paths to skip.</param>
    /// <returns>All markers, ordered by path and line.</returns>
    /// <exception cref="IOException">A file cannot be read.</exception>
    public static List<Marker> Scan(string root, IgnoreList ignore)
    {
        List<string> files = _FindSourceFiles(root, ignore);
        files.Sort(StringComparer.Ordinal);

        // Each worker writes only its own slot, so results need no synchronization and keep the sorted order.
        Marker[]?[] results = new Marker[]?[files.Count];
        if (files.Count < _ParallelThreshold)
        {
            for (int index = 0; index < files.Count; index++)
            {
                results[index] = _ScanFile(root, files[index]);
            }
        }
        else
        {
            try
            {
                Parallel.For(0, files.Count, index => results[index] = _ScanFile(root, files[index]));
            }
            catch (AggregateException exception) when (exception.InnerException is IOException or UnauthorizedAccessException)
            {
                // The parallel loop wraps failures; callers handle input errors as IOException.
                throw new IOException(exception.InnerException.Message, exception.InnerException);
            }
        }

        List<Marker> markers = [];
        for (int index = 0; index < results.Length; index++)
        {
            Marker[]? found = results[index];
            if (found is not null)
            {
                markers.AddRange(found);
            }
        }

        return markers;
    }

    /// <summary>Lists candidate files, never entering build output.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="ignore">Paths to skip.</param>
    /// <returns>Absolute paths.</returns>
    private static List<string> _FindSourceFiles(string root, IgnoreList ignore)
    {
        // Security: reparse points are skipped so that a symbolic link cannot lead the scan outside the repository.
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            ReturnSpecialDirectories = false,
        };

        FileSystemEnumerable<string> entries = new(
            root,
            static (ref FileSystemEntry entry) => entry.ToFullPath(),
            options)
        {
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                !_IsSkippedDirectory(entry.FileName) && !_IsIgnored(ref entry, root, ignore),
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                !entry.IsDirectory && _LanguageOf(entry.FileName) != _Language.None && !_IsIgnored(ref entry, root, ignore),
        };

        return [.. entries];
    }

    /// <summary>Returns whether an entry lies below an ignored path.</summary>
    /// <param name="entry">The entry.</param>
    /// <param name="root">Repository root.</param>
    /// <param name="ignore">Ignored paths.</param>
    /// <returns>Whether to skip it.</returns>
    private static bool _IsIgnored(ref FileSystemEntry entry, string root, IgnoreList ignore)
    {
        if (ignore.IsEmpty)
        {
            return false;
        }

        return ignore.IsIgnored(_Relative(root, entry.ToFullPath()));
    }

    /// <summary>Returns whether a directory is build output or tool state.</summary>
    /// <param name="name">Directory name.</param>
    /// <returns>Whether to skip it.</returns>
    private static bool _IsSkippedDirectory(ReadOnlySpan<char> name)
    {
        return name.SequenceEqual("bin")
            || name.SequenceEqual("obj")
            || name.SequenceEqual("target")
            || name.SequenceEqual("node_modules")
            || name.SequenceEqual(".git");
    }

    /// <summary>Converts an absolute path to a repository-relative one with forward slashes.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="fullPath">Absolute path below the root.</param>
    /// <returns>The relative path.</returns>
    private static string _Relative(string root, string fullPath)
    {
        return fullPath[(root.TrimEnd(Path.DirectorySeparatorChar).Length + 1)..].Replace('\\', '/');
    }

    #endregion

    #region File scan

    /// <summary>The language of a source file, which fixes its marker and method forms.</summary>
    private enum _Language
    {
        /// <summary>Not a source file the scan reads.</summary>
        None,

        /// <summary>C#.</summary>
        CSharp,

        /// <summary>Python.</summary>
        Python,

        /// <summary>Rust.</summary>
        Rust,
    }

    /// <summary>Maps a file name to its language.</summary>
    /// <param name="name">File name.</param>
    /// <returns>The language.</returns>
    private static _Language _LanguageOf(ReadOnlySpan<char> name)
    {
        if (name.EndsWith(".cs", StringComparison.Ordinal))
        {
            return _Language.CSharp;
        }

        if (name.EndsWith(".py", StringComparison.Ordinal))
        {
            return _Language.Python;
        }

        return name.EndsWith(".rs", StringComparison.Ordinal)
            ? _Language.Rust
            : _Language.None;
    }

    /// <summary>Scans one file.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="path">Absolute path.</param>
    /// <returns>Its markers, or null when it has none.</returns>
    private static Marker[]? _ScanFile(string root, string path)
    {
        _Language language = _LanguageOf(path);
        ReadOnlySpan<byte> literal = language switch
        {
            _Language.CSharp => "\"Card\""u8,
            _Language.Python => "pytest.mark.card("u8,
            _ => "card:"u8,
        };

        const FileShare Share = FileShare.ReadWrite | FileShare.Delete;
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, Share, 0, FileOptions.SequentialScan);
            if (!_Contains(stream, literal))
            {
                return null;
            }

            // A hit is rare, so the second pass may be simple: one line at a time, with strings.
            stream.Position = 0;
            return _ReadMarkers(stream, _Relative(root, path), language);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Other agents may work in this repository. A file deleted after listing simply has no markers.
            return null;
        }
    }

    /// <summary>Searches a stream for a byte sequence without loading the file.</summary>
    /// <param name="stream">The stream, positioned at the start.</param>
    /// <param name="literal">The bytes to find.</param>
    /// <returns>Whether they occur.</returns>
    private static bool _Contains(FileStream stream, ReadOnlySpan<byte> literal)
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(_BufferSize);
        try
        {
            Span<byte> buffer = rented;
            int carried = 0;
            while (true)
            {
                int read = stream.Read(buffer[carried..]);
                if (read == 0)
                {
                    return false;
                }

                int available = carried + read;
                if (buffer[..available].IndexOf(literal) >= 0)
                {
                    return true;
                }

                // Invariant: a match split across two reads is found, because the tail is kept for the next one.
                carried = Math.Min(literal.Length - 1, available);
                buffer.Slice(available - carried, carried).CopyTo(buffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>Reads markers line by line and pairs each with the method that follows it.</summary>
    /// <param name="stream">The stream, positioned at the start.</param>
    /// <param name="relativePath">Repository-relative path.</param>
    /// <param name="language">The file's language.</param>
    /// <returns>Its markers, or null when the literal matched something that is no marker.</returns>
    private static Marker[]? _ReadMarkers(FileStream stream, string relativePath, _Language language)
    {
        using StreamReader reader = new(stream, Encoding.UTF8, true, 16384, leaveOpen: true);
        List<Marker> found = [];
        List<(string Id, int Line)> pending = [];
        int waited = 0;
        int lineNumber = 0;
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (_TryReadMarker(line, language, out string id))
            {
                pending.Add((id, lineNumber));
                waited = 0;
                continue;
            }

            if (pending.Count == 0)
            {
                continue;
            }

            if (_TryReadMethod(line, language, out string method))
            {
                _Flush(pending, relativePath, method, found);
                continue;
            }

            // Data-driven tests stack many attributes above a method, so attribute lines do not use up the window.
            // A marker with no method in its window stays reported, not dropped.
            if (!_IsAttributeLine(line) && ++waited >= _MethodWindow)
            {
                _Flush(pending, relativePath, null, found);
            }
        }

        _Flush(pending, relativePath, null, found);
        return found.Count == 0
            ? null
            : [.. found];
    }

    /// <summary>Returns whether a line holds an attribute or decorator in any supported language.</summary>
    /// <param name="line">The line.</param>
    /// <returns>Whether it does.</returns>
    private static bool _IsAttributeLine(ReadOnlySpan<char> line)
    {
        ReadOnlySpan<char> trimmed = line.TrimStart();
        return trimmed.StartsWith("[", StringComparison.Ordinal)
            || trimmed.StartsWith("@", StringComparison.Ordinal)
            || trimmed.StartsWith("#[", StringComparison.Ordinal);
    }

    /// <summary>Moves pending markers to the result with the method they belong to.</summary>
    /// <param name="pending">The waiting markers; emptied.</param>
    /// <param name="file">Repository-relative path.</param>
    /// <param name="method">The method, or null.</param>
    /// <param name="found">Receives the markers.</param>
    private static void _Flush(List<(string Id, int Line)> pending, string file, string? method, List<Marker> found)
    {
        for (int index = 0; index < pending.Count; index++)
        {
            found.Add(new(pending[index].Id, file, pending[index].Line, method));
        }

        pending.Clear();
    }

    #endregion

    #region Line forms

    /// <summary>Reads the marker form of a language from one line.</summary>
    /// <param name="line">The line.</param>
    /// <param name="language">The language.</param>
    /// <param name="id">Receives the ID as written.</param>
    /// <returns>Whether the line holds a marker.</returns>
    private static bool _TryReadMarker(ReadOnlySpan<char> line, _Language language, out string id)
    {
        id = string.Empty;
        return language switch
        {
            _Language.CSharp => _TryReadCSharpMarker(line, out id),
            _Language.Python => _TryReadPythonMarker(line, out id),
            _ => _TryReadRustMarker(line, out id),
        };
    }

    /// <summary>Reads the C# form, the test framework's <c>Property</c> attribute with the name Card.</summary>
    /// <param name="line">The line.</param>
    /// <param name="id">Receives the ID.</param>
    /// <returns>Whether the line holds it.</returns>
    private static bool _TryReadCSharpMarker(ReadOnlySpan<char> line, out string id)
    {
        id = string.Empty;
        int start = line.IndexOf("Property(", StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        ReadOnlySpan<char> rest = line[(start + 9)..].TrimStart();
        if (!rest.StartsWith("\"Card\"", StringComparison.Ordinal))
        {
            return false;
        }

        rest = rest[6..].TrimStart();
        return !rest.IsEmpty && rest[0] == ',' && _TryReadQuoted(rest[1..].TrimStart(), out id);
    }

    /// <summary>Reads the Python form, <c>@pytest.mark.card</c>.</summary>
    /// <param name="line">The line.</param>
    /// <param name="id">Receives the ID.</param>
    /// <returns>Whether the line holds it.</returns>
    private static bool _TryReadPythonMarker(ReadOnlySpan<char> line, out string id)
    {
        id = string.Empty;
        const string Literal = "pytest.mark.card(";
        int start = line.IndexOf(Literal, StringComparison.Ordinal);
        return start >= 0 && _TryReadQuoted(line[(start + Literal.Length)..].TrimStart(), out id);
    }

    /// <summary>Reads the Rust form, a line comment holding only <c>card:</c> and the ID.</summary>
    /// <param name="line">The line.</param>
    /// <param name="id">Receives the ID.</param>
    /// <returns>Whether the line holds it.</returns>
    private static bool _TryReadRustMarker(ReadOnlySpan<char> line, out string id)
    {
        id = string.Empty;
        ReadOnlySpan<char> rest = line.TrimStart();
        if (!rest.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        rest = rest[2..].TrimStart();
        if (!rest.StartsWith("card:", StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> value = rest[5..].Trim();
        if (value.IsEmpty || value.IndexOfAny(' ', '\t') >= 0)
        {
            return false;
        }

        id = value.ToString();
        return true;
    }

    /// <summary>Reads a double-quoted value at the start of a span.</summary>
    /// <param name="text">The text.</param>
    /// <param name="value">Receives the value.</param>
    /// <returns>Whether a non-empty quoted value is there.</returns>
    private static bool _TryReadQuoted(ReadOnlySpan<char> text, out string value)
    {
        value = string.Empty;
        if (text.IsEmpty || text[0] != '"')
        {
            return false;
        }

        int end = text[1..].IndexOf('"');
        if (end <= 0)
        {
            return false;
        }

        value = text.Slice(1, end).ToString();
        return true;
    }

    /// <summary>Reads a method declaration of a language from one line.</summary>
    /// <param name="line">The line.</param>
    /// <param name="language">The language.</param>
    /// <param name="method">Receives the method name.</param>
    /// <returns>Whether the line declares a method.</returns>
    private static bool _TryReadMethod(ReadOnlySpan<char> line, _Language language, out string method)
    {
        if (language == _Language.CSharp)
        {
            return _TryReadAfterKeyword(line, "Task", out method) || _TryReadAfterKeyword(line, "void", out method);
        }

        return _TryReadAfterKeyword(line, language == _Language.Python ? "def" : "fn", out method);
    }

    /// <summary>Reads the identifier that follows a keyword and precedes a parenthesis.</summary>
    /// <param name="line">The line.</param>
    /// <param name="keyword">The keyword.</param>
    /// <param name="identifier">Receives the identifier.</param>
    /// <returns>Whether the form was found.</returns>
    private static bool _TryReadAfterKeyword(ReadOnlySpan<char> line, ReadOnlySpan<char> keyword, out string identifier)
    {
        identifier = string.Empty;
        int start = line.IndexOf(keyword, StringComparison.Ordinal);
        if (start < 0 || (start > 0 && !char.IsWhiteSpace(line[start - 1])))
        {
            return false;
        }

        ReadOnlySpan<char> rest = line[(start + keyword.Length)..];
        if (rest.IsEmpty || !char.IsWhiteSpace(rest[0]))
        {
            return false;
        }

        rest = rest.TrimStart();
        int length = 0;
        while (length < rest.Length && (char.IsLetterOrDigit(rest[length]) || rest[length] == '_'))
        {
            length++;
        }

        if (length == 0 || !rest[length..].TrimStart().StartsWith("(", StringComparison.Ordinal))
        {
            return false;
        }

        identifier = rest[..length].ToString();
        return true;
    }

    #endregion
}
