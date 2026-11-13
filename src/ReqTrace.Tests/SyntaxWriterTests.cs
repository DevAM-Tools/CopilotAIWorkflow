// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>The syntax command: printed forms read back cleanly, every accepted value is printed, kinds pick sections.</summary>
public sealed class SyntaxWriterTests
{
    [Test, Property("Card", "TEST-TRACE-SYNTAX")]
    public async Task PrintedFormsReadBackWithoutFindings()
    {
        (_, string printed, _) = TraceWorkspace.RunFrom(Path.GetTempPath(), "syntax");
        string output = printed.ReplaceLineEndings("\n");
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(SpecForm.RequirementExample + "\n");
        workspace.WriteCards(SpecForm.CardExample + "\n");
        workspace.Write("spec/trace.ignore", SpecForm.IgnoreExample + "\n");

        (int exitCode, string check, _) = workspace.Run();

        await Assert.That(output).Contains(SpecForm.RequirementExample);
        await Assert.That(output).Contains(SpecForm.CardExample);
        await Assert.That(output).Contains(SpecForm.IgnoreExample);
        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(check).Contains("Read: 1 requirements · 1 test cards");
        await Assert.That(check).DoesNotContain("L7");
    }

    [Test, Property("Card", "TEST-TRACE-SYNTAX-MARKER")]
    public async Task PrintedMarkersAreFound()
    {
        (_, string output, _) = TraceWorkspace.RunFrom(Path.GetTempPath(), "syntax", "marker");
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(SpecForm.RequirementExample + "\n");
        workspace.WriteCards(SpecForm.CardExample + "\n");
        workspace.Write("t/A.cs", SpecForm.CSharpMarker + "\npublic async Task AreaTopic()\n{\n}\n");
        workspace.Write("t/test_a.py", SpecForm.PythonMarker + "\ndef test_area() -> None:\n    pass\n");
        workspace.Write("t/a.rs", SpecForm.RustMarker + "\n#[test]\nfn area() {\n}\n");

        (int exitCode, string check, _) = workspace.Run();

        await Assert.That(output).Contains(SpecForm.CSharpMarker);
        await Assert.That(output).Contains(SpecForm.PythonMarker);
        await Assert.That(output).Contains(SpecForm.RustMarker);
        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(check).Contains("· 3 markers ·");
    }

    [Test, Property("Card", "TEST-TRACE-SYNTAX-CLOSED-SETS")]
    public async Task EveryAcceptedValueIsPrinted()
    {
        IEnumerable<string>[] sets =
        [
            SpecForm.States,
            SpecForm.MetMarks,
            SpecForm.StatusMarks,
            SpecForm.Classes,
            SpecForm.Layers,
            SpecForm.ReasonProofKinds,
            [SpecForm.ProofTest],
        ];

        (int exitCode, string output, _) = TraceWorkspace.RunFrom(Path.GetTempPath(), "syntax");

        await Assert.That(exitCode).IsEqualTo(0);
        foreach (IEnumerable<string> set in sets)
        {
            foreach (string value in set)
            {
                await Assert.That(output).Contains(value);
            }
        }
    }

    [Test, Property("Card", "TEST-TRACE-SYNTAX-KINDS")]
    public async Task AllKindsPrintInOrderWithoutARepository()
    {
        string directory = Path.Join(Path.GetTempPath(), "reqtrace-syntax-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        try
        {
            (int exitCode, string output, _) = TraceWorkspace.RunFrom(directory, "syntax");

            int requirement = output.IndexOf("requirement — ", StringComparison.Ordinal);
            int card = output.IndexOf("card — ", StringComparison.Ordinal);
            int marker = output.IndexOf("marker — ", StringComparison.Ordinal);
            int ignore = output.IndexOf("ignore — ", StringComparison.Ordinal);
            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(requirement).IsEqualTo(0);
            await Assert.That(card).IsGreaterThan(requirement);
            await Assert.That(marker).IsGreaterThan(card);
            await Assert.That(ignore).IsGreaterThan(marker);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test, Property("Card", "TEST-TRACE-SYNTAX-KINDS")]
    [Arguments("requirement")]
    [Arguments("card")]
    [Arguments("marker")]
    [Arguments("ignore")]
    public async Task OneKindPrintsOnlyItsSection(string kind)
    {
        string[] headers = ["requirement — ", "card — ", "marker — ", "ignore — "];

        (int exitCode, string output, _) = TraceWorkspace.RunFrom(Path.GetTempPath(), "syntax", kind);

        await Assert.That(exitCode).IsEqualTo(0);
        foreach (string header in headers)
        {
            bool expected = header.StartsWith(kind + " ", StringComparison.Ordinal);
            await Assert.That(output.Contains(header, StringComparison.Ordinal)).IsEqualTo(expected);
        }
    }

    [Test, Property("Card", "TEST-TRACE-SYNTAX-UNKNOWN-KIND")]
    [Arguments("frobnicate", "")]
    [Arguments("card", "extra")]
    public async Task UnknownKindOrExtraArgumentExitsTwo(string kind, string extra)
    {
        string[] args = extra.Length == 0
            ? ["syntax", kind]
            : ["syntax", kind, extra];

        (int exitCode, string output, string error) = TraceWorkspace.RunFrom(Path.GetTempPath(), args);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output).IsEmpty();
        foreach (string word in new[] { "requirement", "card", "marker", "ignore" })
        {
            await Assert.That(error).Contains(word);
        }
    }
}
