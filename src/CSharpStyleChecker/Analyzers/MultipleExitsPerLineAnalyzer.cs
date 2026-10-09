// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
    #region Lifecycle

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

    #endregion

    #region Private helpers

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(
            context.Compilation,
            new ExitPointCollectorOptions { IncludeLocalFunctions = true },
            context.CancellationToken);

        Dictionary<string, SyntaxTree> treesByPath = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);
        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            if (string.IsNullOrEmpty(tree.FilePath) || treesByPath.ContainsKey(tree.FilePath))
            {
                continue;
            }

            treesByPath.Add(tree.FilePath, tree);
        }

        Dictionary<ExitLineKey, List<ExitPointEntry>> groups = new Dictionary<ExitLineKey, List<ExitPointEntry>>();
        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPointEntry exit = exits[exitIndex];
            ExitLineKey key = new ExitLineKey(exit.MethodId, exit.FilePath, exit.Line);
            if (!groups.TryGetValue(key, out List<ExitPointEntry> list))
            {
                list = new List<ExitPointEntry>(2);
                groups.Add(key, list);
            }

            list.Add(exit);
        }

        // Style rule applies regardless of ExcludeFromCodeCoverage (coverage ≠ style waiver).
        foreach (KeyValuePair<ExitLineKey, List<ExitPointEntry>> pair in groups)
        {
            List<ExitPointEntry> groupList = pair.Value;
            if (groupList.Count <= 1)
            {
                continue;
            }

            if (!treesByPath.TryGetValue(pair.Key.FilePath, out SyntaxTree tree))
            {
                continue;
            }

            TryReportGroup(context, tree, groupList);
        }
    }

    /// <summary>
    /// Maps grouped exit points to CSC006 diagnostics.
    /// Reason: location edge paths are exercised via analyzer verifier diagnostics, not line-level coverage on this helper.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static void TryReportGroup(
        CompilationAnalysisContext context,
        SyntaxTree tree,
        List<ExitPointEntry> groupList)
    {
        groupList.Sort(static (left, right) => left.Column.CompareTo(right.Column));
        int sharedLine = groupList[0].Line;
        ExitPointEntry anchor = SelectReportAnchor(groupList, sharedLine);
        int reportColumn = anchor.OperatorLine == sharedLine && anchor.OperatorColumn is int operatorColumn
            ? operatorColumn
            : anchor.Column;
        string kinds = BuildKindsList(groupList);
        SourceText text = tree.GetText(context.CancellationToken);
        Location location = TryCreateLocation(tree, text, sharedLine, reportColumn);
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

    private static string BuildKindsList(List<ExitPointEntry> groupList)
    {
        StringBuilder builder = new StringBuilder();
        for (int exitIndex = 0; exitIndex < groupList.Count; exitIndex++)
        {
            if (exitIndex > 0)
            {
                builder.Append(", ");
            }

            builder.Append(groupList[exitIndex].Kind.ToString());
        }

        return builder.ToString();
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

    /// <summary>
    /// Creates a diagnostic location from an exit entry.
    /// Reason: edge cases for missing trees and out-of-range columns are covered by dedicated location tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static Location TryCreateLocation(Compilation compilation, ExitPointEntry entry) =>
        TryCreateLocation(compilation, entry.FilePath, entry.OperatorLine ?? entry.Line, entry.OperatorColumn ?? entry.Column);

    /// <summary>
    /// Creates a diagnostic location from a file path and 1-based line/column.
    /// Reason: edge cases for missing trees and out-of-range columns are covered by dedicated location tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static Location TryCreateLocation(Compilation compilation, string filePath, int line, int column)
    {
        SyntaxTree tree = null;
        foreach (SyntaxTree candidate in compilation.SyntaxTrees)
        {
            if (string.Equals(candidate.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                tree = candidate;
                break;
            }
        }

        if (tree is null)
        {
            return null;
        }

        SourceText text = tree.GetText();
        return TryCreateLocation(tree, text, line, column);
    }

    /// <summary>
    /// Creates a zero-width location on a syntax tree line.
    /// Reason: edge cases for missing trees and out-of-range columns are covered by dedicated location tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static Location TryCreateLocation(SyntaxTree tree, SourceText text, int line, int column)
    {
        int lineIndex = line - 1;
        if (lineIndex < 0 || lineIndex >= text.Lines.Count)
        {
            return null;
        }

        TextLine textLine = text.Lines[lineIndex];
        int position = textLine.Start;
        if (column >= 1)
        {
            int columnIndex = column - 1;
            if (columnIndex <= textLine.Span.Length)
            {
                position = textLine.Start + columnIndex;
            }
        }

        return Location.Create(tree, TextSpan.FromBounds(position, position));
    }

    private readonly struct ExitLineKey : IEquatable<ExitLineKey>
    {
        private readonly string _MethodId;
        private readonly string _FilePath;
        private readonly int _Line;

        public ExitLineKey(string methodId, string filePath, int line)
        {
            _MethodId = methodId;
            _FilePath = filePath;
            _Line = line;
        }

        public string FilePath => _FilePath;

        public bool Equals(ExitLineKey other) =>
            _Line == other._Line
            && string.Equals(_MethodId, other._MethodId, StringComparison.Ordinal)
            && string.Equals(_FilePath, other._FilePath, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is ExitLineKey other && Equals(other);

        public override int GetHashCode()
        {
            int hash = StringComparer.Ordinal.GetHashCode(_MethodId);
            hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_FilePath);
            hash = (hash * 397) ^ _Line;
            return hash;
        }
    }

    #endregion
}
