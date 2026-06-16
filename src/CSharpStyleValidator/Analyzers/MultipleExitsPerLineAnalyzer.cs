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

        foreach (IGrouping<(string MethodId, string FilePath, int Line), ExitPointEntry> group in included
                     .GroupBy(static exit => (exit.MethodId, exit.FilePath, exit.Line)))
        {
            TryReportGroup(context, group.ToList());
        }
    }

    /// <remarks>Maps grouped exit points to CSV006 diagnostics.</remarks>
    [ExcludeFromCodeCoverage]
    private static void TryReportGroup(CompilationAnalysisContext context, List<ExitPointEntry> groupList)
    {
        if (groupList.Count <= 1)
        {
            return;
        }

        ExitPointEntry first = groupList[0];
        string kinds = string.Join(", ", groupList.Select(static exit => exit.Kind.ToString()));
        Location location = TryCreateLocation(context.Compilation, first);
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MultipleExitsPerLine,
            location,
            first.MethodDisplayName,
            groupList.Count,
            first.Line,
            kinds));
    }

    [ExcludeFromCodeCoverage]
    internal static Location TryCreateLocation(Compilation compilation, ExitPointEntry entry)
    {
        SyntaxTree tree = compilation.SyntaxTrees.FirstOrDefault(
            candidate => string.Equals(candidate.FilePath, entry.FilePath, StringComparison.OrdinalIgnoreCase));

        if (tree is null)
        {
            return null;
        }

        SourceText text = tree.GetText();
        int lineIndex = entry.Line - 1;
        if (lineIndex < 0 || lineIndex >= text.Lines.Count)
        {
            return null;
        }

        TextLine line = text.Lines[lineIndex];
        int column = Math.Min(entry.Column - 1, line.Span.Length);
        int position = line.Start + column;
        if (position > text.Length)
        {
            position = line.Start;
        }

        return Location.Create(tree, TextSpan.FromBounds(position, position));
    }
}
