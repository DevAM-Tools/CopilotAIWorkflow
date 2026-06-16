// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleValidator.Analyzers;
using CSharpStyleValidator.Diagnostics;
using CSharpStyleValidator.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleValidator.Tests.Analyzers;

/// <summary>Tests for <see cref="MultipleExitsPerLineAnalyzer"/>.</summary>
public sealed class MultipleExitsPerLineAnalyzerTests
{
    private const string Usings = "using System;\n";

    [Test]
    public async Task MultipleExitsPerLine_SameLineTwoReturns_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool a, bool b) { if (a) return 1; if (b) return 2; return 0; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 43, 5, 43)
                .WithArguments("M", "4", "5", "Return, Return, Return, ImplicitEnd"));
    }

    [Test]
    public async Task MultipleExitsPerLine_SeparateLines_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool a, bool b)
                {
                    if (a)
                    {
                        return 1;
                    }

                    if (b)
                    {
                        return 2;
                    }

                    return 0;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_TernarySameLine_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool b) => b ? 1 : 2;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 33, 5, 33)
                .WithArguments("M", "2", "5", "ConditionalArmCompletion, ConditionalArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchExpressionSameLine_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int x) => x switch { 1 => 1, 2 => 2 };
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 44, 5, 44)
                .WithArguments("M", "2", "5", "SwitchArmCompletion, SwitchArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_NestedTernarySameLine_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool a, bool b) => a ? 1 : b ? 2 : 3;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 41, 5, 41)
                .WithArguments("M", "3", "5", "ConditionalArmCompletion, ConditionalArmCompletion, ConditionalArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_ExpressionBodiedPropertySameLine_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int P => 1 switch { 1 => 1, _ => 0 };
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 37, 5, 37)
                .WithArguments("P", "2", "5", "SwitchArmCompletion, SwitchArmCompletion"));
    }
}
