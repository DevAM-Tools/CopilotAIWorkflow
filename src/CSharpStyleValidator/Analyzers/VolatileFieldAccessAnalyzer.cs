// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CSharpStyleValidator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleValidator.Analyzers;

/// <summary>Forbids plain access to <c>volatile</c> fields; requires <c>System.Threading.Volatile</c> or <c>System.Threading.Interlocked</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VolatileFieldAccessAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.VolatileFieldAccess);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeNode,
            SyntaxKind.IdentifierName,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.AddAssignmentExpression,
            SyntaxKind.SubtractAssignmentExpression,
            SyntaxKind.MultiplyAssignmentExpression,
            SyntaxKind.DivideAssignmentExpression,
            SyntaxKind.ModuloAssignmentExpression,
            SyntaxKind.AndAssignmentExpression,
            SyntaxKind.OrAssignmentExpression,
            SyntaxKind.ExclusiveOrAssignmentExpression,
            SyntaxKind.LeftShiftAssignmentExpression,
            SyntaxKind.RightShiftAssignmentExpression,
            SyntaxKind.PreIncrementExpression,
            SyntaxKind.PreDecrementExpression,
            SyntaxKind.PostIncrementExpression,
            SyntaxKind.PostDecrementExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode node = context.Node;
        if (node is AssignmentExpressionSyntax assignment)
        {
            AnalyzeExpression(context, assignment.Left, assignment.Left.GetLocation());
            return;
        }

        if (node is PrefixUnaryExpressionSyntax prefixUnary)
        {
            AnalyzeExpression(context, prefixUnary.Operand, prefixUnary.Operand.GetLocation());
            return;
        }

        if (node is PostfixUnaryExpressionSyntax postfixUnary)
        {
            AnalyzeExpression(context, postfixUnary.Operand, postfixUnary.Operand.GetLocation());
            return;
        }

        if (node is ExpressionSyntax expression)
        {
            if (IsNameDeclaration(expression) || IsWriteTargetOperand(expression))
            {
                return;
            }

            AnalyzeExpression(context, expression, expression.GetLocation());
        }
    }

    private static void AnalyzeExpression(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax expression,
        Location diagnosticLocation)
    {
        if (!TryGetVolatileField(expression, context.SemanticModel, out IFieldSymbol field))
        {
            return;
        }

        if (IsAllowedVolatileFieldUse(expression, context.SemanticModel))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.VolatileFieldAccess,
            diagnosticLocation,
            field.Name));
    }

    /// <remarks>Resolves field symbols for identifier and member-access expressions.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool TryGetVolatileField(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        out IFieldSymbol field)
    {
        field = null!;
        ISymbol symbol = semanticModel.GetSymbolInfo(expression).Symbol;
        if (symbol is not IFieldSymbol fieldSymbol || !fieldSymbol.IsVolatile)
        {
            return false;
        }

        field = fieldSymbol;
        return true;
    }

    /// <remarks>Skips declarator and parameter names.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsNameDeclaration(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier
            && identifier.Parent is VariableDeclaratorSyntax declarator
            && declarator.Identifier == identifier.Identifier)
        {
            return true;
        }

        if (expression is IdentifierNameSyntax parameterIdentifier
            && parameterIdentifier.Parent is ParameterSyntax parameter
            && parameter.Identifier == parameterIdentifier.Identifier)
        {
            return true;
        }

        return false;
    }

    /// <remarks>Avoid duplicate diagnostics when assignment or unary handles the write.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsWriteTargetOperand(ExpressionSyntax expression)
    {
        return expression.Parent switch
        {
            AssignmentExpressionSyntax assignment when assignment.Left == expression => true,
            PrefixUnaryExpressionSyntax prefix when prefix.Operand == expression => true,
            PostfixUnaryExpressionSyntax postfix when postfix.Operand == expression => true,
            _ => false,
        };
    }

    /// <remarks>Allows <c>ref</c> arguments to <c>Volatile.Read</c>/<c>Write</c> and <c>Interlocked.*</c> APIs.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsAllowedVolatileFieldUse(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression.Parent is ArgumentSyntax argument
            && argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
            && argument.Parent is ArgumentListSyntax argumentList
            && argumentList.Parent is InvocationExpressionSyntax invocation)
        {
            return IsVolatileOrInterlockedInvocation(invocation, semanticModel);
        }

        if (expression.Parent is RefExpressionSyntax refExpression
            && refExpression.Parent is ArgumentSyntax refArgument
            && refArgument.Parent is ArgumentListSyntax refArgumentList
            && refArgumentList.Parent is InvocationExpressionSyntax refInvocation)
        {
            return IsVolatileOrInterlockedInvocation(refInvocation, semanticModel);
        }

        return false;
    }

    /// <remarks>Falls back to member-access text when semantic binding is unavailable (e.g. static imports).</remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsVolatileOrInterlockedInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        ISymbol symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
        if (symbol is IMethodSymbol method && method.ContainingType is not null)
        {
            string containingType = method.ContainingType.ToDisplayString();
            if (containingType is "System.Threading.Volatile" or "System.Threading.Interlocked")
            {
                return true;
            }
        }

        string targetText = memberAccess.Expression.ToString();
        return targetText is "Volatile" or "Interlocked" or "Threading.Volatile" or "Threading.Interlocked";
    }
}
