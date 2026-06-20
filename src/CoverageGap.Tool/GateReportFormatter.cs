// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Formats coverage gap reports for CLI output.</summary>
internal static class GateReportFormatter
{
    public static string Format(CoverageGapReport report, string format)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (format.Equals("compact", StringComparison.OrdinalIgnoreCase))
        {
            return CoverageGapReportFormatter.ToCompact(report);
        }

        if (format.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            return CoverageGapReportFormatter.ToText(report);
        }

        return CoverageGapReportFormatter.ToAgentJson(report);
    }
}
