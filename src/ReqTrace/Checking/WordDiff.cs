// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Shows which words of a requirement changed, and flags changes that may weaken it.</summary>
internal static class WordDiff
{
    /// <summary>Words whose removal may weaken an obligation.</summary>
    private static readonly FrozenSet<string> _Strong =
        FrozenSet.ToFrozenSet(["always", "never", "must", "shall", "all", "every", "only"], StringComparer.Ordinal);

    /// <summary>Words whose addition may weaken an obligation.</summary>
    private static readonly FrozenSet<string> _Weak =
        FrozenSet.ToFrozenSet(["usually", "mostly", "may", "should", "some", "typically", "often", "sometimes"], StringComparer.Ordinal);

    /// <summary>Compares two texts word by word.</summary>
    /// <param name="before">The accepted text.</param>
    /// <param name="after">The current text.</param>
    /// <param name="removed">Receives words only in the accepted text.</param>
    /// <param name="added">Receives words only in the current text.</param>
    /// <remarks>Quadratic in words. A requirement is a sentence or two, and only changed entries get here.</remarks>
    public static void Compare(string before, string after, List<string> removed, List<string> added)
    {
        string[] left = before.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string[] right = after.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int[,] lengths = new int[left.Length + 1, right.Length + 1];
        for (int row = left.Length - 1; row >= 0; row--)
        {
            for (int column = right.Length - 1; column >= 0; column--)
            {
                lengths[row, column] = string.Equals(left[row], right[column], StringComparison.Ordinal)
                    ? lengths[row + 1, column + 1] + 1
                    : Math.Max(lengths[row + 1, column], lengths[row, column + 1]);
            }
        }

        int leftIndex = 0;
        int rightIndex = 0;
        while (leftIndex < left.Length || rightIndex < right.Length)
        {
            if (leftIndex < left.Length
                && rightIndex < right.Length
                && string.Equals(left[leftIndex], right[rightIndex], StringComparison.Ordinal))
            {
                leftIndex++;
                rightIndex++;
            }
            else if (rightIndex < right.Length
                && (leftIndex == left.Length || lengths[leftIndex, rightIndex + 1] >= lengths[leftIndex + 1, rightIndex]))
            {
                added.Add(right[rightIndex]);
                rightIndex++;
            }
            else
            {
                removed.Add(left[leftIndex]);
                leftIndex++;
            }
        }
    }

    /// <summary>Returns whether a change may weaken an obligation. Only a hint: the tool cannot read meaning.</summary>
    /// <param name="removed">Removed words.</param>
    /// <param name="added">Added words.</param>
    /// <returns>Whether a strong word went, a weak word came, or a number changed.</returns>
    public static bool IsPossiblyWeakened(List<string> removed, List<string> added)
    {
        foreach (string word in removed)
        {
            if (_Strong.Contains(_Clean(word)) || _HasDigit(word))
            {
                return true;
            }
        }

        foreach (string word in added)
        {
            if (_Weak.Contains(_Clean(word)) || _HasDigit(word))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Lowercases a word and drops surrounding punctuation.</summary>
    /// <param name="word">The word.</param>
    /// <returns>The cleaned word.</returns>
    private static string _Clean(string word)
    {
        return word.Trim('.', ',', ';', ':', '"', '“', '”').ToLowerInvariant();
    }

    /// <summary>Returns whether a word holds a digit.</summary>
    /// <param name="word">The word.</param>
    /// <returns>Whether it does.</returns>
    private static bool _HasDigit(string word)
    {
        return word.AsSpan().IndexOfAnyInRange('0', '9') >= 0;
    }
}
