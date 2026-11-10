// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>What a check concluded, ready to print.</summary>
/// <param name="Findings">Every finding, errors and open gaps.</param>
/// <param name="Requirements">Number of requirements read.</param>
/// <param name="Cards">Number of test cards read.</param>
/// <param name="Markers">Number of markers found.</param>
/// <param name="IgnoredPaths">Number of ignored paths.</param>
/// <param name="EvidenceChecked">Whether a result file was compared.</param>
/// <param name="HasAcceptedState">Whether an accepted state exists.</param>
internal sealed record CheckResult(
    IReadOnlyList<Finding> Findings,
    int Requirements,
    int Cards,
    int Markers,
    int IgnoredPaths,
    bool EvidenceChecked,
    bool HasAcceptedState);

/// <summary>Prints check results for an agent or a person.</summary>
/// <remarks>Sync: docs/agents.md § JSON output.</remarks>
internal static class FindingWriter
{
    /// <summary>The version of the JSON contract.</summary>
    private const int _SchemaVersion = 1;

    /// <summary>Writes the text form: errors with location and fix, then the counts.</summary>
    /// <param name="output">Receives the text.</param>
    /// <param name="result">The result.</param>
    public static void WriteText(TextWriter output, CheckResult result)
    {
        int errors = 0;
        int gaps = 0;
        for (int index = 0; index < result.Findings.Count; index++)
        {
            Finding finding = result.Findings[index];
            if (finding.Severity == Severity.OpenGap)
            {
                gaps++;
                continue;
            }

            errors++;
            string location = finding.Line > 0
                ? Invariant($"{finding.File}:{finding.Line}")
                : finding.File;
            output.WriteLine(Invariant($"{location}  {finding.Rule}  {finding.Message}"));
            output.WriteLine(Invariant($"  Fix: {finding.Fix}"));
        }

        output.WriteLine(Invariant(
            $"Read: {result.Requirements} requirements · {result.Cards} test cards · {result.Markers} markers · {result.IgnoredPaths} ignored paths"));
        if (!result.EvidenceChecked)
        {
            output.WriteLine("Evidence not checked: no --results given.");
        }

        if (!result.HasAcceptedState)
        {
            output.WriteLine("No accepted state yet. Run accept to record the starting point.");
        }

        output.WriteLine(Invariant($"Result: {errors} error{(errors == 1 ? string.Empty : "s")} · {gaps} open gap{(gaps == 1 ? string.Empty : "s")}"));
    }

    /// <summary>Writes the JSON form.</summary>
    /// <param name="output">Receives the JSON, with nothing before or after it but a line end.</param>
    /// <param name="result">The result.</param>
    public static void WriteJson(TextWriter output, CheckResult result)
    {
        ArrayBufferWriter<byte> buffer = new(4096);
        using (Utf8JsonWriter json = new(buffer))
        {
            json.WriteStartObject();
            json.WriteNumber("schemaVersion", _SchemaVersion);
            json.WriteBoolean("evidenceChecked", result.EvidenceChecked);
            json.WriteBoolean("hasAcceptedState", result.HasAcceptedState);
            json.WriteStartObject("counts");
            json.WriteNumber("requirements", result.Requirements);
            json.WriteNumber("testCards", result.Cards);
            json.WriteNumber("markers", result.Markers);
            json.WriteNumber("ignoredPaths", result.IgnoredPaths);
            json.WriteEndObject();
            json.WriteStartArray("findings");
            for (int index = 0; index < result.Findings.Count; index++)
            {
                Finding finding = result.Findings[index];
                json.WriteStartObject();
                json.WriteString("rule", finding.Rule);
                json.WriteString("severity", finding.Severity == Severity.Error ? "error" : "openGap");
                json.WriteString("file", finding.File);
                json.WriteNumber("line", finding.Line);
                json.WriteString("message", finding.Message);
                json.WriteString("fix", finding.Fix);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
