// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC008: redundant explicit type in object or collection creation.</summary>
internal static class Csc008_TargetTypedCreation
{
    public static System.Collections.Generic.List<int> M()
    {
        System.Collections.Generic.List<int> items = new System.Collections.Generic.List<int>();
        return items;
    }
}
