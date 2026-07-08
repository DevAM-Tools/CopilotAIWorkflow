// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Exit-point gap CLI entry point.</summary>
public static class Program
{
    /// <summary>Runs the exitpointgaps tool.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Process exit code.</returns>
    public static Task<int> Main(string[] args)
    {
        return _MainAsync(args);
    }

    private static async Task<int> _MainAsync(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        using CancellationTokenSource cancellationSource = new();
        List<IDisposable> signalRegistrations = [];

        void Cancel()
        {
            cancellationSource.Cancel();
        }

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Cancel();
        };

        if (!OperatingSystem.IsWindows())
        {
            signalRegistrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGINT, _ => Cancel()));
            signalRegistrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => Cancel()));
        }

        try
        {
            if (args.Length == 0)
            {
                return await RunCommand.RunAsync(args, cancellationSource.Token).ConfigureAwait(false);
            }

            string command = args[0];
            string[] commandArgs = args.Skip(1).ToArray();

            if (string.Equals(command, CliConstants.RunCommand, StringComparison.OrdinalIgnoreCase))
            {
                return await RunCommand.RunAsync(commandArgs, cancellationSource.Token).ConfigureAwait(false);
            }

            if (string.Equals(command, CliConstants.PlanCommand, StringComparison.OrdinalIgnoreCase))
            {
                return await PlanCommand.RunAsync(commandArgs, cancellationSource.Token).ConfigureAwait(false);
            }

            return await RunCommand.RunAsync(args, cancellationSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return CliConstants.ExitGateOrToolFailure;
        }
        finally
        {
            for (int registrationIndex = 0; registrationIndex < signalRegistrations.Count; registrationIndex++)
            {
                signalRegistrations[registrationIndex].Dispose();
            }
        }
    }
}
