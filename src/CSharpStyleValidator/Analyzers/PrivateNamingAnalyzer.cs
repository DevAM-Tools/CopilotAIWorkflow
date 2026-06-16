// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CSharpStyleValidator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleValidator.Analyzers;

/// <summary>Enforces _PascalCase for private members.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrivateNamingAnalyzer : DiagnosticAnalyzer
{
    private static readonly Regex ValidPrivateName = new(@"^_[A-Z][\w]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (ShouldSkipPrivateSymbol(context.Symbol, context.Symbol.Locations[0]))
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
            context.Symbol.Locations[0],
            memberKind,
            context.Symbol.Name));
    }

    /// <remarks>Filters symbols outside CSV003 scope, including metadata-only and implicit declarations.</remarks>
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

        return ValidPrivateName.IsMatch(symbol.Name);
    }
}
