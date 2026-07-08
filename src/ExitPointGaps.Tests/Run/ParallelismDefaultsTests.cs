// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Run;

/// <summary>Tests for <see cref="ParallelismDefaults"/>.</summary>
public sealed class ParallelismDefaultsTests
{
    [Test]
    public async Task Resolve_WithoutExplicitFlag_UsesProjectCount()
    {
        CliOptions options = new();
        int resolved = ParallelismDefaults.Resolve(options, projectCount: 6);

        await Assert.That(resolved).IsEqualTo(6);
    }

    [Test]
    public async Task Resolve_WithExplicitFlag_CapsAtProjectCount()
    {
        CliOptions options = new() { MaxParallelism = 8 };
        int resolved = ParallelismDefaults.Resolve(options, projectCount: 3);

        await Assert.That(resolved).IsEqualTo(3);
    }
}
