// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleValidator.Demo.Violations;

/// <summary>CSV007: non-atomic read-modify-write on a <c>volatile</c> field.</summary>
internal sealed class Csv007_VolatileAccess
{
    private volatile int _Counter;

    public void M()
    {
        _Counter++;
    }
}
