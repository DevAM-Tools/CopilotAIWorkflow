// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CSharpStyleValidator.Diagnostics;
using ExitPoints;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CSharpStyleValidator.Analyzers;

/// <summary>Forbids multiple callable exit points on the same source line.</summary>
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
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(context.Compilation);
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

    private static string ResolveGroupKey(ExitPointEntry exit) =>
        exit.OperatorGroupId ?? $"line:{exit.Line}";

    /// <remarks>Maps grouped exit points to CSV006 diagnostics.</remarks>
    [ExcludeFromCodeCoverage]
    private static void TryReportGroup(CompilationAnalysisContext context, List<ExitPointEntry> groupList)
    {
        if (groupList.Count <= 1)
        {
            return;
        }

        ExitPointEntry first = groupList[0];
        int reportLine = first.OperatorLine ?? first.Line;
        int reportColumn = first.OperatorColumn ?? first.Column;
        string kinds = string.Join(", ", groupList.Select(static exit => exit.Kind.ToString()));
        Location location = TryCreateLocation(context.Compilation, first.FilePath, reportLine, reportColumn);
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MultipleExitsPerLine,
            location,
            first.MethodDisplayName,
            groupList.Count,
            reportLine,
            kinds));
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
