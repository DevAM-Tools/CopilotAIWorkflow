// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>References between requirements, cards, and tests.</summary>
public sealed class LinkCheckerTests
{
    [Test, Property("Card", "TEST-TRACE-DUPLICATE-ID")]
    public async Task DuplicatesNameBothPlaces()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-SAME"));
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-SAME"), "b");
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-TWICE", "REQ-AREA-SAME") + TraceWorkspace.Card("TEST-AREA-TWICE", "REQ-AREA-SAME"));

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("REQ-AREA-SAME heads two entries, also at spec/requirements/a.md:3");
        await Assert.That(output).Contains("spec/requirements/b.md:3");
        await Assert.That(output).Contains("TEST-AREA-TWICE heads two entries");
    }

    [Test, Property("Card", "TEST-TRACE-UNKNOWN-REFERENCE")]
    public async Task DanglingLinksAreReportedAndMisspellingsSuggested()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-GAPS-EXIT-GAP") + "\n## Retired\n\n- **REQ-GAPS-OLD** — Old.\n  - **State:** retired\n");
        workspace.WriteCards(
            TraceWorkspace.Card("TEST-GAPS-EXIT-GAP", "REQ-GAPS-EXIT-GAP")
            + TraceWorkspace.Card("TEST-GAPS-RETIRED", "REQ-GAPS-OLD")
            + TraceWorkspace.Card("TEST-GAPS-MISSING", "REQ-GAPS-NOPE"));
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-GAPS-EXIT-GAPP", "A") + TraceWorkspace.MarkedTest("TEST-TOTALLY-OTHER-THING", "B"));

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("Did you mean TEST-GAPS-EXIT-GAP?");
        await Assert.That(output).Contains("tests/A.cs:1  L3");
        await Assert.That(output).Contains("TEST-GAPS-RETIRED proves REQ-GAPS-OLD");
        await Assert.That(output).Contains("TEST-GAPS-MISSING proves REQ-GAPS-NOPE");
        await Assert.That(output).Contains("Name an active test card.");
    }

    [Test, Property("Card", "TEST-TRACE-UNPROVEN")]
    public async Task UnprovenEntriesAreOpenGapsWithExactBoundaries()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(
            TraceWorkspace.Requirement("REQ-AREA-NOCARD")
            + TraceWorkspace.Requirement("REQ-AREA-ONLYRETIRED")
            + TraceWorkspace.Requirement("REQ-AREA-ONEACTIVE"));
        workspace.WriteCards(
            "### TEST-AREA-RETIRED\n\n- **Proves:** REQ-AREA-ONLYRETIRED\n- **Content:** C.\n- **State:** retired\n\n"
            + "### TEST-AREA-RETIRED-TWO\n\n- **Proves:** REQ-AREA-ONEACTIVE\n- **Content:** C.\n- **State:** retired\n\n"
            + TraceWorkspace.Card("TEST-AREA-ACTIVE", "REQ-AREA-ONEACTIVE")
            + TraceWorkspace.Card("TEST-AREA-UNMARKED", "REQ-AREA-ONEACTIVE"));
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-ACTIVE", "A") + TraceWorkspace.MarkedTest("TEST-AREA-ACTIVE", "B"));

        (int exitCode, string output, _) = workspace.Run("check", "--json");

        using JsonDocument document = JsonDocument.Parse(output);
        string[] messages = document.RootElement.GetProperty("findings").EnumerateArray()
            .Select(static finding => finding.GetProperty("message").GetString()!).ToArray();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(messages).Contains("REQ-AREA-NOCARD has no active test card.");
        await Assert.That(messages).Contains("REQ-AREA-ONLYRETIRED has no active test card.");
        await Assert.That(messages).Contains("TEST-AREA-UNMARKED has no marked test.");
        await Assert.That(messages.Length).IsEqualTo(3);
    }

    [Test, Property("Card", "TEST-TRACE-PROOF-KINDS")]
    public async Task OtherProofKindsNeedNoTestButNeedAReason()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(
            TraceWorkspace.Requirement("REQ-AREA-DEMO", proof: "demo — a person runs it")
            + TraceWorkspace.Requirement("REQ-AREA-REVIEW", proof: "review — only a person can judge it")
            + TraceWorkspace.Requirement("REQ-AREA-WAIVED", proof: "waived — not worth proving"));

        (int exitCode, string output, _) = workspace.Run();
        (_, string report, _) = workspace.Run("report");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("0 open gaps");
        await Assert.That(report).Contains("REQ-AREA-REVIEW: review — only a person can judge it");

        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-BARE", proof: "review") + TraceWorkspace.Requirement("REQ-AREA-ODD", proof: "vibes — good"));

        (int badExit, string badOutput, _) = workspace.Run();

        await Assert.That(badExit).IsEqualTo(1);
        await Assert.That(badOutput).Contains("REQ-AREA-BARE: invalid value in Proof");
        await Assert.That(badOutput).Contains("REQ-AREA-ODD: invalid value in Proof");
    }

    [Test, Property("Card", "TEST-TRACE-NO-PRODUCTION-MARKS")]
    public async Task ProductionFilesNeedNoMarks()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE"));
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-ONE", "REQ-AREA-ONE"));
        workspace.Write("src/Production.cs", "internal static class Production { }\n");
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-ONE", "A"));
        workspace.Run("accept");

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).DoesNotContain("Production.cs");
    }

    [Test, Property("Card", "TEST-TRACE-SHOW")]
    public async Task ShowListsTextLinksAndTests()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE", "The system shall answer."));
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-ONE", "REQ-AREA-ONE"));
        workspace.Write("tests/A.cs", TraceWorkspace.MarkedTest("TEST-AREA-ONE", "AnswersQuestion"));

        (int requirementExit, string requirementOutput, _) = workspace.Run("show", "REQ-AREA-ONE");
        (_, string cardOutput, _) = workspace.Run("show", "TEST-AREA-ONE");
        (int unknownExit, _, string unknownError) = workspace.Run("show", "REQ-AREA-ONF");

        await Assert.That(requirementExit).IsEqualTo(0);
        await Assert.That(requirementOutput).Contains("The system shall answer.");
        await Assert.That(requirementOutput).Contains("Card: TEST-AREA-ONE");
        await Assert.That(requirementOutput).Contains("tests/A.cs:1 AnswersQuestion");
        await Assert.That(cardOutput).Contains("Proves: REQ-AREA-ONE");
        await Assert.That(unknownExit).IsEqualTo(2);
        await Assert.That(unknownError).Contains("Did you mean REQ-AREA-ONE?");
    }
}
