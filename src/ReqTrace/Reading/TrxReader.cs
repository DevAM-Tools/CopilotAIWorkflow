// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Reads test outcomes from a TRX result file, which the test runner wrote and the agent did not.</summary>
internal static class TrxReader
{
    /// <summary>Reads outcomes from a file, or from every <c>.trx</c> below a folder; each test project writes its own.</summary>
    /// <param name="path">A file or a folder.</param>
    /// <returns>Per method: whether it passed and when that result was written. The newest file wins for a method.</returns>
    /// <exception cref="FileNotFoundException">Neither the file nor a result file in the folder exists.</exception>
    /// <exception cref="InvalidDataException">A file is not valid XML.</exception>
    /// <exception cref="IOException">A file cannot be read.</exception>
    public static Dictionary<string, (bool Passed, DateTime Written)> ReadAll(string path)
    {
        List<string> files = [];
        if (File.Exists(path))
        {
            files.Add(path);
        }
        else if (Directory.Exists(path))
        {
            files.AddRange(Directory.EnumerateFiles(path, "*.trx", SearchOption.AllDirectories));
        }

        if (files.Count == 0)
        {
            throw new FileNotFoundException(Invariant($"No result file found at: {path}"));
        }

        files.Sort(static (left, right) => File.GetLastWriteTimeUtc(left).CompareTo(File.GetLastWriteTimeUtc(right)));

        // Oldest first, so a later run of the same test replaces an earlier one.
        Dictionary<string, (bool Passed, DateTime Written)> outcomes = new(StringComparer.Ordinal);
        foreach (string file in files)
        {
            DateTime written = File.GetLastWriteTimeUtc(file);
            foreach (KeyValuePair<string, bool> result in _Read(file))
            {
                outcomes[result.Key] = (result.Value, written);
            }
        }

        return outcomes;
    }

    /// <summary>Reads outcomes per test method from one file.</summary>
    /// <param name="path">The result file.</param>
    /// <returns>Method name to whether every run of it passed, so a parameterized test needs all cases.</returns>
    /// <exception cref="InvalidDataException">The file is not valid XML.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    private static Dictionary<string, bool> _Read(string path)
    {
        // Security: a result file is parsed as data. DTDs and external resolution are off, so it cannot pull in other
        // files or expand entities. Rejected: the default settings, which leave more to the parser than this needs.
        XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        Dictionary<string, bool> outcomes = new(StringComparer.Ordinal);
        try
        {
            using XmlReader reader = XmlReader.Create(path, settings);
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "UnitTestResult")
                {
                    continue;
                }

                string method = _MethodName(reader.GetAttribute("testName"));
                bool passed = string.Equals(reader.GetAttribute("outcome"), "Passed", StringComparison.Ordinal);
                outcomes[method] = passed && outcomes.GetValueOrDefault(method, true);
            }
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException(Invariant($"Malformed result file {path}: {exception.Message}"), exception);
        }

        return outcomes;
    }

    /// <summary>Reduces a reported test name to its method name.</summary>
    /// <param name="testName">The name as reported, possibly qualified and with arguments.</param>
    /// <returns>The bare method name.</returns>
    private static string _MethodName(string? testName)
    {
        ReadOnlySpan<char> name = testName;
        int arguments = name.IndexOf('(');
        if (arguments >= 0)
        {
            name = name[..arguments];
        }

        int dot = name.LastIndexOf('.');
        return (dot < 0 ? name : name[(dot + 1)..]).Trim().ToString();
    }
}
