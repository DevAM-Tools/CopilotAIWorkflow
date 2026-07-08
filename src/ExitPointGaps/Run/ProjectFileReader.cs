// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Reads lightweight facts from project files without MSBuild.</summary>
internal static class ProjectFileReader
{
    /// <summary>Detects whether a project is an executable host rather than a coverage-gated library.</summary>
    /// <param name="projectPath">Absolute project path.</param>
    /// <returns><see langword="true"/> when <c>OutputType</c> is <c>Exe</c>.</returns>
    public static bool IsExecutableProject(string projectPath)
    {
        if (!ProjectXmlLoader.TryLoadDocument(projectPath, out XDocument? document, out string? _)
            || document is null)
        {
            return false;
        }

        string? outputType = document.Descendants("OutputType")
            .Select(static element => element.Value)
            .FirstOrDefault();
        return string.Equals(outputType, "Exe", StringComparison.OrdinalIgnoreCase);
    }
}
