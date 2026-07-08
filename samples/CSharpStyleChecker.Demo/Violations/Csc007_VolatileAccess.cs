// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC007: non-atomic read-modify-write on a <c>volatile</c> field.</summary>
internal sealed class Csc007_VolatileAccess
{
    private volatile int _Counter;

    public void M()
    {
        _Counter++;
    }
}
