// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Cli;

/// <summary>Additional CLI flag tests.</summary>
public sealed class CliParserParallelismTests
{
    [Test]
    public async Task TryParse_MaxParallelism_SetsValue()
    {
        bool success = CliParser.TryParse(
            ["run", "project", "Demo.csproj", "--max-parallelism", "3"],
            out CliOptions? options,
            out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(options!.MaxParallelism).IsEqualTo(3);
    }

    [Test]
    public async Task TryParse_HelpFlag_SetsShowHelp()
    {
        bool success = CliParser.TryParse(["run", "--help"], out CliOptions? options, out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(options!.ShowHelp).IsTrue();
    }

    [Test]
    public async Task TryParse_StreamFlags_SetValues()
    {
        bool streamSuccess = CliParser.TryParse(["run", "--stream"], out CliOptions? streamOptions, out string? _);
        bool noStreamSuccess = CliParser.TryParse(["run", "--no-stream"], out CliOptions? noStreamOptions, out string? _);

        await Assert.That(streamSuccess).IsTrue();
        await Assert.That(noStreamSuccess).IsTrue();
        await Assert.That(streamOptions!.Stream).IsTrue();
        await Assert.That(noStreamOptions!.NoStream).IsTrue();
    }
}
