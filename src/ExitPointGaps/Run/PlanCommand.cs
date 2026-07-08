// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Runs the <c>plan</c> command.</summary>
internal static class PlanCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (!CliParser.TryParse(_PrependPlan(args), out CliOptions? options, out string? parseError) || options is null)
        {
            if (!string.IsNullOrWhiteSpace(parseError))
            {
                await Console.Error.WriteLineAsync(parseError).ConfigureAwait(false);
            }

            await CliUsage.WriteAsync().ConfigureAwait(false);
            return CliConstants.ExitUsageError;
        }

        if (options.ShowHelp)
        {
            await CliUsage.WriteAsync().ConfigureAwait(false);
            return CliConstants.ExitSuccess;
        }

        options.Command = CliConstants.PlanCommand;
        return await PlanOrchestrator.RunAsync(options, cancellationToken).ConfigureAwait(false);
    }

    private static string[] _PrependPlan(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], CliConstants.PlanCommand, StringComparison.OrdinalIgnoreCase))
        {
            return args;
        }

        string[] withPlan = new string[args.Length + 1];
        withPlan[0] = CliConstants.PlanCommand;
        Array.Copy(args, 0, withPlan, 1, args.Length);
        return withPlan;
    }
}
