// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleValidator.Demo.Violations;

/// <summary>CSV007: volatile fields require Volatile or Interlocked APIs.</summary>
internal sealed class Csv007_VolatileAccess
{
    private volatile int _Counter = 0;

    public int M()
    {
        return _Counter;
    }
}
