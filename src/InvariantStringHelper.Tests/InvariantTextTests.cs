// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace InvariantStringHelper.Tests;

/// <summary>Tests for <see cref="InvariantText"/>: culture independence, hole formatting, allocation, and concurrency.</summary>
/// <remarks>
/// Expected texts come from reference calls (<c>ToString(format, CultureInfo.InvariantCulture)</c>, <c>PadLeft</c>,
/// <c>PadRight</c>), never from literals typed from memory. A <see cref="ReadOnlySpan{T}"/> local must not live across
/// an <c>await</c>, so the strings are built in synchronous helpers and only the assertion is awaited.
/// </remarks>
public sealed class InvariantTextTests
{
    #region Culture independence

    [Test]
    public async Task InvariantIgnoresCurrentCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            double rate = 1234.5;
            int count = 1234567;

            string actual = Invariant($"rate={rate} count={count:N0}");
            string expected = "rate=" + rate.ToString(CultureInfo.InvariantCulture)
                + " count=" + count.ToString("N0", CultureInfo.InvariantCulture);

            // The comparison under the current culture must differ, or the test cannot fail when the machine culture
            // is invariant.
            string underCurrentCulture = string.Format(CultureInfo.CurrentCulture, "rate={0}", rate);

            await Assert.That(actual).IsEqualTo(expected);
            await Assert.That(underCurrentCulture).IsNotEqualTo(Invariant($"rate={rate}"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public async Task InvariantGivesOneResultUnderConcurrentCultures()
    {
        string[] cultureNames = ["de-DE", "fr-FR", "ar-SA", "th-TH", "tr-TR", "ja-JP", "hi-IN", ""];
        string expected = _ConcurrentExpected();

        Task<bool>[] tasks = new Task<bool>[cultureNames.Length];
        for (int index = 0; index < cultureNames.Length; index++)
        {
            string cultureName = cultureNames[index];
            tasks[index] = Task.Run(() => _AllCallsMatch(cultureName, expected));
        }

        bool[] results = await Task.WhenAll(tasks);

        await Assert.That(results.All(static matched => matched)).IsTrue();
    }

    private static string _ConcurrentExpected()
    {
        DateTime when = _FixedUtc();
        return 1234567.891.ToString("N2", CultureInfo.InvariantCulture)
            + " "
            + 0.256.ToString("P1", CultureInfo.InvariantCulture)
            + " "
            + (-5).ToString(CultureInfo.InvariantCulture)
            + " "
            + when.ToString("O", CultureInfo.InvariantCulture);
    }

    private static bool _AllCallsMatch(string cultureName, string expected)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        DateTime when = _FixedUtc();
        bool allMatch = true;
        for (int call = 0; call < 1000; call++)
        {
            string actual = Invariant($"{1234567.891:N2} {0.256:P1} {-5} {when:O}");
            allMatch &= actual == expected;
        }

        return allMatch;
    }

    private static DateTime _FixedUtc()
    {
        return new DateTime(2026, 11, 5, 13, 14, 15, DateTimeKind.Utc);
    }

    #endregion

    #region Hole formats and kinds

    [Test]
    public async Task InvariantAppliesFormatStringsAndAlignment()
    {
        double rate = 0.2563;
        decimal sum = 1234567.891m;
        int count = 42;
        int hex = 255;
        DateTime when = _FixedUtc();

        string formats = Invariant($"[{rate:P1}] [{sum:N2}] [{count:F0}] [{count:D4}] [{hex:X}] [{when:O}] [{rate:e2}]");
        string expectedFormats = "[" + rate.ToString("P1", CultureInfo.InvariantCulture)
            + "] [" + sum.ToString("N2", CultureInfo.InvariantCulture)
            + "] [" + count.ToString("F0", CultureInfo.InvariantCulture)
            + "] [" + count.ToString("D4", CultureInfo.InvariantCulture)
            + "] [" + hex.ToString("X", CultureInfo.InvariantCulture)
            + "] [" + when.ToString("O", CultureInfo.InvariantCulture)
            + "] [" + rate.ToString("e2", CultureInfo.InvariantCulture) + "]";

        string plain = count.ToString(CultureInfo.InvariantCulture);
        string aligned = Invariant($"[{count,8}] [{count,-8}] [{count,8:D4}] [{count,1}] [{count,0}]");
        string expectedAligned = "[" + plain.PadLeft(8)
            + "] [" + plain.PadRight(8)
            + "] [" + count.ToString("D4", CultureInfo.InvariantCulture).PadLeft(8)
            + "] [" + plain
            + "] [" + plain + "]";

        await Assert.That(formats).IsEqualTo(expectedFormats);
        await Assert.That(aligned).IsEqualTo(expectedAligned);
    }

    [Test]
    public async Task InvariantFormatsEveryHoleType()
    {
        string scalars = _FormatScalars();
        string spans = _FormatSpansStringsAndObjects();

        await Assert.That(scalars).IsEqualTo(_ExpectedScalars());
        await Assert.That(spans).IsEqualTo(_ExpectedSpansStringsAndObjects());
    }

    private static string _FormatScalars()
    {
        int number = -5;
        long big = 9_000_000_000L;
        double real = 1.5;
        decimal money = 2.25m;
        DateTime when = _FixedUtc();
        Guid id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
        bool flag = true;
        char letter = 'x';
        DayOfWeek day = DayOfWeek.Friday;
        string text = "text";
        string? missingText = null;
        object? missingObject = null;
        int? missingNumber = null;
        int? presentNumber = 7;

        // Two calls keep each line within the line-length limit; together they cover every hole kind.
        string numeric = Invariant($"{number}|{big}|{real}|{money}|{when:O}|{id}|{flag}");
        string textual = Invariant($"{letter}|{day}|{text}|{missingText}|{missingObject}|{missingNumber}|{presentNumber}");
        return numeric + "|" + textual;
    }

    private static string _ExpectedScalars()
    {
        string[] parts =
        [
            (-5).ToString(CultureInfo.InvariantCulture),
            9_000_000_000L.ToString(CultureInfo.InvariantCulture),
            1.5.ToString(CultureInfo.InvariantCulture),
            2.25m.ToString(CultureInfo.InvariantCulture),
            _FixedUtc().ToString("O", CultureInfo.InvariantCulture),
            Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e").ToString("D", CultureInfo.InvariantCulture),
            true.ToString(),
            'x'.ToString(),
            DayOfWeek.Friday.ToString(),
            "text",
            string.Empty,
            string.Empty,
            string.Empty,
            7.ToString(CultureInfo.InvariantCulture),
        ];

        return string.Join("|", parts);
    }

    private static string _FormatSpansStringsAndObjects()
    {
        ReadOnlySpan<char> span = "abc".AsSpan();
        string text = "xy";
        object boxed = 12;
        string? missing = null;

        return Invariant($"[{span}] [{span,6}] [{span,-6}] [{text,5}] [{text,-5:q}] [{boxed,6:D4}] [{missing,3}]");
    }

    private static string _ExpectedSpansStringsAndObjects()
    {
        return "[abc] [" + "abc".PadLeft(6) + "] [" + "abc".PadRight(6) + "] [" + "xy".PadLeft(5) + "] [" + "xy".PadRight(5)
            + "] [" + 12.ToString("D4", CultureInfo.InvariantCulture).PadLeft(6) + "] [   ]";
    }

    #endregion

    #region Result size and nesting

    [Test]
    public async Task InvariantHandlesEmptyLiteralOnlyAndLargeResults()
    {
        int first = 1;
        int second = 2;
        string large = new string('x', 100_000);

        await Assert.That(Invariant($"")).IsEqualTo(string.Empty);
        await Assert.That(Invariant($"literal")).IsEqualTo("literal");
        await Assert.That(Invariant($"{first}")).IsEqualTo("1");
        await Assert.That(Invariant($"{first}{second}")).IsEqualTo("12");
        await Assert.That(Invariant($"<{large}>")).IsEqualTo("<" + large + ">");
    }

    [Test]
    public async Task InvariantSupportsNestedCalls()
    {
        int value = 7;

        string result = Invariant($"outer={Invariant($"inner={value}")}");

        await Assert.That(result).IsEqualTo("outer=inner=7");
    }

    #endregion

    #region Allocation

    [Test]
    public async Task InvariantAllocatesNoMoreThanStringCreate()
    {
        int count = 42;
        double rate = 0.256;

        // The loops are inline (no lambda, no delegate) so no closure allocation enters the measurement.
        for (int warm = 0; warm < 200; warm++)
        {
            _ = Invariant($"count={count}, rate={rate:F1}");
            _ = string.Create(CultureInfo.InvariantCulture, $"count={count}, rate={rate:F1}");
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 1000; index++)
        {
            _ = Invariant($"count={count}, rate={rate:F1}");
        }

        long package = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 1000; index++)
        {
            _ = string.Create(CultureInfo.InvariantCulture, $"count={count}, rate={rate:F1}");
        }

        long baseline = GC.GetAllocatedBytesForCurrentThread() - before;

        await Assert.That(package).IsLessThanOrEqualTo(baseline);
    }

    #endregion

    #region Failure

    [Test]
    public async Task InvariantPropagatesHoleExceptionAndStaysUsable()
    {
        string? message = null;
        try
        {
            _ = _FormatWithThrowingHole();
        }
        catch (InvalidOperationException exception)
        {
            message = exception.Message;
        }

        int value = 3;

        await Assert.That(message).IsEqualTo("boom");
        await Assert.That(Invariant($"after={value}")).IsEqualTo("after=3");
    }

    private static string _FormatWithThrowingHole()
    {
        return Invariant($"a{_Boom()}b");
    }

    private static int _Boom()
    {
        throw new InvalidOperationException("boom");
    }

    #endregion
}
