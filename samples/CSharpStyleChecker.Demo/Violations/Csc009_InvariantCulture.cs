// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC009: bare interpolated strings and culture-sensitive <c>ToString</c> without invariant culture.</summary>
internal static class Csc009_InvariantCulture
{
    public static string Format(int value)
    {
        return $"value={value}";
    }

    public static string FormatNumber(int value)
    {
        return value.ToString();
    }
}
