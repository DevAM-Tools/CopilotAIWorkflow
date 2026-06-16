// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Immutable;
using CSharpStyleValidator.Diagnostics;
using CSharpStyleValidator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CSharpStyleValidator.Analyzers;

/// <summary>Reports lines exceeding the configured maximum visible length.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LineLengthAnalyzer : DiagnosticAnalyzer
{
    private const string MaxLineLengthKey = "build_property.csv_max_line_length";

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

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
        int maxLength = 160;
        if (options.TryGetValue(MaxLineLengthKey, out string value)
            && int.TryParse(value, out int parsed)
            && parsed > 0)
        {
            maxLength = parsed;
        }

        SourceText text = context.Tree.GetText(context.CancellationToken);
        bool inBlock = false;

        for (int lineIndex = 0; lineIndex < text.Lines.Count; lineIndex++)
        {
            TextLine textLine = text.Lines[lineIndex];
            string raw = textLine.ToString();
            string visible = raw.TrimEnd();
            if (visible.Length == 0)
            {
                (string _, inBlock) = LineMasking.MaskLine(raw, inBlock);
                continue;
            }

            string trimmedStart = visible.TrimStart();
            if (trimmedStart.StartsWith("//", System.StringComparison.Ordinal)
                || trimmedStart.StartsWith("*", System.StringComparison.Ordinal))
            {
                (string _, inBlock) = LineMasking.MaskLine(raw, inBlock);
                continue;
            }

            (string masked, inBlock) = LineMasking.MaskLine(raw, inBlock);
            int maskedVisibleLength = masked.TrimEnd().Length;
            if (maskedVisibleLength > maxLength)
            {
                Location location = Location.Create(context.Tree, textLine.Span);
                Diagnostic diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.LineLength,
                    location,
                    maxLength,
                    maskedVisibleLength);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}

