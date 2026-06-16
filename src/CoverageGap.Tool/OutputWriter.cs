// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

internal static class OutputWriter
{
    public static async Task WriteAsync(
        string output,
        string? outputPath,
        bool appendNewLine,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            if (!string.IsNullOrEmpty(output))
            {
                if (appendNewLine)
                {
                    await Console.Out.WriteLineAsync(output).ConfigureAwait(false);
                }
                else
                {
                    await Console.Out.WriteAsync(output).ConfigureAwait(false);
                }
            }

            return;
        }

        await File.WriteAllTextAsync(outputPath, output, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }
}
