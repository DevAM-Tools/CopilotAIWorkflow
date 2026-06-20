// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Runs the <c>run</c> command.</summary>
internal static class RunCommand
{
    public static Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        string[] effectiveArgs = args.Length == 0
            ? []
            : args;

        if (!CliParser.TryParse(_PrependRunWhenNeeded(effectiveArgs), out CliOptions? options, out string? parseError)
            || options is null)
        {
            return _WriteUsageAndReturnAsync(parseError, CliConstants.ExitUsageError);
        }

        options.Command = CliConstants.RunCommand;
        return GateOrchestrator.RunAsync(options, cancellationToken);
    }

    private static string[] _PrependRunWhenNeeded(string[] args)
    {
        if (args.Length == 0)
        {
            return args;
        }

        if (string.Equals(args[0], CliConstants.RunCommand, StringComparison.OrdinalIgnoreCase)
            || string.Equals(args[0], CliConstants.PlanCommand, StringComparison.OrdinalIgnoreCase))
        {
            return args;
        }

        return args;
    }

    private static async Task<int> _WriteUsageAndReturnAsync(string? error, int exitCode)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            await Console.Error.WriteLineAsync(error).ConfigureAwait(false);
        }

        await CliUsage.WriteAsync().ConfigureAwait(false);
        return exitCode;
    }
}
