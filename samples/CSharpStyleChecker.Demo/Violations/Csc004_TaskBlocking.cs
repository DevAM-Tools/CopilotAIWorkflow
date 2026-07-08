// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Demo.Violations;

/// <summary>CSC004: do not block on <see cref="Task"/> via <c>.Wait()</c> or <c>.Result</c>.</summary>
internal static class Csc004_TaskBlocking
{
    public static int M()
    {
        return Task.FromResult(1).Result;
    }
}
