// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Finds the repository that a command runs in, so the tool needs no arguments.</summary>
internal static class RepositoryLocator
{
    /// <summary>Walks up from a directory to the first one holding <c>spec/index.md</c>.</summary>
    /// <param name="start">The directory to start from.</param>
    /// <param name="root">Receives the repository root.</param>
    /// <returns>Whether a repository was found.</returns>
    public static bool TryFind(string start, out string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(start);

        string? directory = Path.GetFullPath(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Join(directory, "spec", "index.md")))
            {
                root = directory;
                return true;
            }

            directory = Path.GetDirectoryName(directory);
        }

        root = string.Empty;
        return false;
    }
}
