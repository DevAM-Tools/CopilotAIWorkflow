// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Loads project and solution XML with hardened reader settings.</summary>
internal static class ProjectXmlLoader
{
    /// <summary>Loads an XML document from a project or solution file.</summary>
    /// <param name="path">Absolute or relative path to the XML file.</param>
    /// <param name="document">Loaded document on success.</param>
    /// <param name="error">Error message when loading fails.</param>
    /// <returns><see langword="true"/> when the document was loaded.</returns>
    public static bool TryLoadDocument(string path, out XDocument? document, out string? error)
    {
        document = null;
        error = null;
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!File.Exists(path))
        {
            error = $"XML file not found: {path}";
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
            document = XDocument.Load(reader);
            return true;
        }
        catch (XmlException xmlException)
        {
            error = $"Failed to read XML file: {xmlException.Message}";
            return false;
        }
        catch (IOException ioException)
        {
            error = $"Failed to read XML file: {ioException.Message}";
            return false;
        }
    }
}
