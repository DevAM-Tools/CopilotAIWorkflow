// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis;

/// <summary>Reads Cobertura XML into <see cref="Models.CoberturaDocument"/>. 
/// Thread-safe; all members are stateless.
/// </summary>
public static class CoberturaReader
{
    private const long _MaxFileSizeBytes = 50L * 1024L * 1024L;

    /// <summary>Attempts to read a Cobertura file.</summary>
    /// <param name="path">Path to Cobertura XML.</param>
    /// <param name="document">Parsed document when successful.</param>
    /// <param name="error">Error message when unsuccessful.</param>
    /// <returns><see langword="true"/> when parsing succeeds.</returns>
    public static bool TryRead(string path, out Models.CoberturaDocument? document, out string? error)
    {
        document = null;
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Cobertura path is required.";
            return false;
        }

        if (!File.Exists(path))
        {
            error = $"Cobertura file not found: {path}";
            return false;
        }

        FileInfo fileInfo = new FileInfo(path);
        if (fileInfo.Length > _MaxFileSizeBytes)
        {
            error = $"Cobertura file exceeds maximum size of {_MaxFileSizeBytes} bytes.";
            return false;
        }

        try
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            using FileStream stream = File.OpenRead(path);
            using XmlReader reader = XmlReader.Create(stream, settings);
            return _TryParse(reader, path, out document, out error);
        }
        catch (XmlException ex)
        {
            error = $"Malformed Cobertura XML: {ex.Message}";
            return false;
        }
        catch (IOException ex)
        {
            error = $"Failed to read Cobertura file: {ex.Message}";
            return false;
        }
    }

    private static bool _TryParse(XmlReader reader, string path, out Models.CoberturaDocument? document, out string? error)
    {
        document = null;
        error = null;

        double branchRate = 0;
        Dictionary<string, double> packageRates = new Dictionary<string, double>(StringComparer.Ordinal);
        Dictionary<string, double> classRates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Dictionary<int, Models.CoberturaLineInfo>> linesByFile =
            new Dictionary<string, Dictionary<int, Models.CoberturaLineInfo>>(StringComparer.OrdinalIgnoreCase);

        string? currentPackage = null;
        string? currentClassFile = null;
        string? currentMethod = null;

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.Name)
            {
                case "coverage":
                    branchRate = _ReadRate(reader, "branch-rate");
                    break;
                case "package":
                    currentPackage = reader.GetAttribute("name") ?? string.Empty;
                    if (!string.IsNullOrEmpty(currentPackage))
                    {
                        packageRates[currentPackage] = _ReadRate(reader, "branch-rate");
                    }

                    break;
                case "class":
                    currentClassFile = reader.GetAttribute("filename") ?? string.Empty;
                    if (!string.IsNullOrEmpty(currentClassFile))
                    {
                        classRates[currentClassFile] = _ReadRate(reader, "branch-rate");
                    }

                    break;
                case "method":
                    currentMethod = _ReadMethodName(reader);
                    break;
                case "line":
                    if (string.IsNullOrEmpty(currentClassFile))
                    {
                        break;
                    }

                    if (!linesByFile.TryGetValue(currentClassFile, out Dictionary<int, Models.CoberturaLineInfo>? lineMap))
                    {
                        lineMap = [];
                        linesByFile[currentClassFile] = lineMap;
                    }

                    int lineNumber = _ReadInt(reader, "number");
                    int hits = _ReadInt(reader, "hits");
                    bool isBranch = string.Equals(reader.GetAttribute("branch"), "True", StringComparison.OrdinalIgnoreCase);
                    List<double> conditions = _ReadConditions(reader);
                    if (!lineMap.TryGetValue(lineNumber, out Models.CoberturaLineInfo? existing) || hits > existing.Hits)
                    {
                        lineMap[lineNumber] = new Models.CoberturaLineInfo(hits, isBranch, conditions, currentMethod);
                    }

                    break;
            }
        }

        if (packageRates.Count == 0 && linesByFile.Count == 0)
        {
            error = "Cobertura file contains no coverage data.";
            return false;
        }

        IReadOnlyDictionary<string, IReadOnlyDictionary<int, Models.CoberturaLineInfo>> frozenLines =
            linesByFile.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyDictionary<int, Models.CoberturaLineInfo>)pair.Value,
                StringComparer.OrdinalIgnoreCase);

        document = new Models.CoberturaDocument(
            path,
            branchRate,
            packageRates,
            classRates,
            frozenLines);

        return true;
    }

    private static string? _ReadMethodName(XmlReader reader)
    {
        string? name = reader.GetAttribute("name");
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return reader.GetAttribute("signature");
    }

    private static int _ReadInt(XmlReader reader, string attributeName)
    {
        string? value = reader.GetAttribute(attributeName);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static double _ReadRate(XmlReader reader, string attributeName)
    {
        string? value = reader.GetAttribute(attributeName);
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static List<double> _ReadConditions(XmlReader reader)
    {
        List<double> conditions = [];
        string? lineCoverage = reader.GetAttribute("condition-coverage");
        if (reader.IsEmptyElement)
        {
            if (!string.IsNullOrEmpty(lineCoverage))
            {
                double parsed = _ParsePercentage(lineCoverage);
                if (parsed >= 0 && parsed < 1d)
                {
                    conditions.Add(parsed);
                }
            }

            return conditions;
        }

        int depth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "condition")
            {
                string? coverage = reader.GetAttribute("coverage");
                if (!string.IsNullOrEmpty(coverage))
                {
                    double parsed = _ParsePercentage(coverage);
                    if (parsed >= 0)
                    {
                        conditions.Add(parsed);
                    }
                }
            }
        }

        if (conditions.Count == 0 && !string.IsNullOrEmpty(lineCoverage))
        {
            double parsed = _ParsePercentage(lineCoverage);
            if (parsed >= 0 && parsed < 1d)
            {
                conditions.Add(parsed);
            }
        }

        return conditions;
    }

    private static double _ParsePercentage(string value)
    {
        ReadOnlySpan<char> span = value.AsSpan().Trim();
        int percentIndex = span.IndexOf('%');
        if (percentIndex > 0)
        {
            span = span[..percentIndex];
        }

        if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio))
        {
            if (ratio > 1)
            {
                return ratio / 100d;
            }

            return ratio;
        }

        return -1;
    }
}
