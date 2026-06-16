// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExitPoints;

/// <summary>Collects callable exit points from a Roslyn compilation.</summary>
public static class ExitPointCollector
{
    private static readonly ConditionalWeakTable<Compilation, CachedCompilationResults> ResultsByCompilation = new();

    private static readonly SymbolDisplayFormat MethodIdFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>Collects all exit points in <paramref name="compilation"/>.</summary>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <param name="options">Optional collection options.</param>
    /// <returns>Collected exit points.</returns>
    public static IReadOnlyList<ExitPointEntry> Collect(Compilation compilation, ExitPointCollectorOptions? options = null)
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        ExitPointCollectorOptions effectiveOptions = options ?? new ExitPointCollectorOptions();
        CachedCompilationResults cache = ResultsByCompilation.GetValue(compilation, static _ => new CachedCompilationResults());
        return cache.GetOrAdd(effectiveOptions.IncludeLocalFunctions, () => CollectCore(compilation, effectiveOptions));
    }

    private static List<ExitPointEntry> CollectCore(Compilation compilation, ExitPointCollectorOptions effectiveOptions)
    {
        List<ExitPointEntry> results = new List<ExitPointEntry>();

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            if (ShouldSkipTree(tree))
            {
                continue;
            }

            SemanticModel? model = compilation.GetSemanticModel(tree);
            if (model is null)
            {
                continue;
            }

            CollectFromTree(tree, model, effectiveOptions, results);
        }

        return results;
    }

    private sealed class CachedCompilationResults
    {
        private readonly ConcurrentDictionary<bool, IReadOnlyList<ExitPointEntry>> _ResultsByIncludeLocalFunctions = new();

        public IReadOnlyList<ExitPointEntry> GetOrAdd(bool includeLocalFunctions, Func<List<ExitPointEntry>> factory)
        {
            return _ResultsByIncludeLocalFunctions.GetOrAdd(includeLocalFunctions, _ => factory());
        }
    }

    private static void CollectFromTree(
        SyntaxTree tree,
        SemanticModel model,
        ExitPointCollectorOptions options,
        List<ExitPointEntry> results)
    {
        SyntaxNode root = tree.GetRoot();

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            CollectFromCallable(method, method.Body, method.ExpressionBody, method.Identifier, model, results, options);
        }

        foreach (ConstructorDeclarationSyntax constructor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            CollectFromCallable(constructor, constructor.Body, constructor.ExpressionBody, constructor.Identifier, model, results, options);
        }

        foreach (DestructorDeclarationSyntax destructor in root.DescendantNodes().OfType<DestructorDeclarationSyntax>())
        {
            CollectFromCallable(destructor, destructor.Body, null, destructor.TildeToken, model, results, options);
        }

        foreach (OperatorDeclarationSyntax operatorDeclaration in root.DescendantNodes().OfType<OperatorDeclarationSyntax>())
        {
            CollectFromCallable(
                operatorDeclaration,
                operatorDeclaration.Body,
                operatorDeclaration.ExpressionBody,
                operatorDeclaration.OperatorKeyword,
                model,
                results,
                options);
        }

        foreach (ConversionOperatorDeclarationSyntax conversion in root.DescendantNodes().OfType<ConversionOperatorDeclarationSyntax>())
        {
            CollectFromCallable(
                conversion,
                conversion.Body,
                conversion.ExpressionBody,
                conversion.ImplicitOrExplicitKeyword,
                model,
                results,
                options);
        }

        foreach (AccessorDeclarationSyntax accessor in root.DescendantNodes().OfType<AccessorDeclarationSyntax>())
        {
            SyntaxToken nameToken = accessor.Keyword;
            CollectFromCallable(accessor, accessor.Body, accessor.ExpressionBody, nameToken, model, results, options);
        }

        foreach (PropertyDeclarationSyntax property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (property.ExpressionBody is null)
            {
                continue;
            }

            CollectFromCallable(property, null, property.ExpressionBody, property.Identifier, model, results, options);
        }
    }

    private static void CollectFromCallable(
        SyntaxNode callableNode,
        BlockSyntax? body,
        ArrowExpressionClauseSyntax? expressionBody,
        SyntaxToken nameToken,
        SemanticModel model,
        List<ExitPointEntry> results,
        ExitPointCollectorOptions options)
    {
        ISymbol? symbol = model.GetDeclaredSymbol(callableNode);
        string methodId = ResolveMethodId(symbol, nameToken);
        string methodDisplayName = ResolveMethodDisplayName(symbol, nameToken);

        CompletionWalker.WalkCallableBody(
            body,
            expressionBody,
            methodId,
            methodDisplayName,
            results,
            isLocal: false,
            model,
            options);
    }

    /// <remarks>Skips generated, intermediate, and build output trees.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool ShouldSkipTree(SyntaxTree tree)
    {
        string path = tree.FilePath;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <remarks>Fallback when semantic binding is unavailable for a callable syntax node.</remarks>
    [ExcludeFromCodeCoverage]
    private static string ResolveMethodId(ISymbol? symbol, SyntaxToken nameToken) =>
        symbol?.ToDisplayString(MethodIdFormat) ?? nameToken.Text;

    /// <remarks>Fallback when semantic binding is unavailable for a callable syntax node.</remarks>
    [ExcludeFromCodeCoverage]
    private static string ResolveMethodDisplayName(ISymbol? symbol, SyntaxToken nameToken) =>
        symbol?.Name ?? nameToken.Text;
}


