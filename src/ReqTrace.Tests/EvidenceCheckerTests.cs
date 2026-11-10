// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>Claims versus test results.</summary>
public sealed class EvidenceCheckerTests
{
    /// <summary>Creates a repository with one ticked requirement and one marked test.</summary>
    /// <param name="method">Marked method.</param>
    /// <returns>The repository.</returns>
    private static TraceWorkspace _Ticked(string method = "ProvesIt")
    {
        TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", met: "✅"));
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-ONE", "REQ-AREA-ONE"));
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-ONE", method));
        return workspace;
    }

    /// <summary>Writes a result file and marks it newer than the sources.</summary>
    /// <param name="workspace">The repository.</param>
    /// <param name="results">Name and outcome per result.</param>
    private static void _WriteResults(TraceWorkspace workspace, params (string Name, string Outcome)[] results)
    {
        string rows = string.Concat(results.Select(static result => Invariant($"<UnitTestResult testName=\"{result.Name}\" outcome=\"{result.Outcome}\" />")));
        workspace.Write("out/r.trx", "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>" + rows + "</Results></TestRun>");
        File.SetLastWriteTimeUtc(Path.Join(workspace.Root, "out", "r.trx"), DateTime.UtcNow.AddMinutes(1));
    }

    [Test, Property("Card", "TEST-TRACE-CLAIM-NEEDS-PASS")]
    [Arguments("Failed", 1, "did not pass")]
    [Arguments("Passed", 0, "")]
    public async Task TickedRequirementFollowsTheResult(string outcome, int expectedExit, string expectedText)
    {
        using TraceWorkspace workspace = _Ticked();
        _WriteResults(workspace, ("ProvesIt", outcome));

        (int exitCode, string output, _) = workspace.Run("check", "--results", "out/r.trx");

        await Assert.That(exitCode).IsEqualTo(expectedExit);
        await Assert.That(output).Contains(expectedText);
    }

    [Test]
    public async Task AbsentResultIsReported()
    {
        using TraceWorkspace workspace = _Ticked();
        _WriteResults(workspace, ("SomethingElse", "Passed"));

        (int exitCode, string output, _) = workspace.Run("check", "--results", "out/r.trx");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("ProvesIt is not in the results");
    }

    [Test]
    public async Task FolderCombinesProjectsAndTheNewestRunWins()
    {
        using TraceWorkspace workspace = _Ticked();
        const string Row = "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>";
        workspace.Write("out/one/old.trx", Row + "<UnitTestResult testName=\"ProvesIt\" outcome=\"Failed\" /></Results></TestRun>");
        workspace.Write("out/two/new.trx", Row + "<UnitTestResult testName=\"ProvesIt\" outcome=\"Passed\" /></Results></TestRun>");
        File.SetLastWriteTimeUtc(Path.Join(workspace.Root, "out", "one", "old.trx"), DateTime.UtcNow.AddMinutes(1));
        File.SetLastWriteTimeUtc(Path.Join(workspace.Root, "out", "two", "new.trx"), DateTime.UtcNow.AddMinutes(2));

        (int exitCode, _, _) = workspace.Run("check", "--results", "out");

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ParameterizedTestNeedsEveryCaseToPass()
    {
        using TraceWorkspace workspace = _Ticked();
        _WriteResults(workspace, ("ProvesIt(1)", "Passed"), ("ProvesIt(2)", "Failed"));

        (int mixedExit, _, _) = workspace.Run("check", "--results", "out/r.trx");
        _WriteResults(workspace, ("ProvesIt(1)", "Passed"), ("ProvesIt(2)", "Passed"));
        (int passedExit, _, _) = workspace.Run("check", "--results", "out/r.trx");

        await Assert.That(mixedExit).IsEqualTo(1);
        await Assert.That(passedExit).IsEqualTo(0);
    }

    [Test]
    public async Task MethodMarkedInTwoFilesIsAmbiguousNotPassed()
    {
        using TraceWorkspace workspace = _Ticked();
        workspace.Write("tests/B.cs", TraceWorkspace.MarkedTest("TEST-AREA-ONE", "ProvesIt"));
        _WriteResults(workspace, ("ProvesIt", "Passed"));

        (int exitCode, string output, _) = workspace.Run("check", "--results", "out/r.trx");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("ambiguous");
    }

    [Test, Property("Card", "TEST-TRACE-NO-RESULTS-HONEST")]
    public async Task WithoutResultsEvidenceIsNotCheckedAndNotCountedAsPassed()
    {
        using TraceWorkspace workspace = _Ticked();

        (int exitCode, string output, _) = workspace.Run();
        (int missingExit, _, string missing) = workspace.Run("check", "--results", "does/not/exist.trx");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Evidence not checked");
        await Assert.That(missingExit).IsEqualTo(2);
        await Assert.That(missing).Contains("does");
    }

    [Test, Property("Card", "TEST-TRACE-STALE-RESULTS")]
    public async Task OlderResultFileDoesNotCountAndMalformedOneExitsTwo()
    {
        using TraceWorkspace workspace = _Ticked();
        _WriteResults(workspace, ("ProvesIt", "Passed"));
        File.SetLastWriteTimeUtc(Path.Join(workspace.Root, "out", "r.trx"), DateTime.UtcNow.AddHours(-1));

        (int staleExit, string stale, _) = workspace.Run("check", "--results", "out");

        await Assert.That(staleExit).IsEqualTo(1);
        await Assert.That(stale).Contains("older than the test file");

        workspace.Write("out/r.trx", "<not-closed");

        (int badExit, _, string bad) = workspace.Run("check", "--results", "out/r.trx");

        await Assert.That(badExit).IsEqualTo(2);
        await Assert.That(bad).Contains("r.trx");
    }
}
