// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests;

/// <summary>Tests for <see cref="PathNormalizer"/>.</summary>
public sealed class PathNormalizerTests
{
    [Test]
    public async Task ToRepositoryRelative_StripsRepositoryPrefix()
    {
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo-root"));
        string nested = Path.Combine(root, "src", "Foo.cs");

        string relative = PathNormalizer.ToRepositoryRelative(nested, root);

        await Assert.That(relative).IsEqualTo("src/Foo.cs");
    }

    [Test]
    public async Task TryReadSnippet_ReturnsLineWhenFileExists()
    {
        string root = Path.Combine(Path.GetTempPath(), $"snippet-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string file = Path.Combine(root, "Sample.cs");
        await File.WriteAllTextAsync(file, "line1\nline2\n");

        string? snippet = PathNormalizer.TryReadSnippet(root, "Sample.cs", 2);

        await Assert.That(snippet).IsEqualTo("line2");
    }

    [Test]
    public async Task TryReadSnippet_InvalidLine_ReturnsNull()
    {
        string? snippet = PathNormalizer.TryReadSnippet(Path.GetTempPath(), "missing.cs", 0);

        await Assert.That(snippet).IsNull();
    }
}
