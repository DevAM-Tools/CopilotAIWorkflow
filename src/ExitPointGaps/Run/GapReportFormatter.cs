// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Formats exit-point gap reports for CLI output.</summary>
internal static class GapReportFormatter
{
    public static string Format(ExitPointGapReport report, string format)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (format.Equals("compact", StringComparison.OrdinalIgnoreCase))
        {
            return ExitPointGapReportFormatter.ToCompact(report);
        }

        if (format.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            return ExitPointGapReportFormatter.ToText(report);
        }

        return ExitPointGapReportFormatter.ToAgentJson(report);
    }
}
