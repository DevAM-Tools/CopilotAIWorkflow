// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleChecker.Analyzers;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Analyzers;

/// <summary>Tests for <see cref="MultipleExitsPerLineAnalyzer"/>.</summary>
public sealed class MultipleExitsPerLineAnalyzerTests
{
    private const string Usings = "using System;\n";

    [Test]
    public async Task MultipleExitsPerLine_SameLineTwoReturns_ReportsCsc006()
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
    public async Task MultipleExitsPerLine_TernarySameLine_ReportsCsc006()
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
    public async Task MultipleExitsPerLine_SwitchExpressionSameLine_ReportsCsc006()
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
    public async Task MultipleExitsPerLine_NestedTernarySameLine_ReportsCsc006()
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
                .WithSpan(5, 39, 5, 39)
                .WithArguments("M", "3", "5", "ConditionalArmCompletion, ConditionalArmCompletion, ConditionalArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_ExpressionBodiedPropertySameLine_ReportsCsc006()
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
    public async Task MultipleExitsPerLine_CoalesceSameLine_ReportsCsc006()
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
    public async Task MultipleExitsPerLine_CoalesceAssignmentSameLine_ReportsCsc006()
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
    public async Task MultipleExitsPerLine_NestedCoalesceSameLine_ReportsCsc006()
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
                .WithSpan(5, 39, 5, 39)
                .WithArguments("M", "3", "5", "CoalesceArmCompletion, CoalesceArmCompletion, CoalesceArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_TernaryMultilineReturn_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceMultilineReturn_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceAssignmentMultilineReturn_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchExpressionMultilineReturn_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchArmTernaryMultilineReturn_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchArmCoalesceMultilineReturn_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchArmTernaryMultilineExpressionBody_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_TernaryPartialMultilineArmsSameLine_ReportsCsc006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool b)
                {
                    return b
                        ? 1 : 2;
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
    public async Task MultipleExitsPerLine_SwitchExpressionMultilineTwoArmsSameLine_ReportsCsc006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int x)
                {
                    return x switch
                    {
                        1 => 1, 2 => 2,
                        _ => 0,
                    };
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(9, 18, 9, 18)
                .WithArguments("M", "2", "9", "SwitchArmCompletion, SwitchArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_TernaryMultilineExpressionBody_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool b) =>
                    b
                        ? 1
                        : 2;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_SwitchExpressionMultilineExpressionBody_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int x) =>
                    x switch
                    {
                        1 => 1,
                        2 => 2,
                    };
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceMultilineExpressionBody_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int? M(int? left, int? right) =>
                    left
                        ?? right;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_SameLineReturnAndThrow_ReportsCsc006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool a)
                {
                    if (a) return 1; throw new InvalidOperationException();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(7, 16, 7, 16)
                .WithArguments("M", "2", "7", "Return, Throw"));
    }

    [Test]
    public async Task MultipleExitsPerLine_CoalesceThrowSameLine_ReportsCsc006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(int? value) => value ?? throw new InvalidOperationException();
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 39, 5, 39)
                .WithArguments("M", "2", "5", "CoalesceArmCompletion, ThrowExpression"));
    }

    [Test]
    public async Task MultipleExitsPerLine_TernaryThrowSameLine_ReportsCsc006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool b) => b ? throw new InvalidOperationException() : 2;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 31, 5, 31)
                .WithArguments("M", "2", "5", "ThrowExpression, ConditionalArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_LocalFunctionSameLineTwoReturns_ReportsCsc006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool a, bool b)
                {
                    int Local(bool x, bool y) { if (x) return 1; if (y) return 2; return 0; }
                    return Local(a, b);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(7, 44, 7, 44)
                .WithArguments("Local", "3", "7", "Return, Return, Return"));
    }

    [Test]
    public async Task MultipleExitsPerLine_ExcludeFromCodeCoverageSameLine_NoDiagnostic()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            public class C
            {
                [ExcludeFromCodeCoverage]
                public int M(bool a, bool b) { if (a) return 1; if (b) return 2; return 0; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_SingleReturn_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M() => 1;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_DifferentCallablesSamePattern_ReportsPerCallable()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool b) => b ? 1 : 2;

                public int N(bool b) => b ? 3 : 4;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(5, 31, 5, 31)
                .WithArguments("M", "2", "5", "ConditionalArmCompletion, ConditionalArmCompletion"),
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(7, 31, 7, 31)
                .WithArguments("N", "2", "7", "ConditionalArmCompletion, ConditionalArmCompletion"));
    }
}
