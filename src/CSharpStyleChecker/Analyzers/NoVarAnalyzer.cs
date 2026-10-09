// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using CSharpStyleChecker.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleChecker.Analyzers;

/// <summary>Forbids <c>var</c> in local declarations.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoVarAnalyzer : DiagnosticAnalyzer
{
    #region Lifecycle

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NoVar);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ForEachStatement);
        context.RegisterSyntaxNodeAction(AnalyzeForEachVariable, SyntaxKind.ForEachVariableStatement);
        context.RegisterSyntaxNodeAction(
            AnalyzeVariableDeclaration,
            SyntaxKind.VariableDeclaration);
        context.RegisterSyntaxNodeAction(
            AnalyzeDeclarationExpression,
            SyntaxKind.DeclarationExpression);
    }

    #endregion

    #region Private helpers

    private static void AnalyzeDeclarationExpression(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not DeclarationExpressionSyntax declaration || !declaration.Type.IsVar)
        {
            return;
        }

        // foreach (var (a, b) in …) is reported once by AnalyzeForEachVariable.
        if (declaration.Parent is ForEachVariableStatementSyntax)
        {
            return;
        }

        Report(context, declaration.Type.GetLocation());
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not VariableDeclarationSyntax declaration || !declaration.Type.IsVar)
        {
            return;
        }

        // using var / await using var remain LocalDeclarationStatementSyntax.
        if (declaration.Parent is LocalDeclarationStatementSyntax
            or ForStatementSyntax
            or UsingStatementSyntax
            or FixedStatementSyntax)
        {
            Report(context, declaration.Type.GetLocation());
        }
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is ForEachStatementSyntax foreachStatement && foreachStatement.Type.IsVar)
        {
            Report(context, foreachStatement.Type.GetLocation());
        }
    }

    private static void AnalyzeForEachVariable(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ForEachVariableStatementSyntax statement)
        {
            return;
        }

        foreach (SyntaxNode node in statement.Variable.DescendantNodesAndSelf())
        {
            if (node is DeclarationExpressionSyntax { Type.IsVar: true } declaration)
            {
                Report(context, declaration.Type.GetLocation());
                return;
            }

            if (node is VariableDeclarationSyntax { Type.IsVar: true } variableDeclaration)
            {
                Report(context, variableDeclaration.Type.GetLocation());
                return;
            }
        }
    }

    private static void Report(SyntaxNodeAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NoVar, location));
    }

    #endregion
}
