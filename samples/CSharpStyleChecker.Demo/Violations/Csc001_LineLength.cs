// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC001: visible line length exceeds the configured maximum.</summary>
internal static class Csc001_LineLength
{
    private static int _Value = 1;

    public static int M()
    {
        return _Value + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 + 17 + 18 + 19 + 20 + 21 + 22 + 23 + 24 + 25 + 26 + 27 + 28 + 29 + 30 + 31 + 32 + 33 + 34 + 35;
    }
}
