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
                .WithSpan(5, 31, 5, 31)
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
                .WithSpan(5, 30, 5, 30)
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
                .WithSpan(5, 47, 5, 47)
                .WithArguments("M", "2", "5", "ConditionalArmCompletion, ConditionalArmCompletion"));
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
                .WithSpan(5, 23, 5, 23)
                .WithArguments("P", "2", "5", "SwitchArmCompletion, SwitchArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceSameLine_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int? M(int? left, int? right) => left ?? right;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 50, 5, 50)
                .WithArguments("M", "2", "5", "CoalesceArmCompletion, CoalesceArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceAssignmentSameLine_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private int? _Value;

                public int M() => _Value ??= 1;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(7, 30, 7, 30)
                .WithArguments("M", "2", "7", "CoalesceArmCompletion, CoalesceArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_NestedCoalesceSameLine_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int? a, int? b) => a ?? b ?? 0;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 44, 5, 44)
                .WithArguments("M", "2", "5", "CoalesceArmCompletion, CoalesceArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_TernaryMultilineReturn_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool b)
                {
                    return b
                        ? 1
                        : 2;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(8, 13, 8, 13)
                .WithArguments("M", "2", "8", "ConditionalArmCompletion, ConditionalArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceMultilineReturn_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int? M(int? left, int? right)
                {
                    return left
                        ?? right;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(8, 13, 8, 13)
                .WithArguments("M", "2", "8", "CoalesceArmCompletion, CoalesceArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceAssignmentMultilineReturn_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private int? _Value;

                public int M()
                {
                    return _Value
                        ??= 1;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(10, 13, 10, 13)
                .WithArguments("M", "2", "10", "CoalesceArmCompletion, CoalesceArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchExpressionMultilineReturn_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int x)
                {
                    return x switch
                    {
                        1 => 1,
                        2 => 2,
                    };
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(7, 18, 7, 18)
                .WithArguments("M", "2", "7", "SwitchArmCompletion, SwitchArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchArmTernaryMultilineReturn_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int x, bool a)
                {
                    return x switch
                    {
                        1 => a
                            ? 1
                            : 2,
                        _ => 0,
                    };
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(10, 17, 10, 17)
                .WithArguments("M", "2", "10", "ConditionalArmCompletion, ConditionalArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchArmCoalesceMultilineReturn_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int? M(int x, int? left, int? right)
                {
                    return x switch
                    {
                        1 => left
                            ?? right,
                        _ => 0,
                    };
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(10, 17, 10, 17)
                .WithArguments("M", "2", "10", "CoalesceArmCompletion, CoalesceArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchArmTernaryMultilineExpressionBody_ReportsCsv006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int x, bool a) =>
                    x switch
                    {
                        1 => a
                            ? 1
                            : 2,
                        _ => 0,
                    };
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(9, 17, 9, 17)
                .WithArguments("M", "2", "9", "ConditionalArmCompletion, ConditionalArmCompletion"));
    }
}
