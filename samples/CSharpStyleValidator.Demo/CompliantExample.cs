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

    /// <summary>Manual <see cref="IEnumerable{T}"/> with a private nested enumerator (CSV003 exempt).</summary>
    public static IEnumerable<int> Sequence(int start, int count)
    {
        return new IntSequence(start, count);
    }

    private sealed class IntSequence : IEnumerable<int>
    {
        private readonly int start;
        private readonly int count;

        public IntSequence(int start, int count)
        {
            this.start = start;
            this.count = count;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return new Enumerator(start, count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class Enumerator : IEnumerator<int>
        {
            private readonly int start;
            private readonly int count;
            private int index = -1;

            public Enumerator(int start, int count)
            {
                this.start = start;
                this.count = count;
            }

            public int Current
            {
                get
                {
                    return start + index;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                index++;
                return index < count;
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}
