// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using CSharpStyleValidator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleValidator.Analyzers;

/// <summary>Forbids <c>var</c> in local declarations.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoVarAnalyzer : DiagnosticAnalyzer
{
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
        context.RegisterSyntaxNodeAction(
            AnalyzeVariableDeclaration,
            SyntaxKind.VariableDeclaration);
        context.RegisterSyntaxNodeAction(
            AnalyzeDeclarationExpression,
            SyntaxKind.DeclarationExpression);
    }

    private static void AnalyzeDeclarationExpression(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is DeclarationExpressionSyntax declaration && declaration.Type.IsVar)
        {
            Report(context, declaration.Type.GetLocation());
        }
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is VariableDeclarationSyntax declaration
            && declaration.Type.IsVar
            && declaration.Parent is LocalDeclarationStatementSyntax)
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

    private static void Report(SyntaxNodeAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NoVar, location));
    }
}
