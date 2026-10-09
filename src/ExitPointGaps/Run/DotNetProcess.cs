// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Runs <c>dotnet</c> subprocesses. Reason: process I/O boundaries are covered by dedicated DotNetProcess and integration tests.</summary>
[ExcludeFromCodeCoverage]
internal static class DotNetProcess
{
    #region Public API

    /// <summary>Runs a dotnet command and captures output.</summary>
    /// <param name="arguments">Arguments after <c>dotnet</c>.</param>
    /// <param name="workingDirectory">Working directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code and captured streams.</returns>
    public static async Task<DotNetProcessResult> RunAsync(
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(arguments);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(startInfo)!;
        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }
        });

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(cancellationToken)).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return new DotNetProcessResult(process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
    }

    #endregion
}

/// <summary>Captured output from a dotnet subprocess.</summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="StandardOutput">Standard output text.</param>
/// <param name="StandardError">Standard error text.</param>
internal sealed record DotNetProcessResult(int ExitCode, string StandardOutput, string StandardError);
