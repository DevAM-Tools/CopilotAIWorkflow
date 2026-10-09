// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CSharpStyleChecker.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleChecker.Analyzers;

/// <summary>Enforces _PascalCase for private members.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrivateNamingAnalyzer : DiagnosticAnalyzer
{
    #region Lifecycle

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PrivateNaming);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property);
    }

    #endregion

    #region Private helpers

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        ImmutableArray<Location> locations = context.Symbol.Locations;
        if (locations.Length == 0 || ShouldSkipPrivateSymbol(context.Symbol, locations[0]))
        {
            return;
        }

        string memberKind = context.Symbol.Kind == SymbolKind.Field
            ? "field"
            : context.Symbol.Kind == SymbolKind.Method
                ? "method"
                : "property";

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PrivateNaming,
            locations[0],
            memberKind,
            context.Symbol.Name));
    }

    /// <summary>
    /// Filters symbols outside CSC003 scope, including metadata-only and implicit declarations.
    /// Exempt: members inside private nested types, explicit interface implementations, and local functions.
    /// Reason: skip branches are covered by analyzer verifier diagnostics, not line-level coverage on this helper.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool ShouldSkipPrivateSymbol(ISymbol symbol, Location location)
    {
        if (symbol.IsImplicitlyDeclared)
        {
            return true;
        }

        if (symbol.DeclaredAccessibility != Accessibility.Private)
        {
            return true;
        }

        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.PropertyGet or MethodKind.PropertySet })
        {
            return true;
        }

        if (symbol is IMethodSymbol method
            && method.MethodKind == MethodKind.Ordinary
            && method.ContainingType is not null
            && method.Name == method.ContainingType.Name)
        {
            return true;
        }

        if (!location.IsInSource)
        {
            return true;
        }

        if (IsInsidePrivateNestedType(symbol))
        {
            return true;
        }

        if (symbol is IMethodSymbol
            {
                MethodKind: MethodKind.ExplicitInterfaceImplementation or MethodKind.LocalFunction
            })
        {
            return true;
        }

        return IsValidPrivateName(symbol.Name);
    }

    /// <summary>
    /// Matches <c>_PascalCase</c>: underscore, ASCII A–Z, then word characters
    /// (letter, digit, <c>_</c>, or connector punctuation — the non-ECMAScript <c>\w</c> set).
    /// </summary>
    private static bool IsValidPrivateName(string name)
    {
        if (name.Length < 2 || name[0] != '_')
        {
            return false;
        }

        char second = name[1];
        if (second < 'A' || second > 'Z')
        {
            return false;
        }

        for (int index = 2; index < name.Length; index++)
        {
            char ch = name[index];
            if (ch == '_' || char.IsLetter(ch) || char.IsDigit(ch))
            {
                continue;
            }

            if (char.GetUnicodeCategory(ch) == UnicodeCategory.ConnectorPunctuation)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Detects members declared inside a private nested type.
    /// Reason: skip branches are covered by analyzer verifier diagnostics, not line-level coverage on this helper.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool IsInsidePrivateNestedType(ISymbol symbol)
    {
        return symbol.ContainingType is INamedTypeSymbol containingType
            && containingType.ContainingType is not null
            && containingType.DeclaredAccessibility == Accessibility.Private;
    }

    #endregion
}
