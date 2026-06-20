// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool.Tests;

/// <summary>Creates temporary workspace directories for graph tests.</summary>
internal static class TempWorkspace
{
    public static async Task<string> CreateAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), $"coveragegap-workspace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        await File.WriteAllTextAsync(
            Path.Combine(root, "global.json"),
            await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "global.json")));
        return root;
    }
}
