// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace InvariantStringHelper;

/// <summary>
/// Collects the parts of an interpolated string and formats every hole with <see cref="CultureInfo.InvariantCulture"/>.
/// The compiler creates it for the argument of <see cref="InvariantText.Invariant"/>; calling code never names it.
/// </summary>
/// <remarks>
/// Wraps <see cref="DefaultInterpolatedStringHandler"/>, so holes keep the framework's formatting, alignment, and
/// <see cref="ISpanFormattable"/> path, and only the result string is allocated. The wrapper exists because the framework
/// handler takes its provider from a constructor argument that a plain <see cref="string"/> parameter cannot supply.
/// Not thread-safe: one instance serves one interpolated string on one thread. A handler that is never passed to
/// <see cref="InvariantText.Invariant"/> keeps its rented buffer until the garbage collector reclaims it.
/// Architecture: docs/architecture.md § InvariantStringHelper.
/// Sync: src/CSharpStyleChecker/Analyzers/InvariantCultureFormattingAnalyzer.cs (InvariantHandlerMetadataName).
/// </remarks>
/// <seealso cref="InvariantText"/>
[InterpolatedStringHandler]
public ref struct InvariantInterpolatedStringHandler
{
    #region Collecting the interpolated parts

    private DefaultInterpolatedStringHandler _Inner;

    /// <summary>Starts a handler for one interpolated string.</summary>
    /// <param name="literalLength">Total length of the literal parts;
    /// the compiler supplies it as a first-buffer size hint.</param>
    /// <param name="formattedCount">Number of holes; the compiler supplies it as a first-buffer size hint.</param>
    /// <remarks>
    /// Omitted validation: both values are compiler-supplied hints; a wrong value only changes the first buffer size.
    /// </remarks>
    public InvariantInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _Inner = new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture);
    }

    /// <summary>Appends a literal part of the interpolated string.</summary>
    /// <param name="value">The literal text; the compiler never passes <see langword="null"/>.</param>
    public void AppendLiteral(string value) => _Inner.AppendLiteral(value);

    /// <summary>Appends a hole formatted with the invariant culture.</summary>
    /// <typeparam name="T">The hole type; no boxing happens for value types.</typeparam>
    /// <param name="value">The value to format.</param>
    public void AppendFormatted<T>(T value) => _Inner.AppendFormatted(value);

    /// <summary>Appends a hole with a format string, formatted with the invariant culture.</summary>
    /// <typeparam name="T">The hole type.</typeparam>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format string after the colon; <see langword="null"/> for the default format.</param>
    public void AppendFormatted<T>(T value, string? format) => _Inner.AppendFormatted(value, format);

    /// <summary>Appends a hole with an alignment, formatted with the invariant culture.</summary>
    /// <typeparam name="T">The hole type.</typeparam>
    /// <param name="value">The value to format.</param>
    /// <param name="alignment">Minimum width; negative pads on the right, positive on the left.</param>
    public void AppendFormatted<T>(T value, int alignment) => _Inner.AppendFormatted(value, alignment);

    /// <summary>Appends a hole with an alignment and a format string, formatted with the invariant culture.</summary>
    /// <typeparam name="T">The hole type.</typeparam>
    /// <param name="value">The value to format.</param>
    /// <param name="alignment">Minimum width; negative pads on the right, positive on the left.</param>
    /// <param name="format">The format string after the colon; <see langword="null"/> for the default format.</param>
    public void AppendFormatted<T>(T value, int alignment, string? format) => _Inner.AppendFormatted(value, alignment, format);

    /// <summary>Appends a character span without formatting.</summary>
    /// <param name="value">The characters to copy.</param>
    public void AppendFormatted(ReadOnlySpan<char> value) => _Inner.AppendFormatted(value);

    /// <summary>
    /// Appends a character span padded to an alignment. The format string is ignored, as in the framework handler.
    /// </summary>
    /// <param name="value">The characters to copy.</param>
    /// <param name="alignment">Minimum width; negative pads on the right, positive on the left.</param>
    /// <param name="format">Ignored for spans.</param>
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        => _Inner.AppendFormatted(value, alignment, format);

    /// <summary>Appends a string hole; <see langword="null"/> appends nothing.</summary>
    /// <param name="value">The text to append.</param>
    public void AppendFormatted(string? value) => _Inner.AppendFormatted(value);

    /// <summary>Appends a string hole padded to an alignment; <see langword="null"/> appends padding only.</summary>
    /// <param name="value">The text to append.</param>
    /// <param name="alignment">Minimum width; negative pads on the right, positive on the left.</param>
    /// <param name="format">Ignored for strings, as in the framework handler.</param>
    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
        => _Inner.AppendFormatted(value, alignment, format);

    /// <summary>Appends an object hole formatted with the invariant culture; <see langword="null"/> appends nothing.</summary>
    /// <param name="value">The object to format.</param>
    /// <param name="alignment">Minimum width; negative pads on the right, positive on the left.</param>
    /// <param name="format">The format string after the colon; <see langword="null"/> for the default format.</param>
    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
        => _Inner.AppendFormatted(value, alignment, format);

    #endregion

    #region Producing the result

    /// <summary>Builds the result string and returns the rented buffer to the pool.</summary>
    /// <returns>The formatted text; empty for a default or empty handler. Allocates the result string only.</returns>
    /// <remarks>Call once; the handler is empty afterwards.</remarks>
    public string ToStringAndClear() => _Inner.ToStringAndClear();

    #endregion
}
