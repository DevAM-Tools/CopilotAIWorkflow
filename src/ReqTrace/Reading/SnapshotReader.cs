// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Reads everything the checks need from one repository.</summary>
internal static class SnapshotReader
{
    /// <summary>Reads the spec and the test markers.</summary>
    /// <param name="root">Repository root.</param>
    /// <returns>The snapshot.</returns>
    /// <exception cref="IOException">A file cannot be read.</exception>
    public static TraceSnapshot Read(string root)
    {
        List<Requirement> requirements = [];
        List<TestCard> cards = [];
        List<Finding> findings = [];

        _ReadFolder(root, "requirements", (path, relative) => SpecReader.ReadRequirements(path, relative, requirements, findings));
        _ReadFolder(root, "tests", (path, relative) => SpecReader.ReadCards(path, relative, cards, findings));

        IgnoreList ignore = IgnoreList.Read(root, findings);
        List<Marker> markers = MarkerScanner.Scan(root, ignore);

        return new(requirements, cards, markers, ignore.Entries, findings);
    }

    /// <summary>Applies a reader to each Markdown file of a spec folder, in ordinal path order.</summary>
    /// <param name="root">Repository root.</param>
    /// <param name="folder">Subfolder of <c>spec</c>.</param>
    /// <param name="read">Reader taking the absolute and the relative path.</param>
    private static void _ReadFolder(string root, string folder, Action<string, string> read)
    {
        string directory = Path.Join(root, "spec", folder);
        if (!Directory.Exists(directory))
        {
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.md");
        Array.Sort(files, StringComparer.Ordinal);
        for (int index = 0; index < files.Length; index++)
        {
            read(files[index], Invariant($"spec/{folder}/{Path.GetFileName(files[index])}"));
        }
    }
}
