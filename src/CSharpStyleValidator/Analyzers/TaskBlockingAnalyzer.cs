// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CSharpStyleValidator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleValidator.Analyzers;

/// <summary>Forbids blocking on tasks via <c>.Wait()</c> or <c>.Result</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TaskBlockingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.TaskBlocking);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (!TryGetTaskWaitLocation(context, (InvocationExpressionSyntax)context.Node, out Location location))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TaskBlocking, location));
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (!TryGetTaskResultLocation(context, (MemberAccessExpressionSyntax)context.Node, out Location location))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TaskBlocking, location));
    }

    /// <remarks>Encapsulates task-wait pattern matching and semantic binding edge cases.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool TryGetTaskWaitLocation(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        out Location location)
    {
        location = null!;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.Text != "Wait")
        {
            return false;
        }

        ITypeSymbol type = (context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol as IFieldSymbol)?.Type
            ?? context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;

        if (type is null || !IsTaskType(type))
        {
            return false;
        }

        location = memberAccess.Name.GetLocation();
        return true;
    }

    /// <remarks>Encapsulates task-result pattern matching and semantic binding edge cases.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool TryGetTaskResultLocation(
        SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax memberAccess,
        out Location location)
    {
        location = null!;
        if (memberAccess.Name.Identifier.Text != "Result")
        {
            return false;
        }

        ITypeSymbol type = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
        if (type is null)
        {
            return false;
        }

        if (type is not INamedTypeSymbol named || named.TypeArguments.Length != 1 || !IsTaskType(named))
        {
            return false;
        }

        location = memberAccess.Name.GetLocation();
        return true;
    }

    /// <remarks>Only <see cref="System.Threading.Tasks.Task"/> types are blocked.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsTaskType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        INamedTypeSymbol definition = named.OriginalDefinition;
        if (definition.ContainingNamespace?.ToDisplayString() != "System.Threading.Tasks")
        {
            return false;
        }

        return definition.Name == "Task";
    }
}
