// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Immutable;
using System.IO;
using CSharpStyleChecker.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpStyleChecker.Analyzers;

/// <summary>Requires namespace using directives to appear only in GlobalUsings.cs; file-local type aliases are allowed.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GlobalUsingsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.GlobalUsingsOnly);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        AnalyzerGuard.RequireContext(context);
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeUsing, Microsoft.CodeAnalysis.CSharp.SyntaxKind.UsingDirective);
    }

    private static void AnalyzeUsing(SyntaxNodeAnalysisContext context)
    {
        UsingDirectiveSyntax usingDirective = (UsingDirectiveSyntax)context.Node;

        string path = context.Node.SyntaxTree.FilePath;
        if (string.IsNullOrEmpty(path) || ShouldSkipPath(path))
        {
            return;
        }

        if (usingDirective.Alias is not null)
        {
            return;
        }

        string fileName = Path.GetFileName(path);
        if (string.Equals(fileName, "GlobalUsings.cs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.GlobalUsingsOnly,
            usingDirective.GetLocation()));
    }

    private static bool ShouldSkipPath(string path)
    {
        return path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }
}
