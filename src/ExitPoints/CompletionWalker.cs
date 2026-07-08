// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExitPoints;

/// <summary>Walks callable bodies and completion expressions for exit-point collection.</summary>
/// <remarks>
/// Does not model <c>goto</c> (not an exit point), filter/catch-only control flow, or async state-machine exits inside
/// expression-bodied members; those patterns may be under-reported. <c>yield return</c> is not an exit point; only
/// <c>yield break</c> terminates the iterator. Models <c>??</c> and <c>??=</c> as dual completion arms.
/// Multi-arm <c>?:</c>, <c>??</c>, <c>??=</c>, and <c>switch</c> exits share an <see cref="ExitPointEntry.OperatorGroupId"/> across line breaks.
/// </remarks>
internal static class CompletionWalker
{
    private static readonly SymbolDisplayFormat MethodIdFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static void WalkCompletionExpression(
        ExpressionSyntax expression,
        string methodId,
        string methodDisplayName,
        List<ExitPointEntry> results)
    {
        WalkCompletionExpression(expression, methodId, methodDisplayName, results, ExitKind.ExpressionCompletion);
    }

    /// <remarks>Walks completion expressions for exit-point collection.</remarks>
    [ExcludeFromCodeCoverage]
    private static void WalkCompletionExpression(
        ExpressionSyntax expression,
        string methodId,
        string methodDisplayName,
        List<ExitPointEntry> results,
        ExitKind leafKind,
        OperatorGroup? operatorGroup = null)
    {
        switch (expression)
        {
            case ConditionalExpressionSyntax conditional:
                OperatorGroup conditionalGroup = CreateOperatorGroup(methodId, GetQuestionMarkToken(conditional));
                WalkCompletionExpression(
                    conditional.WhenTrue,
                    methodId,
                    methodDisplayName,
                    results,
                    ExitKind.ConditionalArmCompletion,
                    conditionalGroup);
                WalkCompletionExpression(
                    conditional.WhenFalse,
                    methodId,
                    methodDisplayName,
                    results,
                    ExitKind.ConditionalArmCompletion,
                    conditionalGroup);
                break;

            case SwitchExpressionSyntax switchExpression:
                OperatorGroup switchGroup = CreateOperatorGroup(methodId, switchExpression.SwitchKeyword);
                foreach (SwitchExpressionArmSyntax arm in switchExpression.Arms)
                {
                    WalkCompletionExpression(
                        arm.Expression,
                        methodId,
                        methodDisplayName,
                        results,
                        ExitKind.SwitchArmCompletion,
                        switchGroup);
                }

                break;

            case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } coalesce:
                OperatorGroup coalesceGroup = CreateOperatorGroup(methodId, coalesce.OperatorToken);
                WalkCompletionExpression(
                    coalesce.Left,
                    methodId,
                    methodDisplayName,
                    results,
                    ExitKind.CoalesceArmCompletion,
                    coalesceGroup);
                if (coalesce.Right is ThrowExpressionSyntax rightThrow)
                {
                    AddExit(rightThrow.ThrowKeyword, methodId, methodDisplayName, ExitKind.ThrowExpression, results);
                }
                else
                {
                    WalkCompletionExpression(
                        coalesce.Right,
                        methodId,
                        methodDisplayName,
                        results,
                        ExitKind.CoalesceArmCompletion,
                        coalesceGroup);
                }

                break;

            case AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceAssignmentExpression } coalesceAssignment:
                OperatorGroup coalesceAssignmentGroup = CreateOperatorGroup(methodId, coalesceAssignment.OperatorToken);
                WalkCompletionExpression(
                    coalesceAssignment.Left,
                    methodId,
                    methodDisplayName,
                    results,
                    ExitKind.CoalesceArmCompletion,
                    coalesceAssignmentGroup);
                WalkCompletionExpression(
                    coalesceAssignment.Right,
                    methodId,
                    methodDisplayName,
                    results,
                    ExitKind.CoalesceArmCompletion,
                    coalesceAssignmentGroup);
                break;

            case ThrowExpressionSyntax throwExpression:
                AddExit(throwExpression.ThrowKeyword, methodId, methodDisplayName, ExitKind.ThrowExpression, results);
                break;

            default:
                AddExitFromExpression(expression, methodId, methodDisplayName, leafKind, results, operatorGroup);
                break;
        }
    }

    public static void WalkBlockStatements(
        SyntaxList<StatementSyntax> statements,
        string methodId,
        string methodDisplayName,
        List<ExitPointEntry> results,
        SemanticModel model,
        ExitPointCollectorOptions options)
    {
        foreach (StatementSyntax statement in statements)
        {
            WalkStatement(statement, methodId, methodDisplayName, results, model, options);
        }
    }

    /// <remarks>Walks callable statement trees for exit-point collection.</remarks>
    [ExcludeFromCodeCoverage]
    public static void WalkStatement(
        StatementSyntax statement,
        string methodId,
        string methodDisplayName,
        List<ExitPointEntry> results,
        SemanticModel model,
        ExitPointCollectorOptions options)
    {
        switch (statement)
        {
            case ReturnStatementSyntax returnStatement:
                if (returnStatement.Expression is null)
                {
                    AddExit(returnStatement.ReturnKeyword, methodId, methodDisplayName, ExitKind.Return, results);
                }
                else if (IsSimpleCompletionOperand(returnStatement.Expression))
                {
                    AddExit(returnStatement.ReturnKeyword, methodId, methodDisplayName, ExitKind.Return, results);
                }
                else
                {
                    WalkCompletionExpression(returnStatement.Expression, methodId, methodDisplayName, results);
                }

                break;

            case ThrowStatementSyntax throwStatement:
                AddExit(throwStatement.ThrowKeyword, methodId, methodDisplayName, ExitKind.Throw, results);
                break;

            case YieldStatementSyntax { RawKind: (int)SyntaxKind.YieldBreakStatement } yieldBreak:
                AddExit(yieldBreak.YieldKeyword, methodId, methodDisplayName, ExitKind.YieldBreak, results);
                break;

            case BlockSyntax block:
                WalkBlockStatements(block.Statements, methodId, methodDisplayName, results, model, options);
                break;

            case SwitchStatementSyntax switchStatement:
                foreach (SwitchSectionSyntax section in switchStatement.Sections)
                {
                    foreach (StatementSyntax sectionStatement in section.Statements)
                    {
                        WalkStatement(sectionStatement, methodId, methodDisplayName, results, model, options);
                    }
                }

                break;

            case IfStatementSyntax ifStatement:
                if (ifStatement.Statement is not null)
                {
                    WalkStatement(ifStatement.Statement, methodId, methodDisplayName, results, model, options);
                }

                if (ifStatement.Else is not null)
                {
                    WalkStatement(ifStatement.Else.Statement, methodId, methodDisplayName, results, model, options);
                }

                break;

            case LocalFunctionStatementSyntax localFunction
                when localFunction.Body is not null || localFunction.ExpressionBody is not null:
                if (!options.IncludeLocalFunctions)
                {
                    break;
                }

                (string localId, string localName) = ResolveCallableIds(localFunction, localFunction.Identifier, model);
                WalkCallableBody(
                    localFunction.Body,
                    localFunction.ExpressionBody,
                    localId,
                    localName,
                    results,
                    isLocal: true,
                    model,
                    options);
                break;

            default:
                foreach (SyntaxNode child in statement.ChildNodes())
                {
                    if (child is StatementSyntax childStatement)
                    {
                        WalkStatement(childStatement, methodId, methodDisplayName, results, model, options);
                    }
                }

                break;
        }
    }

    public static void WalkCallableBody(
        BlockSyntax? body,
        ArrowExpressionClauseSyntax? expressionBody,
        string methodId,
        string methodDisplayName,
        List<ExitPointEntry> results,
        bool isLocal,
        SemanticModel model,
        ExitPointCollectorOptions options)
    {
        if (expressionBody is not null)
        {
            WalkCompletionExpression(expressionBody.Expression, methodId, methodDisplayName, results);
            return;
        }

        if (body is null)
        {
            return;
        }

        WalkBlockStatements(body.Statements, methodId, methodDisplayName, results, model, options);

        if (!isLocal)
        {
            AddExit(body.CloseBraceToken, methodId, methodDisplayName, ExitKind.ImplicitEnd, results);
        }
    }

    private static (string MethodId, string DisplayName) ResolveCallableIds(
        SyntaxNode callableNode,
        SyntaxToken nameToken,
        SemanticModel model)
    {
        ISymbol? symbol = model.GetDeclaredSymbol(callableNode);
        string methodId = ResolveCallableMethodId(symbol, nameToken);
        string methodDisplayName = ResolveCallableDisplayName(symbol, nameToken);
        return (methodId, methodDisplayName);
    }

    /// <remarks>Fallback when semantic binding is unavailable for a callable syntax node.</remarks>
    [ExcludeFromCodeCoverage]
    private static string ResolveCallableMethodId(ISymbol? symbol, SyntaxToken nameToken) =>
        symbol?.ToDisplayString(MethodIdFormat) ?? nameToken.Text;
    /// <remarks>Fallback when semantic binding is unavailable for a callable syntax node.</remarks>
    [ExcludeFromCodeCoverage]
    private static string ResolveCallableDisplayName(ISymbol? symbol, SyntaxToken nameToken) =>
        symbol?.Name ?? nameToken.Text;

    /// <remarks>Maps completion expressions to diagnostic exit tokens.</remarks>
    [ExcludeFromCodeCoverage]
    private static void AddExitFromExpression(
        ExpressionSyntax expression,
        string methodId,
        string methodDisplayName,
        ExitKind kind,
        List<ExitPointEntry> results,
        OperatorGroup? operatorGroup)
    {
        SyntaxToken token = expression switch
        {
            LiteralExpressionSyntax literal => literal.Token,
            IdentifierNameSyntax identifier => identifier.Identifier,
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier,
                IdentifierNameSyntax id => id.Identifier,
                _ => invocation.GetFirstToken(),
            },
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier,
            _ => expression.GetFirstToken(),
        };

        AddExit(token, methodId, methodDisplayName, kind, results, operatorGroup);
    }

    [ExcludeFromCodeCoverage]
    private static void AddExit(
        SyntaxToken token,
        string methodId,
        string methodDisplayName,
        ExitKind kind,
        List<ExitPointEntry> results,
        OperatorGroup? operatorGroup = null)
    {
        Location location = token.GetLocation();
        if (IsNonSourceLocation(location))
        {
            return;
        }

        FileLinePositionSpan span = location.GetLineSpan();
        string filePath = span.Path;
        int line = span.StartLinePosition.Line + 1;
        int column = span.StartLinePosition.Character + 1;
        string exitPointId = $"{methodId}:{line}:{column}:{kind}";
        string? operatorGroupId = null;
        int? operatorLine = null;
        int? operatorColumn = null;

        if (operatorGroup is not null && UsesOperatorGroup(kind))
        {
            operatorGroupId = operatorGroup.Value.GroupId;
            operatorLine = operatorGroup.Value.Line;
            operatorColumn = operatorGroup.Value.Column;
        }

        results.Add(new ExitPointEntry(
            exitPointId,
            filePath,
            line,
            column,
            methodId,
            methodDisplayName,
            kind,
            operatorGroupId,
            operatorLine,
            operatorColumn));
    }

    /// <remarks>Locates the <c>?</c> token for operator grouping; fallback path handles malformed trees only.</remarks>
    [ExcludeFromCodeCoverage]
    private static SyntaxToken GetQuestionMarkToken(ConditionalExpressionSyntax conditional)
    {
        foreach (SyntaxToken token in conditional.DescendantTokens(descendIntoTrivia: false))
        {
            if (token.IsKind(SyntaxKind.QuestionToken))
            {
                return token;
            }
        }

        return GetQuestionMarkTokenFallback(conditional);
    }

    /// <remarks>Defensive fallback when no <c>?</c> token appears in descendants (malformed or synthesized trees).</remarks>
    [ExcludeFromCodeCoverage]
    private static SyntaxToken GetQuestionMarkTokenFallback(ConditionalExpressionSyntax conditional) =>
        conditional.WhenTrue.GetFirstToken();

    private static OperatorGroup CreateOperatorGroup(string methodId, SyntaxToken operatorToken)
    {
        FileLinePositionSpan span = operatorToken.GetLocation().GetLineSpan();
        int line = span.StartLinePosition.Line + 1;
        int column = span.StartLinePosition.Character + 1;
        return new OperatorGroup($"{methodId}:{line}:{column}:multiarm", line, column);
    }

    private static bool UsesOperatorGroup(ExitKind kind) =>
        kind is ExitKind.ConditionalArmCompletion
            or ExitKind.CoalesceArmCompletion
            or ExitKind.SwitchArmCompletion;

    /// <summary>Non-source locations cannot produce stable file/line exit ids.</summary>
    /// <remarks>Defensive guard for synthesized tokens; production callers only pass in-source syntax tokens.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool IsNonSourceLocation(Location location) => !location.IsInSource;

    private static bool IsSimpleCompletionOperand(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax or IdentifierNameSyntax;
    }

    private readonly struct OperatorGroup(string groupId, int line, int column)
    {
        public string GroupId { get; } = groupId;

        public int Line { get; } = line;

        public int Column { get; } = column;
    }
}
