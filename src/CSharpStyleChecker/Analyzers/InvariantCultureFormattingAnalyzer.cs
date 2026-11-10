// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CSharpStyleChecker.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleChecker.Analyzers;

/// <summary>
/// Requires invariant culture for interpolated strings and culture-sensitive <c>ToString</c> calls, and the allocation-free
/// interpolation form wherever the compilation offers it.
/// </summary>
/// <remarks>
/// Allowed interpolation: <c>Invariant($"…")</c> from the InvariantStringHelper package
/// (<c>InvariantStringHelper.InvariantText.Invariant</c>),
/// <c>string.Create(CultureInfo.InvariantCulture, $"…")</c>, and
/// <c>StringBuilder.Append</c> / <c>AppendLine(CultureInfo.InvariantCulture, $"…")</c>. All three bind to an interpolated
/// string handler,
/// so no <see cref="System.FormattableString"/> and no <c>object[]</c> are allocated and value-type arguments are not boxed.
/// <c>FormattableString.Invariant</c> / <c>Invariant($"…")</c> of <c>System.FormattableString</c> allocates all three.
/// It is therefore an
/// error when the compilation can bind <c>string.Create(IFormatProvider, ref DefaultInterpolatedStringHandler)</c>
/// (.NET 6 and later),
/// and it stays allowed on targets without that overload (<c>netstandard2.0</c>), where it is the only invariant interpolation.
/// The helper is recognized by symbol identity (metadata names below), so a method that only shares the name
/// <c>Invariant</c> is not
/// accepted. A project that declares these types itself is trusted: its owner controls them.
/// Thread safety: stateless; the per-compilation <c>SymbolCache</c> is immutable.
/// Architecture: docs/architecture.md § CSharpStyleChecker.
/// Sync: .github/skills/tech-csharp.md § Style (interpolation rule); README.md § CSharpStyleChecker (CSC009 row);
/// docs/decisions/invariant-interpolation.md; src/InvariantStringHelper/InvariantText.cs and
/// src/InvariantStringHelper/InvariantInterpolatedStringHandler.cs (the looked-up names).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvariantCultureFormattingAnalyzer : DiagnosticAnalyzer
{
    #region Fields

    // .NET 6 and later, package not referenced: point at the package. string.Create stays accepted but is not recommended.
    private const string InterpolationMessage =
        "Add the InvariantStringHelper package and use Invariant($\"…\") (StringBuilder.Append with CultureInfo.InvariantCulture "
        + "for builders) instead of a bare interpolated string";

    // Target or language version without interpolated string handlers: FormattableString.Invariant is the only invariant
    // interpolation.
    private const string LegacyInterpolationMessage =
        "Use FormattableString.Invariant($\"…\") (for example Invariant($\"…\") with 'using static System.FormattableString;') "
        + "instead of a bare interpolated string; this target has no allocation-free alternative";

    private const string InvariantWrapperMessage =
        "FormattableString.Invariant allocates a FormattableString and boxes its arguments; "
        + "add the InvariantStringHelper package and use Invariant($\"…\") instead";

    // .NET 6 and later, package referenced.
    private const string PackageInterpolationMessage =
        "Use Invariant($\"…\") from the InvariantStringHelper package (StringBuilder.Append with CultureInfo.InvariantCulture "
        + "for builders) instead of a bare interpolated string";

    private const string PackageWrapperMessage =
        "FormattableString.Invariant allocates a FormattableString and boxes its arguments; "
        + "use Invariant($\"…\") from the InvariantStringHelper package instead";

    private const string ToStringMessage =
        "Pass CultureInfo.InvariantCulture to ToString";

    /// <summary>
    /// Metadata name of the InvariantStringHelper helper class. Sync: src/InvariantStringHelper/InvariantText.cs.
    /// </summary>
    internal const string InvariantTextMetadataName = "InvariantStringHelper.InvariantText";

    /// <summary>
    /// Metadata name of the InvariantStringHelper handler type.
    /// Sync: src/InvariantStringHelper/InvariantInterpolatedStringHandler.cs.
    /// </summary>
    internal const string InvariantHandlerMetadataName = "InvariantStringHelper.InvariantInterpolatedStringHandler";

    /// <summary>Name of the helper method. Sync: src/InvariantStringHelper/InvariantText.cs.</summary>
    internal const string InvariantMethodName = "Invariant";

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.InvariantCultureFormatting);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static start =>
        {
            SymbolCache cache = SymbolCache.Create(start.Compilation);
            start.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInterpolatedString(nodeContext, cache),
                SyntaxKind.InterpolatedStringExpression);
            start.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeToString(nodeContext, cache),
                SyntaxKind.InvocationExpression);
        });
    }

    #endregion

    #region Private helpers

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context, SymbolCache cache)
    {
        InterpolatedStringExpressionSyntax interpolated = (InterpolatedStringExpressionSyntax)context.Node;
        InterpolationWrapper wrapper = ClassifyWrapper(context, interpolated, cache);
        if (wrapper == InterpolationWrapper.AllocationFree)
        {
            return;
        }

        // FormattableString.Invariant is the only invariant interpolation on targets without the handler overload;
        // with it, the wrapper is the defect.
        if (wrapper == InterpolationWrapper.FormattableStringInvariant
            && !cache.HasInterpolatedStringCreate)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.InvariantCultureFormatting,
                interpolated.GetLocation(),
                SelectMessage(wrapper, cache)));
    }

    /// <summary>Picks the diagnostic text; names the InvariantStringHelper helper when the compilation references it.</summary>
    /// <param name="wrapper">The wrapper the interpolated string was passed to;
    /// never <see cref="InterpolationWrapper.AllocationFree"/>.</param>
    /// <param name="cache">The compilation's symbols.</param>
    /// <returns>The message that fills the diagnostic's <c>{0}</c> argument.</returns>
    private static string SelectMessage(InterpolationWrapper wrapper, SymbolCache cache)
    {
        bool packageReferenced = cache.InvariantStringHelperInvariant is not null;
        if (wrapper == InterpolationWrapper.FormattableStringInvariant)
        {
            return packageReferenced
                ? PackageWrapperMessage
                : InvariantWrapperMessage;
        }

        if (!cache.HasInterpolatedStringCreate)
        {
            return LegacyInterpolationMessage;
        }

        return packageReferenced
            ? PackageInterpolationMessage
            : InterpolationMessage;
    }

    private static void AnalyzeToString(SyntaxNodeAnalysisContext context, SymbolCache cache)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        if (!TryGetToStringMethod(
                context,
                invocation,
                out IMethodSymbol method,
                out MemberAccessExpressionSyntax memberAccess,
                out Location location))
        {
            return;
        }

        ITypeSymbol receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        INamedTypeSymbol cultureType = TryGetCultureSensitiveType(receiverType) ?? method.ContainingType;
        if (!RequiresFormatProvider(cultureType, cache))
        {
            return;
        }

        if (HasInvariantFormatProvider(context, invocation, method, cache))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.InvariantCultureFormatting,
                location,
                ToStringMessage));
    }

    /// <summary>
    /// Names the invariant API the interpolated string is passed to, if any.
    /// Reason: binder edge paths for reduced/extension symbols are covered by verifier diagnostics.
    /// </summary>
    /// <returns>
    /// <see cref="InterpolationWrapper.AllocationFree"/> for the InvariantStringHelper <c>Invariant</c> method,
    /// <c>string.Create</c>, and
    /// <c>StringBuilder.Append</c> with the invariant culture, <see cref="InterpolationWrapper.FormattableStringInvariant"/> for
    /// <c>FormattableString.Invariant</c>, otherwise <see cref="InterpolationWrapper.None"/>.
    /// </returns>
    [ExcludeFromCodeCoverage]
    private static InterpolationWrapper ClassifyWrapper(
        SyntaxNodeAnalysisContext context,
        InterpolatedStringExpressionSyntax interpolated,
        SymbolCache cache)
    {
        // Parentheses around the interpolated string do not change the handler conversion: look through them to the argument.
        SyntaxNode parent = interpolated.Parent;
        while (parent is ParenthesizedExpressionSyntax)
        {
            parent = parent.Parent;
        }

        if (parent is not ArgumentSyntax argument
            || argument.Parent is not ArgumentListSyntax argumentList
            || argumentList.Parent is not InvocationExpressionSyntax invocation)
        {
            return InterpolationWrapper.None;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return InterpolationWrapper.None;
        }

        IMethodSymbol definition = method.ReducedFrom ?? method.OriginalDefinition;

        // Symbol identity, not name equality: a method that only shares the name Invariant must not pass (look-alike spoof).
        if (cache.InvariantStringHelperInvariant is not null
            && SymbolEqualityComparer.Default.Equals(definition, cache.InvariantStringHelperInvariant))
        {
            return InterpolationWrapper.AllocationFree;
        }

        if (cache.FormattableStringInvariant is not null
            && SymbolEqualityComparer.Default.Equals(definition, cache.FormattableStringInvariant))
        {
            return InterpolationWrapper.FormattableStringInvariant;
        }

        if (IsStringCreateWithInvariantCulture(context, argumentList, argument, method, cache))
        {
            return InterpolationWrapper.AllocationFree;
        }

        if (IsStringBuilderAppendWithInvariantCulture(context, argumentList, argument, method, cache))
        {
            return InterpolationWrapper.AllocationFree;
        }

        return InterpolationWrapper.None;
    }

    private static bool IsStringCreateWithInvariantCulture(
        SyntaxNodeAnalysisContext context,
        ArgumentListSyntax argumentList,
        ArgumentSyntax interpolatedArgument,
        IMethodSymbol method,
        SymbolCache cache)
    {
        if (method.ContainingType.SpecialType != SpecialType.System_String
            || method.Name != "Create"
            || method.Parameters.Length < 2
            || !IsFormatProviderType(method.Parameters[0].Type, cache)
            || argumentList.Arguments.Count < 2
            || !ReferenceEquals(argumentList.Arguments[1], interpolatedArgument))
        {
            return false;
        }

        return IsInvariantCultureExpression(context, argumentList.Arguments[0].Expression, cache);
    }

    private static bool IsStringBuilderAppendWithInvariantCulture(
        SyntaxNodeAnalysisContext context,
        ArgumentListSyntax argumentList,
        ArgumentSyntax interpolatedArgument,
        IMethodSymbol method,
        SymbolCache cache)
    {
        if (cache.StringBuilderType is null
            || !SymbolEqualityComparer.Default.Equals(method.ContainingType, cache.StringBuilderType)
            || (method.Name != "Append" && method.Name != "AppendLine")
            || method.Parameters.Length < 2
            || !IsFormatProviderType(method.Parameters[0].Type, cache)
            || argumentList.Arguments.Count < 2
            || !ReferenceEquals(argumentList.Arguments[1], interpolatedArgument))
        {
            return false;
        }

        return IsInvariantCultureExpression(context, argumentList.Arguments[0].Expression, cache);
    }

    /// <summary>
    /// Binds instance <c>ToString</c> invocations.
    /// Reason: defensive binder edge paths are exercised via analyzer verifier diagnostics.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool TryGetToStringMethod(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        out IMethodSymbol method,
        out MemberAccessExpressionSyntax memberAccess,
        out Location location)
    {
        method = null!;
        memberAccess = null!;
        location = null!;

        if (invocation.Expression is not MemberAccessExpressionSyntax access
            || access.Name.Identifier.Text != "ToString")
        {
            return false;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol bound
            || bound.Name != "ToString"
            || bound.MethodKind == MethodKind.Constructor)
        {
            return false;
        }

        method = bound;
        memberAccess = access;
        location = access.Name.GetLocation();
        return true;
    }

    private static INamedTypeSymbol TryGetCultureSensitiveType(ITypeSymbol type)
    {
        // A null type falls through both checks below and yields null, so it needs no guard of its own.
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            type = named.TypeArguments[0];
        }

        return type as INamedTypeSymbol;
    }

    private static bool RequiresFormatProvider(INamedTypeSymbol type, SymbolCache cache)
    {
        if (type is null
            || type.TypeKind == TypeKind.Enum
            || type.SpecialType is SpecialType.System_Boolean
                or SpecialType.System_Char
                or SpecialType.System_String
                or SpecialType.System_Enum)
        {
            return false;
        }

        if (cache.IFormattableType is not null)
        {
            foreach (INamedTypeSymbol iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, cache.IFormattableType))
                {
                    return true;
                }
            }
        }

        foreach (ISymbol member in type.GetMembers("ToString"))
        {
            if (member is IMethodSymbol candidate && HasFormatProviderParameter(candidate, cache))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFormatProviderParameter(IMethodSymbol method, SymbolCache cache)
    {
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (IsFormatProviderType(parameter.Type, cache))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasInvariantFormatProvider(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SymbolCache cache)
    {
        for (int index = 0; index < method.Parameters.Length; index++)
        {
            if (!IsFormatProviderType(method.Parameters[index].Type, cache))
            {
                continue;
            }

            if (index >= invocation.ArgumentList.Arguments.Count)
            {
                return false;
            }

            return IsInvariantCultureExpression(
                context,
                invocation.ArgumentList.Arguments[index].Expression,
                cache);
        }

        return false;
    }

    private static bool IsFormatProviderType(ITypeSymbol type, SymbolCache cache)
    {
        if (cache.IFormatProviderType is not null
            && SymbolEqualityComparer.Default.Equals(type, cache.IFormatProviderType))
        {
            return true;
        }

        if (cache.CultureInfoType is not null
            && SymbolEqualityComparer.Default.Equals(type, cache.CultureInfoType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// True when the expression is <c>CultureInfo.InvariantCulture</c>.
    /// Reason: alternate bindings (locals aliasing invariant culture) are out of scope.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool IsInvariantCultureExpression(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax expression,
        SymbolCache cache)
    {
        if (cache.InvariantCultureProperty is null)
        {
            return false;
        }

        if (context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol is not IPropertySymbol property)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(property, cache.InvariantCultureProperty);
    }

    /// <summary>How an interpolated string reaches an invariant-culture API.</summary>
    private enum InterpolationWrapper
    {
        /// <summary>Not an argument of a recognized invariant API.</summary>
        None,

        /// <summary>
        /// Argument of <c>string.Create</c> or <c>StringBuilder.Append</c> with the invariant culture;
        /// handler-based, no extra allocation.
        /// </summary>
        AllocationFree,

        /// <summary>
        /// Argument of <c>FormattableString.Invariant</c>;
        /// allocates a <see cref="System.FormattableString"/> and an <c>object[]</c>.
        /// </summary>
        FormattableStringInvariant,
    }

    /// <summary>
    /// Symbols of one compilation, resolved once per compilation start; null members mean the type is not referenced.
    /// </summary>
    private sealed class SymbolCache
    {
        private SymbolCache(
            IMethodSymbol formattableStringInvariant,
            INamedTypeSymbol stringBuilderType,
            INamedTypeSymbol iFormattableType,
            INamedTypeSymbol iFormatProviderType,
            INamedTypeSymbol cultureInfoType,
            IPropertySymbol invariantCultureProperty,
            bool hasInterpolatedStringCreate,
            IMethodSymbol invariantStringHelperInvariant)
        {
            InvariantStringHelperInvariant = invariantStringHelperInvariant;
            FormattableStringInvariant = formattableStringInvariant;
            StringBuilderType = stringBuilderType;
            IFormattableType = iFormattableType;
            IFormatProviderType = iFormatProviderType;
            CultureInfoType = cultureInfoType;
            InvariantCultureProperty = invariantCultureProperty;
            HasInterpolatedStringCreate = hasInterpolatedStringCreate;
        }

        /// <summary>
        /// True when the compilation binds <c>string.Create(IFormatProvider, ref DefaultInterpolatedStringHandler)</c>, which is
        /// the case for .NET 6 and later and not for <c>netstandard2.0</c>. It decides whether <c>Invariant($"…")</c> is still
        /// acceptable.
        /// </summary>
        public bool HasInterpolatedStringCreate { get; }

        /// <summary>
        /// The static <c>InvariantStringHelper.InvariantText.Invariant(ref InvariantInterpolatedStringHandler)</c> method,
        /// or null when the compilation does not reference InvariantStringHelper (or two assemblies define the names,
        /// which makes the lookup ambiguous).
        /// </summary>
        public IMethodSymbol InvariantStringHelperInvariant { get; }

        public IMethodSymbol FormattableStringInvariant { get; }

        public INamedTypeSymbol StringBuilderType { get; }

        public INamedTypeSymbol IFormattableType { get; }

        public INamedTypeSymbol IFormatProviderType { get; }

        public INamedTypeSymbol CultureInfoType { get; }

        public IPropertySymbol InvariantCultureProperty { get; }

        public static SymbolCache Create(Compilation compilation)
        {
            INamedTypeSymbol formattableStringType = compilation.GetTypeByMetadataName("System.FormattableString");
            IMethodSymbol formattableStringInvariant = null;
            if (formattableStringType is not null)
            {
                foreach (ISymbol member in formattableStringType.GetMembers("Invariant"))
                {
                    if (member is IMethodSymbol method
                        && method.IsStatic
                        && method.Parameters.Length == 1)
                    {
                        formattableStringInvariant = method;
                        break;
                    }
                }
            }

            INamedTypeSymbol cultureInfoType = compilation.GetTypeByMetadataName("System.Globalization.CultureInfo");
            IPropertySymbol invariantCultureProperty = null;
            if (cultureInfoType is not null)
            {
                foreach (ISymbol member in cultureInfoType.GetMembers("InvariantCulture"))
                {
                    if (member is IPropertySymbol property && property.IsStatic)
                    {
                        invariantCultureProperty = property;
                        break;
                    }
                }
            }

            INamedTypeSymbol iFormatProviderType = compilation.GetTypeByMetadataName("System.IFormatProvider");

            return new SymbolCache(
                formattableStringInvariant,
                compilation.GetTypeByMetadataName("System.Text.StringBuilder"),
                compilation.GetTypeByMetadataName("System.IFormattable"),
                iFormatProviderType,
                cultureInfoType,
                invariantCultureProperty,
                SupportsInterpolatedStringHandlers(compilation) && HasStringCreateWithHandler(compilation, iFormatProviderType),
                FindInvariantStringHelperMethod(compilation));
        }

        /// <summary>
        /// Finds the InvariantStringHelper helper by metadata name and checks its single by-reference handler parameter.
        /// </summary>
        /// <param name="compilation">The compilation to search.</param>
        /// <returns>The method symbol, or null when the types or the method are missing;
        /// an ambiguous type name also yields null (fail closed).</returns>
        private static IMethodSymbol FindInvariantStringHelperMethod(Compilation compilation)
        {
            INamedTypeSymbol textType = compilation.GetTypeByMetadataName(InvariantTextMetadataName);
            INamedTypeSymbol handlerType = compilation.GetTypeByMetadataName(InvariantHandlerMetadataName);
            if (textType is null || handlerType is null)
            {
                return null;
            }

            foreach (ISymbol member in textType.GetMembers(InvariantMethodName))
            {
                if (member is IMethodSymbol method
                    && method.IsStatic
                    && method.Parameters.Length == 1
                    && method.Parameters[0].RefKind == RefKind.Ref
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, handlerType))
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// True when the compiler can build interpolated string handlers: C# 10 or later. A project that targets .NET 6 but pins
        /// an older <c>LangVersion</c> has the framework overload and cannot use it, so it must not be pointed at it.
        /// </summary>
        /// <param name="compilation">The compilation whose language version is read.</param>
        /// <returns>True for a C# compilation at language version 10 or later.</returns>
        private static bool SupportsInterpolatedStringHandlers(Compilation compilation)
        {
            return compilation is CSharpCompilation csharp
                && csharp.LanguageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp10;
        }

        /// <summary>
        /// Looks for the static <c>string.Create</c> overload whose second parameter is the interpolated string handler.
        /// </summary>
        /// <param name="compilation">The compilation whose referenced <c>System.String</c> is searched.</param>
        /// <param name="iFormatProviderType">The <c>IFormatProvider</c> symbol; null when it is not referenced.</param>
        /// <returns>True when the overload exists in the referenced framework.</returns>
        private static bool HasStringCreateWithHandler(Compilation compilation, INamedTypeSymbol iFormatProviderType)
        {
            INamedTypeSymbol handlerType = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler");
            if (handlerType is null || iFormatProviderType is null)
            {
                return false;
            }

            foreach (ISymbol member in compilation.GetSpecialType(SpecialType.System_String).GetMembers("Create"))
            {
                if (member is IMethodSymbol method
                    && method.IsStatic
                    && method.Parameters.Length == 2
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, iFormatProviderType)
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, handlerType))
                {
                    return true;
                }
            }

            return false;
        }
    }

    #endregion
}
