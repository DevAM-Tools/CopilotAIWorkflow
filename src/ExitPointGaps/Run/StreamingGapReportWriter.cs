// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Writes per-project reports incrementally and emits schema v3 summaries.</summary>
internal sealed class StreamingGapReportWriter
{
    private static readonly object _StdoutLock = new();
    private readonly bool _StreamToStdout;
    private readonly string _Format;

    /// <summary>Initializes the streaming writer.</summary>
    /// <param name="format">Report format.</param>
    /// <param name="streamToStdout">Whether to emit NDJSON lines to stdout.</param>
    public StreamingGapReportWriter(string format, bool streamToStdout)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        _Format = format;
        _StreamToStdout = streamToStdout;
    }

    /// <summary>Writes one per-project report file immediately.</summary>
    /// <param name="result">Project gap result.</param>
    /// <param name="outputDirectory">Output directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Written report file name without directory.</returns>
    public async Task<string> WriteProjectAsync(
        ProjectGapResult result,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        string reportFileName = $"{Path.GetFileNameWithoutExtension(result.ProjectPath)}.json";
        string projectPayload = GapReportFormatter.Format(result.Report, _Format);
        string projectFile = Path.Combine(outputDirectory, reportFileName);
        await File.WriteAllTextAsync(projectFile, projectPayload, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        if (_StreamToStdout)
        {
            await _WriteStdoutProjectLineAsync(result, projectPayload, cancellationToken).ConfigureAwait(false);
        }

        return reportFileName;
    }

    /// <summary>Writes the schema v3 summary file.</summary>
    /// <param name="accumulator">Summary accumulator.</param>
    /// <param name="expectedProjectCount">Expected project count.</param>
    /// <param name="outputDirectory">Output directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteSummaryAsync(
        RunSummaryAccumulator accumulator,
        int expectedProjectCount,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accumulator);
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        string payload = AggregatedGapReportSerializer.ToAgentJsonV3(accumulator, expectedProjectCount);
        string summaryPath = Path.Combine(outputDirectory, CliConstants.SummaryFileName);
        await File.WriteAllTextAsync(summaryPath, payload, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        if (_StreamToStdout)
        {
            await WriteStdoutSummaryAsync(accumulator, expectedProjectCount, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Writes a single-project report directly to a file path.</summary>
    /// <param name="result">Project gap result.</param>
    /// <param name="outputPath">Absolute output file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteSingleProjectFileAsync(
        ProjectGapResult result,
        string outputPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrEmpty(outputPath);

        string payload = GapReportFormatter.Format(result.Report, _Format);
        await File.WriteAllTextAsync(outputPath, payload, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes a single-project report to stdout.</summary>
    /// <param name="result">Project gap result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteSingleProjectStdoutAsync(ProjectGapResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        string payload = GapReportFormatter.Format(result.Report, _Format);
        await OutputWriter.WriteAsync(payload, null, appendNewLine: false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes the final NDJSON summary line to stdout.</summary>
    /// <param name="accumulator">Summary accumulator.</param>
    /// <param name="expectedProjectCount">Expected project count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteStdoutSummaryAsync(
        RunSummaryAccumulator accumulator,
        int expectedProjectCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accumulator);
        if (!_StreamToStdout)
        {
            return;
        }

        string summaryPayload = AggregatedGapReportSerializer.ToStdoutSummaryLine(accumulator, expectedProjectCount);
        lock (_StdoutLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.Out.WriteLine(summaryPayload);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task _WriteStdoutProjectLineAsync(
        ProjectGapResult result,
        string projectPayload,
        CancellationToken cancellationToken)
    {
        if (!_StreamToStdout)
        {
            return;
        }

        string line = AggregatedGapReportSerializer.ToStdoutProjectLine(result.ProjectPath, projectPayload);
        lock (_StdoutLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.Out.WriteLine(line);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
