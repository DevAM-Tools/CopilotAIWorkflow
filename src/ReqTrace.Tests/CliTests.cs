// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>Command line, output contract, and the rule table.</summary>
public sealed class CliTests
{
    [Test, Property("Card", "TEST-TRACE-ONE-COMMAND")]
    public async Task RunsWithoutArgumentsFromRootAndSubfolder()
    {
        using TraceWorkspace workspace = new();
        workspace.Write("src/deep/er/file.txt", "x");

        (int fromRoot, _, _) = workspace.Run();
        (int fromNested, _, _) = TraceWorkspace.RunFrom(Path.Join(workspace.Root, "src", "deep", "er"));

        await Assert.That(fromRoot).IsEqualTo(0);
        await Assert.That(fromNested).IsEqualTo(0);
    }

    [Test]
    public async Task OutsideARepositoryExitsTwoAndNamesTheFile()
    {
        string directory = Path.Join(Path.GetTempPath(), "reqtrace-none-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        try
        {
            (int exitCode, _, string error) = TraceWorkspace.RunFrom(directory);

            await Assert.That(exitCode).IsEqualTo(2);
            await Assert.That(error).Contains("spec/index.md");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    [Arguments("--help")]
    [Arguments("-h")]
    [Arguments("help")]
    public async Task HelpNamesEveryCommandAndNeedsNoRepository(string flag)
    {
        (int exitCode, string output, _) = TraceWorkspace.RunFrom(Path.GetTempPath(), flag);

        await Assert.That(exitCode).IsEqualTo(0);
        foreach (string word in new[] { "check", "show", "report", "accept", "confirm", "syntax", "--results", "--json", "--allow-gap", "--note" })
        {
            await Assert.That(output).Contains(word);
        }
    }

    [Test]
    [Arguments("--nope")]
    [Arguments("frobnicate")]
    public async Task UnknownArgumentsExitTwo(string argument)
    {
        using TraceWorkspace workspace = new();

        (int exitCode, _, string error) = workspace.Run(argument);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(error).Contains("Unknown");
    }

    [Test, Property("Card", "TEST-TRACE-ACTIONABLE")]
    public async Task EveryRuleProducesLocationMessageAndFix()
    {
        foreach (RuleTemplate rule in Rules.All)
        {
            Finding finding = Rules.Create(rule.Id, Severity.Error, "spec/a.md", 7, "SUBJECT-X", "DETAIL");

            await Assert.That(finding.File).IsNotEmpty();
            await Assert.That(finding.Message).IsNotEmpty();
            await Assert.That(finding.Fix).IsNotEmpty();
        }

        await Assert.That(Rules.All.Select(static rule => rule.Id).Distinct().Count()).IsEqualTo(Rules.All.Length);
    }

    [Test, Property("Card", "TEST-TRACE-JSON")]
    public async Task JsonHasLockedFieldsAndNoTextAround()
    {
        using TraceWorkspace workspace = new();
        workspace.Write("spec/requirements/a.md", "### Area\n\nStray.\n");

        (int exitCode, string output, _) = workspace.Run("check", "--json");

        using JsonDocument document = JsonDocument.Parse(output);
        JsonElement root = document.RootElement;
        JsonElement finding = root.GetProperty("findings")[0];

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(root.GetProperty("schemaVersion").GetInt32()).IsEqualTo(1);
        await Assert.That(root.GetProperty("counts").GetProperty("requirements").GetInt32()).IsEqualTo(0);
        await Assert.That(finding.GetProperty("rule").GetString()).IsEqualTo("F1");
        await Assert.That(finding.GetProperty("severity").GetString()).IsEqualTo("error");
        await Assert.That(finding.GetProperty("file").GetString()).IsEqualTo("spec/requirements/a.md");
        await Assert.That(finding.GetProperty("line").GetInt32()).IsEqualTo(3);
        await Assert.That(finding.GetProperty("message").GetString()).IsNotEmpty();
        await Assert.That(finding.GetProperty("fix").GetString()).IsNotEmpty();
    }
}
