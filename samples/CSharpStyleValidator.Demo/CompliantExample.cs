// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleValidator.Demo;

/// <summary>Example code that satisfies CSV001–CSV008.</summary>
internal static class CompliantExample
{
    private static int _Seed = 1;
    private static List<int> _Values = [];

    public static int Add(int left, int right)
    {
        StringBuilder builder = new();
        builder.Append(_Seed);
        return left + right + _Seed + _Values.Count;
    }
}
