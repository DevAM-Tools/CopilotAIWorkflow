// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Formats exit-point manifests for <c>plan</c>.</summary>
internal static class ExitManifestFormatter
{
    private static readonly JsonSerializerOptions _JsonOptions = new() { WriteIndented = true };

    public static string Format(IReadOnlyList<ExitPointEntry> exits, string format)
    {
        ArgumentNullException.ThrowIfNull(exits);

        if (format.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            return _FormatText(exits);
        }

        return JsonSerializer.Serialize(exits, _JsonOptions);
    }

    private static string _FormatText(IReadOnlyList<ExitPointEntry> exits)
    {
        StringBuilder builder = new();
        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPointEntry exit = exits[exitIndex];
            builder.Append(exit.ExitPointId)
                .Append(' ')
                .Append(exit.FilePath)
                .Append(':')
                .Append(exit.Line)
                .Append(':')
                .Append(exit.Column)
                .Append(' ')
                .Append(exit.MethodDisplayName)
                .Append(' ')
                .AppendLine(exit.Kind.ToString());
        }

        return builder.ToString();
    }
}
