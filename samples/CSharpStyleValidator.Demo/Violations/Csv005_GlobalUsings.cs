// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;

namespace CSharpStyleValidator.Demo.Violations;

/// <summary>CSV005: <c>using</c> directives belong in <c>GlobalUsings.cs</c> only.</summary>
internal static class Csv005_GlobalUsings
{
    public static int M()
    {
        List<int> values = [1];
        return values.Count;
    }
}
