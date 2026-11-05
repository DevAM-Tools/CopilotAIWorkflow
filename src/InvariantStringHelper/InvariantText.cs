// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace InvariantStringHelper;

/// <summary>Formats interpolated strings with the invariant culture and allocates only the result string.</summary>
/// <remarks>
/// Import once per project with <c>global using static InvariantStringHelper.InvariantText;</c>.
/// Thread-safe: static, stateless.
/// Architecture: docs/architecture.md § InvariantStringHelper.
/// Sync: src/CSharpStyleChecker/Analyzers/InvariantCultureFormattingAnalyzer.cs (InvariantTextMetadataName, InvariantMethodName);
/// .github/skills/tech-csharp.md § Style; .github/skills/tech-solution.md § InvariantStringHelper.
/// </remarks>
/// <seealso cref="InvariantInterpolatedStringHandler"/>
public static class InvariantText
{
    #region Formatting

    /// <summary>Formats an interpolated string under <see cref="CultureInfo.InvariantCulture"/>.</summary>
    /// <param name="handler">The handler the compiler builds from the interpolated string; consumed by this call.</param>
    /// <returns>The formatted text, independent of the current culture.</returns>
    /// <example>
    /// <code>
    /// string message = Invariant($"Rejection rate: {rate:P1}, count={count}");
    /// </code>
    /// </example>
    /// <remarks>
    /// Cost: one allocation, the result string; the working buffer comes from <see cref="System.Buffers.ArrayPool{T}"/> and is returned here.
    /// A hand-built or default handler yields its parts or an empty string.
    /// </remarks>
    public static string Invariant(ref InvariantInterpolatedStringHandler handler) => handler.ToStringAndClear();

    #endregion
}
