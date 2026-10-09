// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using System.Globalization;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CSharpStyleChecker.Analyzers;

/// <summary>
/// Reports lines exceeding the configured maximum visible length.
/// String and character literal content is masked; comments and holes in <c>$"..."</c> / <c>@$"..."</c> / <c>$@"..."</c> count toward the limit.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LineLengthAnalyzer : DiagnosticAnalyzer
{
    #region Fields

    private const string MaxLineLengthKey = "build_property.csc_max_line_length";

    /// <summary>Default matches the repository line-length limit; editorconfig may override.</summary>
    private const int DefaultMaxLineLength = 160;

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.LineLength);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    #endregion

    #region Private helpers

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
        int maxLength = DefaultMaxLineLength;
        if (options.TryGetValue(MaxLineLengthKey, out string value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            && parsed > 0)
        {
            maxLength = parsed;
        }

        SourceText text = context.Tree.GetText(context.CancellationToken);
        LineMaskState state = default;

        for (int lineIndex = 0; lineIndex < text.Lines.Count; lineIndex++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            TextLine textLine = text.Lines[lineIndex];

            // Always measure so open string / block-comment state advances on blank lines too.
            int visible = LineMasking.MeasureVisibleLength(text, textLine, ref state);
            if (visible > maxLength)
            {
                Location location = Location.Create(context.Tree, textLine.Span);
                Diagnostic diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.LineLength,
                    location,
                    maxLength,
                    visible);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    #endregion
}
