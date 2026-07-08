// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC006: at most one callable exit point per source line.</summary>
internal static class Csc006_MultipleExits
{
    public static int M(bool a, bool b)
    {
        if (a)
        {
            return 1;
        }

        if (b)
        {
            return 2;
        }

        return 0;
    }

    public static int MSameLine(bool a, bool b)
    {
        if (a) return 1; if (b) return 2; return 0;
    }
}
