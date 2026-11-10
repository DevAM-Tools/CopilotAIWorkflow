// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>What an agent that works badly can do. Mistakes no tool can see are listed as passing.</summary>
public sealed class SabotageTests
{
    private const string _Requirements = "spec/requirements/a.md";

    /// <summary>Requirement text of the accepted repository.</summary>
    private static string _RequirementFile()
    {
        return TraceWorkspace.Requirement("REQ-AREA-GAP", "The tool shall always exit with code 1.", "✅")
            + TraceWorkspace.Requirement("REQ-AREA-PASS", "The tool shall exit with code 0.", "✅");
    }

    /// <summary>Test source of the accepted repository.</summary>
    private static string _TestFile()
    {
        return TraceWorkspace.MarkedTest("TEST-AREA-GAP", "ExitsOne") + TraceWorkspace.MarkedTest("TEST-AREA-PASS", "ExitsZero");
    }

    /// <summary>Creates the accepted repository, with results that pass.</summary>
    private static TraceWorkspace _Accepted()
    {
        TraceWorkspace workspace = new();
        workspace.WriteRequirements(_RequirementFile());
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-GAP", "REQ-AREA-GAP") + TraceWorkspace.Card("TEST-AREA-PASS", "REQ-AREA-PASS"));
        workspace.Write("tests/A.cs", _TestFile());
        workspace.Run("accept");
        _WriteResults(workspace, "Passed");
        return workspace;
    }

    /// <summary>Writes passing or failing results, newer than the sources.</summary>
    private static void _WriteResults(TraceWorkspace workspace, string outcomeOfGap)
    {
        workspace.Write(
            "out/r.trx",
            "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>"
            + Invariant($"<UnitTestResult testName=\"ExitsOne\" outcome=\"{outcomeOfGap}\" /><UnitTestResult testName=\"ExitsZero\" outcome=\"Passed\" />")
            + "</Results></TestRun>");
        File.SetLastWriteTimeUtc(Path.Join(workspace.Root, "out", "r.trx"), DateTime.UtcNow.AddMinutes(1));
    }

    [Test]
    public async Task UntouchedRepositoryPasses()
    {
        using TraceWorkspace workspace = _Accepted();

        (int exitCode, _, _) = workspace.Run("check", "--results", "out");

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test, Property("Card", "TEST-TRACE-SABOTAGE")]
    [Arguments("malformed-id", "F2")]
    [Arguments("loose-note", "F1")]
    [Arguments("missing-field", "F3")]
    [Arguments("rewritten-as-table", "F5")]
    [Arguments("deleted-requirement", "C1")]
    [Arguments("duplicate-id", "L1")]
    [Arguments("misspelled-marker", "L3")]
    [Arguments("deleted-test", "C2")]
    [Arguments("ticked-but-failing", "E2")]
    [Arguments("reworded", "C3")]
    public async Task EachSabotageFailsAndIsNamed(string sabotage, string rule)
    {
        using TraceWorkspace workspace = _Accepted();
        _Apply(workspace, sabotage);

        (int exitCode, string output, _) = workspace.Run("check", "--results", "out");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains(Invariant($"  {rule}  "));
    }

    [Test]
    [Arguments("marker-on-wrong-test")]
    [Arguments("test-without-assert")]
    public async Task WhatNoToolCanSeePassesAndNeedsAReader(string sabotage)
    {
        using TraceWorkspace workspace = _Accepted();
        _Apply(workspace, sabotage);

        (int exitCode, _, _) = workspace.Run("check", "--results", "out");

        await Assert.That(exitCode).IsEqualTo(0);
    }

    /// <summary>Applies one sabotage to the repository.</summary>
    private static void _Apply(TraceWorkspace workspace, string sabotage)
    {
        string area = "### Area\n\n";
        switch (sabotage)
        {
            case "malformed-id":
                workspace.Write(_Requirements, area + _RequirementFile().Replace("**REQ-AREA-PASS**", "**req-area-pass**", StringComparison.Ordinal));
                break;
            case "loose-note":
                workspace.Write(_Requirements, area + TraceWorkspace.Requirement("REQ-AREA-GAP", "The tool shall always exit with code 1.", "✅")
                    + "Note: also handle timeouts.\n" + TraceWorkspace.Requirement("REQ-AREA-PASS", "The tool shall exit with code 0.", "✅"));
                break;
            case "missing-field":
                workspace.Write(_Requirements, area + _RequirementFile().Replace("  - **Proof:** test\n", string.Empty, StringComparison.Ordinal));
                break;
            case "rewritten-as-table":
                workspace.Write(_Requirements, "| ID | Text |\n|---|---|\n| GAP | exit 1 |\n");
                break;
            case "deleted-requirement":
                workspace.Write(_Requirements, area + TraceWorkspace.Requirement("REQ-AREA-GAP", "The tool shall always exit with code 1.", "✅"));
                break;
            case "duplicate-id":
                workspace.Write(_Requirements, area + _RequirementFile() + TraceWorkspace.Requirement("REQ-AREA-GAP", "Copy.", "⬜"));
                break;
            case "misspelled-marker":
                workspace.Write("tests/A.cs", _TestFile().Replace("TEST-AREA-PASS", "TEST-AREA-PAS", StringComparison.Ordinal));
                break;
            case "deleted-test":
                workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-GAP", "ExitsOne"));
                break;
            case "ticked-but-failing":
                _WriteResults(workspace, "Failed");
                break;
            case "reworded":
                workspace.Write(_Requirements, area + _RequirementFile().Replace("shall always exit", "shall usually exit", StringComparison.Ordinal));
                break;
            case "marker-on-wrong-test":
                workspace.Write("tests/A.cs", _TestFile().Replace("ExitsOne", "DeletesWorkDirectory", StringComparison.Ordinal));
                workspace.Write(
                    "out/r.trx",
                    "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>"
                    + "<UnitTestResult testName=\"DeletesWorkDirectory\" outcome=\"Passed\" /><UnitTestResult testName=\"ExitsZero\" outcome=\"Passed\" />"
                    + "</Results></TestRun>");
                File.SetLastWriteTimeUtc(Path.Join(workspace.Root, "out", "r.trx"), DateTime.UtcNow.AddMinutes(1));
                break;
            case "test-without-assert":
                // The fixture's test bodies are already empty: a marked, passing test that checks nothing.
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sabotage));
        }
    }
}
