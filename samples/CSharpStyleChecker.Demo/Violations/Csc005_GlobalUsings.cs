// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC005: <c>using</c> directives belong in <c>GlobalUsings.cs</c> only.</summary>
internal static class Csc005_GlobalUsings
{
    public static int M()
    {
        List<int> values = [1];
        return values.Count;
    }
}
