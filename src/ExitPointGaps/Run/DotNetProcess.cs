// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Runs <c>dotnet</c> subprocesses.</summary>
[ExcludeFromCodeCoverage]
internal static class DotNetProcess
{
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

    /// <summary>Synchronous wrapper used by compilation loader.</summary>
    /// <param name="arguments">Arguments after <c>dotnet</c>.</param>
    /// <param name="workingDirectory">Working directory.</param>
    /// <param name="error">Error text when exit code is non-zero.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Process exit code.</returns>
    public static int Run(
        string arguments,
        string workingDirectory,
        out string? error,
        CancellationToken cancellationToken = default)
    {
        error = null;
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

        string stdout = string.Empty;
        string stderr = string.Empty;
        Thread stdoutThread = new(() => stdout = process.StandardOutput.ReadToEnd())
        {
            Name = "DotNetProcess-stdout",
            IsBackground = true,
        };
        Thread stderrThread = new(() => stderr = process.StandardError.ReadToEnd())
        {
            Name = "DotNetProcess-stderr",
            IsBackground = true,
        };
        stdoutThread.Start();
        stderrThread.Start();
        process.WaitForExit();
        stdoutThread.Join();
        stderrThread.Join();
        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
        {
            error = !string.IsNullOrWhiteSpace(stderr)
                ? stderr.Trim()
                : stdout.Trim();
        }

        return process.ExitCode;
    }
}

/// <summary>Captured output from a dotnet subprocess.</summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="StandardOutput">Standard output text.</param>
/// <param name="StandardError">Standard error text.</param>
internal sealed record DotNetProcessResult(int ExitCode, string StandardOutput, string StandardError);
