// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

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
        string root = Path.Combine(Path.GetTempPath(), Invariant($"snippet-root-{Guid.NewGuid():N}"));
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

    [Test]
    public async Task TryReadSnippet_AbsolutePathOutsideRepo_ReturnsNull()
    {
        string root = Path.Combine(Path.GetTempPath(), Invariant($"snippet-in-{Guid.NewGuid():N}"));
        string outsideRoot = Path.Combine(Path.GetTempPath(), Invariant($"snippet-out-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outsideRoot);
        string outsideFile = Path.Combine(outsideRoot, "Secret.cs");
        await File.WriteAllTextAsync(outsideFile, "secret-line\n");

        string? snippet = PathNormalizer.TryReadSnippet(root, outsideFile, 1);

        await Assert.That(snippet).IsNull();
    }

    [Test]
    public async Task TryReadSnippet_DotDotEscape_ReturnsNull()
    {
        string root = Path.Combine(Path.GetTempPath(), Invariant($"snippet-esc-{Guid.NewGuid():N}"));
        string sibling = Path.Combine(Path.GetTempPath(), Invariant($"snippet-sib-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(sibling);
        string siblingFile = Path.Combine(sibling, "Outside.cs");
        await File.WriteAllTextAsync(siblingFile, "escaped\n");

        string relativeEscape = Path.Combine("..", Path.GetFileName(sibling), "Outside.cs")
            .Replace('\\', '/');
        string? snippet = PathNormalizer.TryReadSnippet(root, relativeEscape, 1);

        await Assert.That(snippet).IsNull();
    }

    [Test]
    public async Task ToRepositoryRelative_RelativePath_UsesRepositoryRootNotCwd()
    {
        string repoRoot = Path.Combine(Path.GetTempPath(), Invariant($"norm-repo-{Guid.NewGuid():N}"));
        string foreignCwd = Path.Combine(Path.GetTempPath(), Invariant($"norm-cwd-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(repoRoot);
        Directory.CreateDirectory(foreignCwd);

        string previousCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = foreignCwd;
            string relative = PathNormalizer.ToRepositoryRelative("src/Sample/Foo.cs", repoRoot);
            await Assert.That(relative).IsEqualTo("src/Sample/Foo.cs");
        }
        finally
        {
            Environment.CurrentDirectory = previousCwd;
        }
    }
}
