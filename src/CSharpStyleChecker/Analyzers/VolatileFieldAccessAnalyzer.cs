// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CSharpStyleChecker.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleChecker.Analyzers;

/// <summary>Forbids non-atomic read-modify-write on <c>volatile</c> fields; plain volatile read/write is allowed.</summary>
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
        if (TryGetTargetExpression(context.Node, out ExpressionSyntax target)
            && TryGetVolatileField(target, context.SemanticModel, out IFieldSymbol field))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.VolatileFieldAccess,
                target.GetLocation(),
                field.Name));
        }
    }

    /// <remarks>Defensive: registered syntax kinds map to assignment/unary nodes; default is unreachable in practice.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool TryGetTargetExpression(SyntaxNode node, out ExpressionSyntax target)
    {
        switch (node)
        {
            case AssignmentExpressionSyntax assignment:
                target = assignment.Left;
                return true;
            case PrefixUnaryExpressionSyntax prefixUnary:
                target = prefixUnary.Operand;
                return true;
            case PostfixUnaryExpressionSyntax postfixUnary:
                target = postfixUnary.Operand;
                return true;
            default:
                target = null!;
                return false;
        }
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
}
