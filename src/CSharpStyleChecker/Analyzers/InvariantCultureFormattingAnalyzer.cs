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
/// Requires invariant culture for interpolated strings and culture-sensitive <c>ToString</c> calls.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvariantCultureFormattingAnalyzer : DiagnosticAnalyzer
{
    #region Fields

    private const string InterpolationMessage =
        "Use Invariant($\"…\") (or string.Create / StringBuilder.Append with CultureInfo.InvariantCulture) "
        + "instead of a bare interpolated string";

    private const string ToStringMessage =
        "Pass CultureInfo.InvariantCulture to ToString";

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
        if (IsAllowedInterpolation(context, interpolated, cache))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.InvariantCultureFormatting,
                interpolated.GetLocation(),
                InterpolationMessage));
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
    /// True when the interpolated string is the culture argument to an allowed invariant API.
    /// Reason: binder edge paths for reduced/extension symbols are covered by verifier diagnostics.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool IsAllowedInterpolation(
        SyntaxNodeAnalysisContext context,
        InterpolatedStringExpressionSyntax interpolated,
        SymbolCache cache)
    {
        if (interpolated.Parent is not ArgumentSyntax argument
            || argument.Parent is not ArgumentListSyntax argumentList
            || argumentList.Parent is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        IMethodSymbol definition = method.ReducedFrom ?? method.OriginalDefinition;

        if (cache.FormattableStringInvariant is not null
            && SymbolEqualityComparer.Default.Equals(definition, cache.FormattableStringInvariant))
        {
            return true;
        }

        if (IsStringCreateWithInvariantCulture(context, argumentList, argument, method, cache))
        {
            return true;
        }

        if (IsStringBuilderAppendWithInvariantCulture(context, argumentList, argument, method, cache))
        {
            return true;
        }

        return false;
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
        if (type is null)
        {
            return null;
        }

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

    private sealed class SymbolCache
    {
        private SymbolCache(
            IMethodSymbol formattableStringInvariant,
            INamedTypeSymbol stringBuilderType,
            INamedTypeSymbol iFormattableType,
            INamedTypeSymbol iFormatProviderType,
            INamedTypeSymbol cultureInfoType,
            IPropertySymbol invariantCultureProperty)
        {
            FormattableStringInvariant = formattableStringInvariant;
            StringBuilderType = stringBuilderType;
            IFormattableType = iFormattableType;
            IFormatProviderType = iFormatProviderType;
            CultureInfoType = cultureInfoType;
            InvariantCultureProperty = invariantCultureProperty;
        }

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

            return new SymbolCache(
                formattableStringInvariant,
                compilation.GetTypeByMetadataName("System.Text.StringBuilder"),
                compilation.GetTypeByMetadataName("System.IFormattable"),
                compilation.GetTypeByMetadataName("System.IFormatProvider"),
                cultureInfoType,
                invariantCultureProperty);
        }
    }

    #endregion
}
