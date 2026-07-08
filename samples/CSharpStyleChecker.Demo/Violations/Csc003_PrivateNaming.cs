// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC003: private members must use <c>_PascalCase</c>.</summary>
internal static class Csc003_PrivateNaming
{
    private static int count = 1;

    public static int M()
    {
        return count;
    }
}
