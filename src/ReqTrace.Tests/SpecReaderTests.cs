// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>Closed-world reading of the spec.</summary>
public sealed class SpecReaderTests
{
    [Test, Property("Card", "TEST-TRACE-UNREADABLE-LINE")]
    public async Task UnknownLinesAreReportedWithFileLineAndExpectedForm()
    {
        using TraceWorkspace workspace = new();
        workspace.Write(
            "spec/requirements/a.md",
            "# Title\n\nAn introduction is fine here.\n\n## User stories\n\n- As a person I do things.\n\n### Area\n\n"
            + TraceWorkspace.Requirement("REQ-AREA-ONE")
            + "Note: stray text.\n"
            + TraceWorkspace.Requirement("REQ-AREA-TWO")
            + "  - **Unknown:** a field nobody knows\n");

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("spec/requirements/a.md:15  F1");
        await Assert.That(output).Contains("spec/requirements/a.md:20  F1");
        await Assert.That(output).Contains("Fix: Write a field");
        await Assert.That(output).DoesNotContain("a.md:3 ");
        await Assert.That(output).DoesNotContain("a.md:7 ");
        await Assert.That(output).Contains("Read: 2 requirements");
    }

    [Test, Property("Card", "TEST-TRACE-NEAR-MISS")]
    [Arguments("- **req-export-x** — The system shall work.")]
    [Arguments("- **REQ_EXPORT_X** — The system shall work.")]
    [Arguments("- **REQ-1** — The system shall work.")]
    [Arguments("* *REQ-EXPORT-X* — The system shall work.")]
    public async Task MalformedIdsAreReportedAndValidNeighborsStillRead(string malformed)
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(malformed + "\n" + TraceWorkspace.Requirement("REQ-AREA-VALID"));

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("F2");
        await Assert.That(output).Contains("looks like an ID");
        await Assert.That(output).Contains("Read: 1 requirements");
    }

    [Test, Property("Card", "TEST-TRACE-MISSING-FIELD")]
    public async Task MissingFieldsAndBadValuesNameTheEntryAndField()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(
            "- **REQ-AREA-NODONE** — Text.\n  - **State:** active · **Met:** ⬜\n  - **Proof:** test\n"
            + "- **REQ-AREA-BADSTATE** — Text.\n  - **Done when:** It works.\n  - **State:** maybe · **Met:** ⬜\n  - **Proof:** test\n"
            + "- **REQ-AREA-NOPROOF** — Text.\n  - **Done when:** It works.\n  - **State:** active · **Met:** ⬜\n");
        workspace.WriteCards("### TEST-AREA-NOPROVES\n\n- **Content:** A scenario.\n- **State:** active · **Status:** ⬜\n");

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("REQ-AREA-NODONE has no Done when");
        await Assert.That(output).Contains("REQ-AREA-BADSTATE: invalid value in State");
        await Assert.That(output).Contains("REQ-AREA-NOPROOF has no Proof");
        await Assert.That(output).Contains("TEST-AREA-NOPROVES has no Proves");
    }

    [Test, Property("Card", "TEST-TRACE-FIX-SHOWS-FORM")]
    public async Task FixesQuoteTheExpectedFormAndValues()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(
            "- **REQ-AREA-NOPROOF** — Text.\n  - **Done when:** It works.\n  - **State:** active · **Met:** ⬜\n"
            + "- **REQ-AREA-BADPROOF** — Text.\n  - **Done when:** It works.\n  - **State:** active · **Met:** ⬜\n"
            + "  - **Proof:** maybe\n"
            + "- **REQ-AREA-BADMET** — Text.\n  - **Done when:** It works.\n  - **State:** active · **Met:** 🟢\n"
            + "  - **Proof:** test\n");
        workspace.WriteCards(
            "### TEST-AREA-BADCLASS\n\n- **Proves:** REQ-AREA-NOPROOF\n- **Class:** smoke\n- **Content:** A scenario.\n"
            + "- **State:** active · **Status:** ⬜\n\n"
            + "### TEST-AREA-NOCONTENT\n\n- **Proves:** REQ-AREA-NOPROOF\n- **State:** active · **Status:** ⬜\n");

        (int exitCode, string output, _) = workspace.Run("check", "--json");

        using JsonDocument document = JsonDocument.Parse(output);
        List<(string Rule, string Message, string Fix)> findings = [];
        foreach (JsonElement finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            findings.Add((
                finding.GetProperty("rule").GetString()!, // the JSON contract always writes rule, message, and fix
                finding.GetProperty("message").GetString()!,
                finding.GetProperty("fix").GetString()!));
        }

        string FixOf(string rule, string messageStart)
        {
            return findings
                .Single(f => f.Rule == rule && f.Message.StartsWith(messageStart, StringComparison.Ordinal))
                .Fix;
        }

        string missingProof = FixOf("F3", "REQ-AREA-NOPROOF has no Proof");
        string badProof = FixOf("F4", "REQ-AREA-BADPROOF: invalid value in Proof");
        string badMet = FixOf("F4", "REQ-AREA-BADMET: invalid value in Met");
        string badClass = FixOf("F4", "TEST-AREA-BADCLASS: invalid value in Class");
        string missingContent = FixOf("F3", "TEST-AREA-NOCONTENT has no Content");
        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(missingProof).Contains("\"  - **Proof:** test\"");
        await Assert.That(missingProof).Contains("reqtrace syntax requirement");
        await Assert.That(badProof).Contains("test | demo — {reason} | review — {reason} | waived — {reason}");
        await Assert.That(badProof).Contains("reqtrace syntax requirement");
        await Assert.That(badMet).Contains("⬜ | ✅ | ❌");
        await Assert.That(badClass).Contains("happy | error | boundary | collection | absence | concurrency | trust-boundary");
        await Assert.That(badClass).Contains("reqtrace syntax card");
        await Assert.That(missingContent).Contains("Add the line \"- **Content:** ");
        await Assert.That(missingContent).Contains("reqtrace syntax card");
        foreach ((_, _, string fix) in findings)
        {
            await Assert.That(fix).DoesNotContain("tech-spec.md");
        }
    }

    [Test, Property("Card", "TEST-TRACE-COUNTS")]
    public async Task EmptySpecPassesWithZeroCounts()
    {
        using TraceWorkspace workspace = new();

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Read: 0 requirements · 0 test cards · 0 markers · 0 ignored paths");
    }

    [Test]
    public async Task EntriesAboveTheFirstHeadingAreReadAndBrokenOnesReported()
    {
        using TraceWorkspace workspace = new();
        workspace.Write("spec/requirements/a.md", TraceWorkspace.Requirement("REQ-AREA-TOP") + "- **req-area-low** — Text.\n\n### Area\n\n" + TraceWorkspace.Requirement("REQ-AREA-BELOW"));

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("F2");
        await Assert.That(output).Contains("Read: 2 requirements");
    }

    [Test]
    public async Task WrappedFieldLinesJoinTheField()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements("- **REQ-AREA-WRAP** — Text.\n  - **Done when:** A person\n      sees it.\n  - **State:** active · **Met:** ⬜\n  - **Proof:** test\n");

        (int exitCode, _, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task FileWithoutEntriesIsReported()
    {
        using TraceWorkspace workspace = new();
        workspace.Write("spec/requirements/a.md", "| ID | Text |\n|---|---|\n| A | B |\n");

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("F5");
    }
}
