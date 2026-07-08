// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC002: <c>var</c> is not allowed.</summary>
internal static class Csc002_NoVar
{
    public static int M()
    {
        var value = 1;
        return value;
    }
}
