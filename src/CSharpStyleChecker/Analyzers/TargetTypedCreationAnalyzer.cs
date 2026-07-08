// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CSharpStyleChecker.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleChecker.Analyzers;

/// <summary>Requires target-typed <c>new()</c> and <c>[]</c> instead of redundant explicit type names.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TargetTypedCreationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.TargetTypedCreation);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeArrayCreation, SyntaxKind.ArrayCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        ObjectCreationExpressionSyntax creation = (ObjectCreationExpressionSyntax)context.Node;

        if (creation.ArgumentList?.Arguments.Count > 0)
        {
            return;
        }

        if (creation.Initializer is InitializerExpressionSyntax objectInitializer
            && objectInitializer.IsKind(SyntaxKind.ObjectInitializerExpression))
        {
            if (objectInitializer.Expressions.Count > 0)
            {
                return;
            }
        }
        else if (creation.Initializer is InitializerExpressionSyntax collectionInitializer
            && collectionInitializer.IsKind(SyntaxKind.CollectionInitializerExpression))
        {
            if (ShouldExemptCollectionReplacement(context.SemanticModel, creation))
            {
                return;
            }

            Report(context, creation.Type);
            return;
        }

        if (IsNoTargetTypedContext(creation))
        {
            return;
        }

        if (ShouldExemptTypeMismatch(context.SemanticModel, creation))
        {
            return;
        }

        Report(context, creation.Type);
    }

    private static void AnalyzeArrayCreation(SyntaxNodeAnalysisContext context)
    {
        ArrayCreationExpressionSyntax creation = (ArrayCreationExpressionSyntax)context.Node;
        if (creation.Initializer is null)
        {
            return;
        }

        if (ShouldExemptCollectionReplacement(context.SemanticModel, creation))
        {
            return;
        }

        Report(context, creation.Type);
    }

    private static void Report(SyntaxNodeAnalysisContext context, TypeSyntax typeSyntax)
    {
        string typeName = typeSyntax.ToString();
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TargetTypedCreation,
            typeSyntax.GetLocation(),
            typeName));
    }

    /// <remarks><c>throw new X()</c> has no target-typed <c>new()</c> context.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsNoTargetTypedContext(ObjectCreationExpressionSyntax creation)
    {
        for (SyntaxNode node = creation.Parent; node is not null; node = node.Parent)
        {
            if (node is ThrowStatementSyntax)
            {
                return true;
            }
        }

        return false;
    }

    /// <remarks>Skips polymorphic, interface-target, and type-parameter constructions.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool ShouldExemptTypeMismatch(SemanticModel semanticModel, ObjectCreationExpressionSyntax creation)
    {
        if (semanticModel.GetSymbolInfo(creation.Type).Symbol is ITypeParameterSymbol)
        {
            return true;
        }

        ITypeSymbol creationType = semanticModel.GetTypeInfo(creation).Type;
        ITypeSymbol targetType = GetContextualTargetType(semanticModel, creation);
        if (targetType is null || creationType is null)
        {
            return true;
        }

        return !SymbolEqualityComparer.Default.Equals(creationType, targetType);
    }

    /// <remarks>
    /// Skips collection-to-<c>[]</c> suggestions when the contextual target cannot use a collection expression
    /// (e.g. <c>ReadOnlyMemory&lt;byte&gt;</c>) or differs from the created type (e.g. <c>byte[]</c> to <c>ReadOnlyMemory&lt;byte&gt;</c>).
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private static bool ShouldExemptCollectionReplacement(SemanticModel semanticModel, ExpressionSyntax creation)
    {
        ITypeSymbol creationType = semanticModel.GetTypeInfo(creation).Type;
        ITypeSymbol targetType = GetContextualTargetType(semanticModel, creation);
        if (creationType is null || targetType is null)
        {
            return true;
        }

        if (!SymbolEqualityComparer.Default.Equals(creationType, targetType))
        {
            return true;
        }

        return !SupportsCollectionExpressionTarget(targetType);
    }

    /// <remarks><c>Memory&lt;T&gt;</c> and <c>ReadOnlyMemory&lt;T&gt;</c> cannot be initialized with <c>[]</c>.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool SupportsCollectionExpressionTarget(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        if (type is not INamedTypeSymbol named)
        {
            return true;
        }

        INamedTypeSymbol original = named.OriginalDefinition;
        if (original.ContainingNamespace?.ToDisplayString() == "System"
            && (original.Name == "Memory" || original.Name == "ReadOnlyMemory"))
        {
            return false;
        }

        return true;
    }

    /// <remarks>Resolves the type expected at the creation site from declaration, assignment, return, or argument context.</remarks>
    [ExcludeFromCodeCoverage]
    private static ITypeSymbol GetContextualTargetType(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        SyntaxNode parent = expression.Parent;
        if (parent is EqualsValueClauseSyntax equalsValue)
        {
            return GetTypeFromEqualsValue(semanticModel, equalsValue);
        }

        if (parent is ArgumentSyntax argument)
        {
            return GetTypeFromArgument(semanticModel, argument);
        }

        if (parent is ReturnStatementSyntax)
        {
            return GetEnclosingMethodReturnType(semanticModel, expression);
        }

        if (parent is AssignmentExpressionSyntax assignment && assignment.Right == expression)
        {
            return semanticModel.GetTypeInfo(assignment.Left).Type;
        }

        return null;
    }

    [ExcludeFromCodeCoverage]
    private static ITypeSymbol GetTypeFromEqualsValue(SemanticModel semanticModel, EqualsValueClauseSyntax equalsValue)
    {
        if (equalsValue.Parent is not VariableDeclaratorSyntax declarator)
        {
            return null;
        }

        return semanticModel.GetDeclaredSymbol(declarator) switch
        {
            ILocalSymbol local => local.Type,
            IFieldSymbol field => field.Type,
            _ => null,
        };
    }

    [ExcludeFromCodeCoverage]
    private static ITypeSymbol GetTypeFromArgument(SemanticModel semanticModel, ArgumentSyntax argument)
    {
        if (argument.Parent is not ArgumentListSyntax argumentList)
        {
            return null;
        }

        int index = argumentList.Arguments.IndexOf(argument);
        if (argumentList.Parent is InvocationExpressionSyntax invocation
            && semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol invocationMethod
            && index < invocationMethod.Parameters.Length)
        {
            return invocationMethod.Parameters[index].Type;
        }

        if (argumentList.Parent is ObjectCreationExpressionSyntax objectCreation
            && semanticModel.GetSymbolInfo(objectCreation).Symbol is IMethodSymbol constructor
            && index < constructor.Parameters.Length)
        {
            return constructor.Parameters[index].Type;
        }

        return null;
    }

    [ExcludeFromCodeCoverage]
    private static ITypeSymbol GetEnclosingMethodReturnType(SemanticModel semanticModel, SyntaxNode node)
    {
        for (SyntaxNode current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is MethodDeclarationSyntax method
                && semanticModel.GetDeclaredSymbol(method) is IMethodSymbol methodSymbol)
            {
                return methodSymbol.ReturnType;
            }
        }

        return null;
    }
}
