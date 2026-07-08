// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CSharpStyleChecker.Diagnostics;
using ExitPoints;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CSharpStyleChecker.Analyzers;

/// <summary>Forbids multiple callable exit points on the same source line; multi-line arms are allowed.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultipleExitsPerLineAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MultipleExitsPerLine);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(
            context.Compilation,
            new ExitPointCollectorOptions { IncludeLocalFunctions = true });
        List<ExitPointEntry> included = new List<ExitPointEntry>(exits.Count);

        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPointEntry exit = exits[exitIndex];
            SyntaxTree tree = context.Compilation.SyntaxTrees.FirstOrDefault(
                candidate => string.Equals(candidate.FilePath, exit.FilePath, StringComparison.OrdinalIgnoreCase));

            if (tree is not null && ExitPointExclusion.IsExcludedAtPosition(tree, exit.Line, exit.Column))
            {
                continue;
            }

            included.Add(exit);
        }

        foreach (IGrouping<(string MethodId, string FilePath, string GroupKey), ExitPointEntry> group in included
                     .GroupBy(static exit => (exit.MethodId, exit.FilePath, ResolveGroupKey(exit))))
        {
            TryReportGroup(context, group.ToList());
        }
    }

    private static string ResolveGroupKey(ExitPointEntry exit) => $"line:{exit.Line}";

    /// <remarks>Maps grouped exit points to CSC006 diagnostics.</remarks>
    [ExcludeFromCodeCoverage]
    private static void TryReportGroup(CompilationAnalysisContext context, List<ExitPointEntry> groupList)
    {
        if (groupList.Count <= 1)
        {
            return;
        }

        groupList.Sort(static (left, right) => left.Column.CompareTo(right.Column));
        int sharedLine = groupList[0].Line;
        ExitPointEntry anchor = SelectReportAnchor(groupList, sharedLine);
        int reportColumn = anchor.OperatorLine == sharedLine && anchor.OperatorColumn is int operatorColumn
            ? operatorColumn
            : anchor.Column;
        string kinds = string.Join(", ", groupList.Select(static exit => exit.Kind.ToString()));
        Location location = TryCreateLocation(context.Compilation, anchor.FilePath, sharedLine, reportColumn);
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MultipleExitsPerLine,
            location,
            anchor.MethodDisplayName,
            groupList.Count,
            sharedLine,
            kinds));
    }

    private static ExitPointEntry SelectReportAnchor(List<ExitPointEntry> groupList, int sharedLine)
    {
        for (int exitIndex = 0; exitIndex < groupList.Count; exitIndex++)
        {
            ExitPointEntry exit = groupList[exitIndex];
            if (exit.OperatorLine == sharedLine && exit.OperatorColumn is not null)
            {
                return exit;
            }
        }

        return groupList[0];
    }

    [ExcludeFromCodeCoverage]
    internal static Location TryCreateLocation(Compilation compilation, ExitPointEntry entry) =>
        TryCreateLocation(compilation, entry.FilePath, entry.OperatorLine ?? entry.Line, entry.OperatorColumn ?? entry.Column);

    [ExcludeFromCodeCoverage]
    internal static Location TryCreateLocation(Compilation compilation, string filePath, int line, int column)
    {
        SyntaxTree tree = compilation.SyntaxTrees.FirstOrDefault(
            candidate => string.Equals(candidate.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (tree is null)
        {
            return null;
        }

        SourceText text = tree.GetText();
        int lineIndex = line - 1;
        if (lineIndex < 0 || lineIndex >= text.Lines.Count)
        {
            return null;
        }

        TextLine textLine = text.Lines[lineIndex];
        int columnIndex = Math.Min(column - 1, textLine.Span.Length);
        int position = textLine.Start + columnIndex;
        if (position > text.Length)
        {
            position = textLine.Start;
        }

        return Location.Create(tree, TextSpan.FromBounds(position, position));
    }
}
