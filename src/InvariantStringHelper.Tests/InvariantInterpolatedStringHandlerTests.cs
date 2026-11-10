// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace InvariantStringHelper.Tests;

/// <summary>Tests for <see cref="InvariantInterpolatedStringHandler"/>: provider hand-over and hand-built handlers.</summary>
public sealed class InvariantInterpolatedStringHandlerTests
{
    #region Provider hand-over

    [Test]
    public async Task FormattablesReceiveInvariantCultureAndFormat()
    {
        FormattableProbe formattable = new();
        SpanFormattableProbe spanFormattable = new();

        string text = Invariant($"[{formattable}] [{formattable:custom}] [{spanFormattable}] [{spanFormattable:custom}]");

        await Assert.That(text).IsEqualTo("[f] [f] [s] [s]");
        await Assert.That(formattable.Providers.All(static provider => ReferenceEquals(provider, CultureInfo.InvariantCulture)))
            .IsTrue();
        await Assert.That(
                spanFormattable.Providers.All(static provider => ReferenceEquals(provider, CultureInfo.InvariantCulture)))
            .IsTrue();
        await Assert.That(formattable.Formats.Count).IsEqualTo(2);
        await Assert.That(formattable.Formats[0]).IsNull();
        await Assert.That(formattable.Formats[1]).IsEqualTo("custom");
        await Assert.That(spanFormattable.Formats.Count).IsEqualTo(2);
        await Assert.That(spanFormattable.Formats[0]).IsNull();
        await Assert.That(spanFormattable.Formats[1]).IsEqualTo("custom");
    }

    /// <summary>
    /// Records the provider and format it receives through <see cref="IFormattable.ToString(string, IFormatProvider)"/>.
    /// </summary>
    private sealed class FormattableProbe : IFormattable
    {
        public List<IFormatProvider?> Providers { get; } = [];

        public List<string?> Formats { get; } = [];

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            Providers.Add(formatProvider);
            Formats.Add(format);
            return "f";
        }
    }

    /// <summary>
    /// Records the provider and format it receives through <see cref="ISpanFormattable.TryFormat"/>,
    /// the framework's preferred path.
    /// </summary>
    private sealed class SpanFormattableProbe : ISpanFormattable
    {
        public List<IFormatProvider?> Providers { get; } = [];

        public List<string?> Formats { get; } = [];

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            Providers.Add(formatProvider);
            Formats.Add(format);
            return "s";
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            Providers.Add(provider);
            if (format.IsEmpty)
            {
                Formats.Add(null);
            }
            else
            {
                Formats.Add(format.ToString());
            }

            destination[0] = 's';
            charsWritten = 1;
            return true;
        }
    }

    #endregion

    #region Hand-built handlers

    [Test]
    public async Task DefaultHandlerYieldsEmptyString()
    {
        string result = _ResultOfDefaultHandler();

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ManuallyBuiltHandlerYieldsItsParts()
    {
        string result = _ResultOfManualHandler();

        await Assert.That(result).IsEqualTo("x1");
    }

    private static string _ResultOfDefaultHandler()
    {
        InvariantInterpolatedStringHandler handler = default;
        return Invariant(ref handler);
    }

    private static string _ResultOfManualHandler()
    {
        InvariantInterpolatedStringHandler handler = new(0, 0);
        handler.AppendLiteral("x");
        handler.AppendFormatted(1);
        return Invariant(ref handler);
    }

    #endregion
}
