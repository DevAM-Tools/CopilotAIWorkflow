// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExitPoints;

/// <summary>
/// Collects callable exit points from a Roslyn compilation.
/// Thread-safe; caches per <see cref="Compilation"/> with concurrent lookups.
/// </summary>
public static class ExitPointCollector
{
    #region Fields

    private static readonly ConditionalWeakTable<Compilation, CachedCompilationResults> ResultsByCompilation = new();

    private static readonly SymbolDisplayFormat MethodIdFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    #endregion

    #region Public API

    /// <summary>Collects all exit points in <paramref name="compilation"/>.</summary>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <param name="options">Optional collection options.</param>
    /// <param name="cancellationToken">Cancels tree walks.</param>
    /// <returns>Collected exit points.</returns>
    public static IReadOnlyList<ExitPointEntry> Collect(
        Compilation compilation,
        ExitPointCollectorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        ExitPointCollectorOptions effectiveOptions = options ?? new ExitPointCollectorOptions();
        CachedCompilationResults cache = ResultsByCompilation.GetValue(compilation, static _ => new CachedCompilationResults());
        return cache.GetOrAdd(
            effectiveOptions.IncludeLocalFunctions,
            () => CollectCore(compilation, effectiveOptions, cancellationToken));
    }

    #endregion

    #region Private helpers

    private static List<ExitPointEntry> CollectCore(
        Compilation compilation,
        ExitPointCollectorOptions effectiveOptions,
        CancellationToken cancellationToken)
    {
        List<ExitPointEntry> results = [];

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ShouldSkipTree(tree))
            {
                continue;
            }

            SemanticModel model = compilation.GetSemanticModel(tree)
                ?? throw new InvalidOperationException("Semantic model is unavailable for a compilation syntax tree.");

            CollectFromTree(tree, model, effectiveOptions, results, cancellationToken);
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
        List<ExitPointEntry> results,
        CancellationToken cancellationToken)
    {
        SyntaxNode root = tree.GetRoot(cancellationToken);
        CollectDeclarations(root, model, options, results, cancellationToken);
    }

    /// <summary>
    /// Declaration kinds live on types, namespaces, and accessor lists.
    /// Method bodies are not scanned here because local functions are collected while walking the body.
    /// One walk replaces seven <c>DescendantNodes</c> passes.
    /// </summary>
    private static void CollectDeclarations(
        SyntaxNode node,
        SemanticModel model,
        ExitPointCollectorOptions options,
        List<ExitPointEntry> results,
        CancellationToken cancellationToken)
    {
        foreach (SyntaxNode child in node.ChildNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (child)
            {
                case MethodDeclarationSyntax method:
                    CollectFromCallable(method, method.Body, method.ExpressionBody, method.Identifier, model, results, options);
                    break;
                case ConstructorDeclarationSyntax constructor:
                    CollectFromCallable(constructor, constructor.Body, constructor.ExpressionBody, constructor.Identifier, model, results, options);
                    break;
                case DestructorDeclarationSyntax destructor:
                    CollectFromCallable(destructor, destructor.Body, null, destructor.TildeToken, model, results, options);
                    break;
                case OperatorDeclarationSyntax operatorDeclaration:
                    CollectFromCallable(
                        operatorDeclaration,
                        operatorDeclaration.Body,
                        operatorDeclaration.ExpressionBody,
                        operatorDeclaration.OperatorKeyword,
                        model,
                        results,
                        options);
                    break;
                case ConversionOperatorDeclarationSyntax conversion:
                    CollectFromCallable(
                        conversion,
                        conversion.Body,
                        conversion.ExpressionBody,
                        conversion.ImplicitOrExplicitKeyword,
                        model,
                        results,
                        options);
                    break;
                case AccessorDeclarationSyntax accessor:
                    CollectFromCallable(accessor, accessor.Body, accessor.ExpressionBody, accessor.Keyword, model, results, options);
                    break;
                case PropertyDeclarationSyntax property:
                    CollectExpressionBody(property, property.ExpressionBody, property.Identifier, property.AccessorList, model, results, options, cancellationToken);
                    break;
                case IndexerDeclarationSyntax indexer:
                    CollectExpressionBody(indexer, indexer.ExpressionBody, indexer.ThisKeyword, indexer.AccessorList, model, results, options, cancellationToken);
                    break;
                case EventDeclarationSyntax eventDeclaration:
                    if (eventDeclaration.AccessorList is not null)
                    {
                        CollectDeclarations(eventDeclaration.AccessorList, model, options, results, cancellationToken);
                    }

                    break;
                case BaseNamespaceDeclarationSyntax:
                case BaseTypeDeclarationSyntax:
                    CollectDeclarations(child, model, options, results, cancellationToken);
                    break;
            }
        }
    }

    private static void CollectExpressionBody(
        SyntaxNode callableNode,
        ArrowExpressionClauseSyntax? expressionBody,
        SyntaxToken nameToken,
        AccessorListSyntax? accessorList,
        SemanticModel model,
        List<ExitPointEntry> results,
        ExitPointCollectorOptions options,
        CancellationToken cancellationToken)
    {
        if (expressionBody is not null)
        {
            CollectFromCallable(callableNode, null, expressionBody, nameToken, model, results, options);
        }

        if (accessorList is not null)
        {
            CollectDeclarations(accessorList, model, options, results, cancellationToken);
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

    #endregion
}
