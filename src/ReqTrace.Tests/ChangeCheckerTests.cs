// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>Comparison with the accepted state.</summary>
public sealed class ChangeCheckerTests
{
    /// <summary>Creates a repository with one fully linked requirement, already accepted.</summary>
    /// <param name="text">Requirement text.</param>
    /// <returns>The repository.</returns>
    private static TraceWorkspace _Accepted(string text = "The tool shall always exit with code 1 when at most 30 gaps remain.")
    {
        TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", text));
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-ONE", "REQ-AREA-ONE") + TraceWorkspace.Card("TEST-AREA-TWO", "REQ-AREA-ONE"));
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-ONE", "A") + TraceWorkspace.MarkedTest("TEST-AREA-TWO", "B"));
        workspace.Run("accept");
        return workspace;
    }

    [Test, Property("Card", "TEST-TRACE-VANISHED")]
    public async Task DeletedEntriesAreReportedUnlessRetired()
    {
        using TraceWorkspace workspace = _Accepted();
        workspace.Write("spec/requirements/a.md", "| ID | Text |\n|---|---|\n| A | B |\n");

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("REQ-AREA-ONE was accepted and no longer exists.");
        await Assert.That(output).Contains("F5");
        await Assert.That(output).Contains("Read: 0 requirements");

        workspace.WriteRequirements("## Retired\n\n- **REQ-AREA-ONE** — Old.\n  - **State:** retired · **Replaced by:** none · **Why:** dropped.\n");
        workspace.Delete("spec/tests/a.md");
        workspace.Write("tests/A.cs", "// nothing\n");

        (_, string afterRetire, _) = workspace.Run();

        await Assert.That(afterRetire).DoesNotContain("REQ-AREA-ONE was accepted");
        await Assert.That(afterRetire).Contains("TEST-AREA-ONE was accepted");
    }

    [Test, Property("Card", "TEST-TRACE-MARKER-REMOVED")]
    public async Task LosingTheOnlyMarkerFailsTheCard()
    {
        using TraceWorkspace workspace = _Accepted();
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-TWO", "B"));

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("TEST-AREA-ONE had a marked test and has none now.");
        await Assert.That(output).DoesNotContain("TEST-AREA-TWO had");
    }

    [Test]
    public async Task LosingOneOfTwoMarkersIsFine()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE"));
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-ONE", "REQ-AREA-ONE"));
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-ONE", "A") + TraceWorkspace.MarkedTest("TEST-AREA-ONE", "B"));
        workspace.Run("accept");
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-ONE", "A"));

        (int exitCode, _, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test, Property("Card", "TEST-TRACE-CHANGED-VISIBLE")]
    public async Task ReportShowsRemovedAndAddedWordsAndIgnoresWhitespace()
    {
        using TraceWorkspace workspace = _Accepted("The tool shall always exit.");
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", "The tool   shall   always exit."));

        (_, string whitespaceReport, _) = workspace.Run("report");

        await Assert.That(whitespaceReport).Contains("Changed:\n  none".Replace("\n", Environment.NewLine, StringComparison.Ordinal));

        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", "The tool shall usually exit."));

        (_, string report, _) = workspace.Run("report");

        await Assert.That(report).Contains("REQ-AREA-ONE: removed \"always\", added \"usually\"");
    }

    [Test, Property("Card", "TEST-TRACE-SUSPECT")]
    public async Task ChangedRequirementLeavesCardsUnconfirmedUntilEachIsConfirmed()
    {
        using TraceWorkspace workspace = _Accepted();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", "The tool shall sometimes exit."));

        (int exitCode, string output, _) = workspace.Run();
        (int acceptExit, _, _) = workspace.Run("accept");
        (int noNote, _, _) = workspace.Run("confirm", "TEST-AREA-ONE");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("TEST-AREA-ONE proves REQ-AREA-ONE, which changed");
        await Assert.That(output).Contains("TEST-AREA-TWO proves REQ-AREA-ONE, which changed");
        await Assert.That(acceptExit).IsEqualTo(1);
        await Assert.That(noNote).IsEqualTo(2);

        workspace.Run("confirm", "TEST-AREA-ONE", "--note", "still proves it");

        (int oneLeft, string oneOutput, _) = workspace.Run();

        await Assert.That(oneLeft).IsEqualTo(1);
        await Assert.That(oneOutput).DoesNotContain("TEST-AREA-ONE proves");
        await Assert.That(oneOutput).Contains("TEST-AREA-TWO proves");

        workspace.Run("confirm", "TEST-AREA-TWO", "--note", "also fine");

        (int allConfirmed, _, _) = workspace.Run();
        (_, string report, _) = workspace.Run("report");

        await Assert.That(allConfirmed).IsEqualTo(0);
        await Assert.That(report).Contains("TEST-AREA-ONE: still proves it");
        await Assert.That(report).Contains("TEST-AREA-TWO: also fine");

        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", "The tool shall rarely exit."));

        (int reopened, _, _) = workspace.Run();

        await Assert.That(reopened).IsEqualTo(1);
    }

    [Test, Property("Card", "TEST-TRACE-WEAKENED")]
    [Arguments("The tool shall exit with code 1 when at most 30 gaps remain.", true)]
    [Arguments("The tool shall usually always exit with code 1 when at most 30 gaps remain.", true)]
    [Arguments("The tool shall always exit with code 1 when at most 60 gaps remain.", true)]
    [Arguments("The tool should always exit with code 1 when at most 30 gaps remain.", true)]
    [Arguments("The tool shall always exit with code 1 when at most 30 gaps persist.", false)]
    public async Task WeakeningIsFlaggedOnlyAsAHint(string changed, bool flagged)
    {
        using TraceWorkspace workspace = _Accepted();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", changed));
        workspace.Run("confirm", "TEST-AREA-ONE", "--note", "ok");
        workspace.Run("confirm", "TEST-AREA-TWO", "--note", "ok");

        (int exitCode, _, _) = workspace.Run();
        (_, string report, _) = workspace.Run("report");
        string section = report[report.IndexOf("Possibly weakened:", StringComparison.Ordinal)..report.IndexOf("Vanished:", StringComparison.Ordinal)];

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(section.Contains("REQ-AREA-ONE", StringComparison.Ordinal)).IsEqualTo(flagged);
    }

    [Test, Property("Card", "TEST-TRACE-ONLY-CHANGE-BLOCKS")]
    public async Task OldGapsDoNotBlockButNewOnesAndReadingErrorsDo()
    {
        using TraceWorkspace workspace = new();
        StringBuilder requirements = new();
        for (int index = 0; index < 50; index++)
        {
            requirements.Append(TraceWorkspace.Requirement(Invariant($"REQ-OLD-N{index:D2}")));
        }

        workspace.WriteRequirements(requirements.ToString(), "old");
        workspace.Run("accept");

        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-NEW-ONE"), "new");
        workspace.WriteCards(TraceWorkspace.Card("TEST-NEW-ONE", "REQ-NEW-ONE"), "new");
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-NEW-ONE", "A"));

        (int goodExit, string good, _) = workspace.Run();

        await Assert.That(goodExit).IsEqualTo(0);
        await Assert.That(good).Contains("50 open gaps");

        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-NEW-ONE") + TraceWorkspace.Requirement("REQ-NEW-TWO"), "new");

        (int gapExit, string gap, _) = workspace.Run();

        await Assert.That(gapExit).IsEqualTo(1);
        await Assert.That(gap).Contains("REQ-NEW-TWO has no active test card.");
        await Assert.That(gap).DoesNotContain("REQ-OLD-N01 has");

        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-NEW-ONE"), "new");
        workspace.Write("spec/requirements/old.md", "### Area\n\nStray.\n" + requirements);

        (int readExit, string read, _) = workspace.Run();

        await Assert.That(readExit).IsEqualTo(1);
        await Assert.That(read).Contains("F1");
    }

    [Test, Property("Card", "TEST-TRACE-ALLOW-GAP")]
    public async Task NewGapsNeedAReasonOnlyAfterTheStartingPoint()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE"));

        (int firstExit, string first, _) = workspace.Run("accept");

        await Assert.That(firstExit).IsEqualTo(0);
        await Assert.That(first).Contains("starting point");

        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE") + TraceWorkspace.Requirement("REQ-AREA-TWO"));

        (int refusedExit, string refused, _) = workspace.Run("accept");
        (int emptyExit, _, _) = workspace.Run("accept", "--allow-gap", "  ");
        (int allowedExit, _, _) = workspace.Run("accept", "--allow-gap", "test follows next sprint");
        (_, string report, _) = workspace.Run("report");

        await Assert.That(refusedExit).IsEqualTo(1);
        await Assert.That(refused).Contains("REQ-AREA-TWO has no active test card.");
        await Assert.That(emptyExit).IsEqualTo(2);
        await Assert.That(allowedExit).IsEqualTo(0);
        await Assert.That(report).Contains("REQ-AREA-TWO: test follows next sprint");
    }

    [Test, Property("Card", "TEST-TRACE-REPORT")]
    public async Task ReportListsEachChangeKindOnce()
    {
        using TraceWorkspace workspace = new();

        (_, string empty, _) = workspace.Run("report");

        await Assert.That(empty).Contains("Changed:");
        await Assert.That(empty).Contains("none");

        workspace.WriteRequirements(
            TraceWorkspace.Requirement("REQ-AREA-CHANGE", "The tool shall always run.")
            + TraceWorkspace.Requirement("REQ-AREA-ZCHANGE", "The tool shall stop.")
            + TraceWorkspace.Requirement("REQ-AREA-GONE")
            + TraceWorkspace.Requirement("REQ-AREA-RETIRE"));
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-CHANGE", "REQ-AREA-CHANGE"));
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-CHANGE", "A"));
        workspace.Run("accept");

        workspace.WriteRequirements(
            TraceWorkspace.Requirement("REQ-AREA-CHANGE", "The tool shall usually run.")
            + TraceWorkspace.Requirement("REQ-AREA-ZCHANGE", "The tool shall halt.")
            + "\n## Retired\n\n- **REQ-AREA-RETIRE** — Old.\n  - **State:** retired\n");

        (_, string report, _) = workspace.Run("report");

        int first = report.IndexOf("REQ-AREA-CHANGE: removed", StringComparison.Ordinal);
        int second = report.IndexOf("REQ-AREA-ZCHANGE: removed", StringComparison.Ordinal);

        await Assert.That(first).IsGreaterThanOrEqualTo(0);
        await Assert.That(second).IsGreaterThan(first);
        await Assert.That(report).Contains("REQ-AREA-GONE was accepted");
        await Assert.That(report).Contains("  REQ-AREA-RETIRE");
        await Assert.That(report).Contains("TEST-AREA-CHANGE proves REQ-AREA-CHANGE");
    }
}
