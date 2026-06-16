// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleValidator.Demo;

public static class Program
{
    public static async Task Main()
    {
        int value = CompliantExample.Add(1, 2);
        await Console.Out.WriteLineAsync(value.ToString()).ConfigureAwait(false);
    }
}
