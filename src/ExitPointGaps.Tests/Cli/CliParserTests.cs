// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Cli;

/// <summary>Tests for <see cref="CliParser"/>.</summary>
public sealed class CliParserTests
{
    [Test]
    public async Task TryParse_ProjectWithoutPaths_ReturnsError()
    {
        bool success = CliParser.TryParse(["run", "project"], out CliOptions? options, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(options).IsNotNull();
        await Assert.That(error).Contains("project");
    }

    [Test]
    public async Task TryParse_UnknownFlag_ReturnsError()
    {
        bool success = CliParser.TryParse(
            ["run", "project", "Demo.csproj", "--unknown"],
            out CliOptions? _,
            out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task TryParse_RepeatedCoberturaPaths_AreCollected()
    {
        bool success = CliParser.TryParse(
        [
            "run",
            "project",
            "Demo.csproj",
            "--cobertura",
            "first.cobertura.xml",
            "--cobertura",
            "second.cobertura.xml",
        ],
        out CliOptions? options,
        out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(options!.CoberturaPaths.Count).IsEqualTo(2);
        await Assert.That(options.CoberturaPaths[0]).IsEqualTo("first.cobertura.xml");
        await Assert.That(options.CoberturaPaths[1]).IsEqualTo("second.cobertura.xml");
    }

    [Test]
    public async Task TryParse_SkipNoTests_SetsFlag()
    {
        bool success = CliParser.TryParse(
            ["run", "project", "Demo.csproj", "--skip-no-tests"],
            out CliOptions? options,
            out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(options!.SkipNoTests).IsTrue();
    }

    [Test]
    public async Task TryParse_PlanCommand_DefaultsToSolutionAuto()
    {
        bool success = CliParser.TryParse(["plan"], out CliOptions? options, out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(options!.Command).IsEqualTo("plan");
        await Assert.That(options.TargetKind).IsEqualTo(RunTargetKind.SolutionAuto);
    }
}
