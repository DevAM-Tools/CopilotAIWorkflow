// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleValidator.Demo.Violations;

/// <summary>CSV002: <c>var</c> is not allowed.</summary>
internal static class Csv002_NoVar
{
    public static int M()
    {
        var value = 1;
        return value;
    }
}
