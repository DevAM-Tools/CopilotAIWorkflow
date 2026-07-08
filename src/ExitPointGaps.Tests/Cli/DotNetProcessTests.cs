// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Cli;

/// <summary>Tests for <see cref="DotNetProcess"/>.</summary>
public sealed class DotNetProcessTests
{
    [Test]
    public async Task RunAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.That(async () => await DotNetProcess.RunAsync("--version", Environment.CurrentDirectory, cancellation.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task RunAsync_ValidCommand_ReturnsZero()
    {
        DotNetProcessResult result = await DotNetProcess.RunAsync("--version", Environment.CurrentDirectory);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(0);
    }
}
