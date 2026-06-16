// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Coverage gap CLI entry point.</summary>
public static class Program
{
    /// <summary>Runs the coverage gap tool.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        if (args.Length == 0)
        {
            await _WriteUsageAsync().ConfigureAwait(false);
            return 1;
        }

        string command = args[0];
        string[] commandArgs = args.Skip(1).ToArray();

        if (string.Equals(command, "report", StringComparison.OrdinalIgnoreCase))
        {
            return await ReportCommand.RunAsync(commandArgs).ConfigureAwait(false);
        }

        if (string.Equals(command, "manifest", StringComparison.OrdinalIgnoreCase))
        {
            return await ManifestCommand.RunAsync(commandArgs).ConfigureAwait(false);
        }

        await _WriteUsageAsync().ConfigureAwait(false);
        return 1;
    }

    /// <remarks>Usage text only; invoked from <see cref="Main"/> error paths covered by CLI tests.</remarks>
    [ExcludeFromCodeCoverage]
    private static async Task _WriteUsageAsync()
    {
        await Console.Error.WriteLineAsync(
            """
            Usage:
              coveragegap report project <path.csproj> [options]
              coveragegap manifest project <path.csproj> [-o <file>] [--format agent|text]

            Report options:
              --search-root <path> --cobertura <file> --repo-root <path> --scope <suffix>
              -o <file> --format agent|compact|text --include-snippet --no-fail --no-build
            """).ConfigureAwait(false);
    }
}
