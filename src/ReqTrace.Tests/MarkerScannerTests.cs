// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests;

/// <summary>Finding markers across languages and skipping what must be skipped.</summary>
public sealed class MarkerScannerTests
{
    /// <summary>Runs the scanner with no ignored paths.</summary>
    /// <param name="workspace">The repository.</param>
    /// <returns>The markers.</returns>
    private static List<Marker> _Scan(TraceWorkspace workspace)
    {
        List<Finding> findings = [];
        return MarkerScanner.Scan(workspace.Root, IgnoreList.Read(workspace.Root, findings));
    }

    [Test, Property("Card", "TEST-TRACE-MARKER-LANGUAGES")]
    public async Task EachLanguageYieldsIdFileLineAndMethod()
    {
        using TraceWorkspace workspace = new();
        workspace.Write("t/A.cs", "// header\n[Test, Property(\"Card\", \"TEST-A-CS\")]\n[Arguments(1)]\n[Arguments(2)]\npublic async Task CsTest(int value)\n{\n}\n");
        workspace.Write("t/b.py", "@pytest.mark.card(\"TEST-A-PY\")\n@pytest.mark.slow\ndef test_python() -> None:\n    pass\n");
        workspace.Write("t/c.rs", "// card: TEST-A-RS\n#[test]\nfn rust_test() {\n}\n");

        List<Marker> markers = _Scan(workspace);

        await Assert.That(markers.Count).IsEqualTo(3);
        await Assert.That(markers[0]).IsEqualTo(new Marker("TEST-A-CS", "t/A.cs", 2, "CsTest"));
        await Assert.That(markers[1]).IsEqualTo(new Marker("TEST-A-PY", "t/b.py", 1, "test_python"));
        await Assert.That(markers[2]).IsEqualTo(new Marker("TEST-A-RS", "t/c.rs", 1, "rust_test"));
    }

    [Test]
    public async Task MarkerWithoutMethodInWindowIsKeptAndReported()
    {
        using TraceWorkspace workspace = new();
        workspace.WriteRequirements(TraceWorkspace.Requirement("REQ-AREA-ONE"));
        workspace.WriteCards(TraceWorkspace.Card("TEST-AREA-ONE", "REQ-AREA-ONE"));
        workspace.Write("t/A.cs", "[Test, Property(\"Card\", \"TEST-AREA-ONE\")]\n\n\n\n\n\n\n\n\n\n\npublic async Task TooFar()\n{\n}\n");

        (int exitCode, string output, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("L5");
    }

    [Test]
    public async Task LiteralSplitAcrossReadBufferIsFound()
    {
        using TraceWorkspace workspace = new();
        const int BufferSize = 65536;
        string filler = new('x', BufferSize - 12);
        workspace.Write("t/A.cs", "// " + filler + "\n[Test, Property(\"Card\", \"TEST-A-EDGE\")]\npublic async Task Edge()\n{\n}\n");

        List<Marker> markers = _Scan(workspace);

        await Assert.That(markers.Count).IsEqualTo(1);
        await Assert.That(markers[0].CardId).IsEqualTo("TEST-A-EDGE");
    }

    [Test]
    public async Task ManyFilesAreScannedInStablePathOrder()
    {
        using TraceWorkspace workspace = new();
        const int FileCount = 130;
        for (int index = 0; index < FileCount; index++)
        {
            string name = Invariant($"t/F{index:D3}.cs");
            workspace.Write(name, TraceWorkspace.MarkedTest(Invariant($"TEST-A-N{index:D3}"), "M"));
        }

        List<Marker> markers = _Scan(workspace);

        await Assert.That(markers.Count).IsEqualTo(FileCount);
        await Assert.That(markers.Select(static marker => marker.File)).IsOrderedBy(static file => file, StringComparer.Ordinal);
    }

    [Test, Property("Card", "TEST-TRACE-IGNORE-PATH")]
    public async Task IgnoredPathsNeedReasonsAndMayNotLeaveTheRoot()
    {
        using TraceWorkspace workspace = new();
        workspace.Write("spec/trace.ignore", "fixtures — sample markers\n");
        workspace.Write("fixtures/A.cs", TraceWorkspace.MarkedTest("TEST-A-ONE", "M"));
        workspace.Write("obj/B.cs", TraceWorkspace.MarkedTest("TEST-A-TWO", "M"));
        workspace.Write("bin/C.cs", TraceWorkspace.MarkedTest("TEST-A-THREE", "M"));
        workspace.Write("target/D.rs", "// card: TEST-A-FOUR\nfn d() {}\n");
        workspace.Write("node_modules/E.py", "@pytest.mark.card(\"TEST-A-FIVE\")\ndef e(): pass\n");
        workspace.Write(".git/F.cs", TraceWorkspace.MarkedTest("TEST-A-SIX", "M"));

        (_, string output, _) = workspace.Run();
        (_, string report, _) = workspace.Run("report");

        await Assert.That(output).Contains("0 markers · 1 ignored paths");
        await Assert.That(report).Contains("fixtures: sample markers");

        workspace.Write("spec/trace.ignore", "no-reason-here\n../outside — tries to leave\n");

        (int exitCode, string bad, _) = workspace.Run();

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(bad).Contains("a reason is required");
        await Assert.That(bad).Contains("must stay inside the repository");
    }
}
