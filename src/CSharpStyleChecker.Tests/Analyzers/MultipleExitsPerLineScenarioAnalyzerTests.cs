// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleChecker.Analyzers;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Analyzers;

/// <summary>Scenario tests for <see cref="MultipleExitsPerLineAnalyzer"/> (async, yield, exclusion).</summary>
public sealed class MultipleExitsPerLineScenarioAnalyzerTests
{
    private const string TaskUsings = "using System;\nusing System.Threading.Tasks;\n";
    private const string IteratorUsings = "using System;\nusing System.Collections.Generic;\n";

    [Test]
    public async Task MultipleExitsPerLine_AsyncSameLineTwoReturns_ReportsCsc006()
    {
        const string source = TaskUsings + """
            namespace N;
            public class C
            {
                public async Task<int> M(bool a, bool b) { if (a) return 1; if (b) return 2; return 0; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(6, 55, 6, 55)
                .WithArguments("M", "4", "6", "Return, Return, Return, ImplicitEnd"));
    }

    [Test]
    public async Task MultipleExitsPerLine_AsyncSeparateLines_NoDiagnostic()
    {
        const string source = TaskUsings + """
            namespace N;
            public class C
            {
                public async Task<int> M(bool a, bool b)
                {
                    await Task.Delay(1);
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
    public async Task MultipleExitsPerLine_AsyncTernarySameLine_ReportsCsc006()
    {
        const string source = TaskUsings + """
            namespace N;
            public class C
            {
                public async Task<int> M(bool b) => b ? 1 : 2;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(6, 43, 6, 43)
                .WithArguments("M", "2", "6", "ConditionalArmCompletion, ConditionalArmCompletion"));
    }

    [Test]
    public async Task MultipleExitsPerLine_AsyncTernaryMultiline_NoDiagnostic()
    {
        const string source = TaskUsings + """
            namespace N;
            public class C
            {
                public async Task<int> M(bool b) =>
                    b
                        ? 1
                        : 2;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_AsyncSameLineReturnAndThrow_ReportsCsc006()
    {
        const string source = TaskUsings + """
            namespace N;
            public class C
            {
                public async Task M(bool a)
                {
                    if (a) return; throw new InvalidOperationException();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(8, 16, 8, 16)
                .WithArguments("M", "2", "8", "Return, Throw"));
    }

    [Test]
    public async Task MultipleExitsPerLine_IteratorYieldBreakSameLine_ReportsCsc006()
    {
        const string source = IteratorUsings + """
            namespace N;
            public class C
            {
                public IEnumerable<int> M(bool a, bool b) { if (a) yield break; if (b) yield break; yield break; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(6, 56, 6, 56)
                .WithArguments("M", "4", "6", "YieldBreak, YieldBreak, YieldBreak, ImplicitEnd"));
    }

    [Test]
    public async Task MultipleExitsPerLine_IteratorYieldBreakSeparateLines_NoDiagnostic()
    {
        const string source = IteratorUsings + """
            namespace N;
            public class C
            {
                public IEnumerable<int> M(bool a, bool b)
                {
                    if (a)
                    {
                        yield break;
                    }

                    if (b)
                    {
                        yield break;
                    }

                    yield break;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_AsyncIteratorYieldBreakSameLine_ReportsCsc006()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            namespace N;
            public class C
            {
                public async IAsyncEnumerable<int> M(bool a, bool b) { if (a) yield break; yield break; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(7, 67, 7, 67)
                .WithArguments("M", "3", "7", "YieldBreak, YieldBreak, ImplicitEnd"));
    }

    [Test]
    public async Task MultipleExitsPerLine_GotoSameLineTwoReturns_ReportsCsc006()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M(bool a)
                {
                    if (a) goto End; return 1; End: return 2;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.MultipleExitsPerLine)
                .WithSpan(7, 26, 7, 26)
                .WithArguments("M", "2", "7", "Return, Return"));
    }

    [Test]
    public async Task MultipleExitsPerLine_GotoOnlyPath_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public void M(bool a)
                {
                    if (a)
                    {
                        goto End;
                    }

                End: ;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_ExcludeFromCodeCoverageTypeLevel_NoDiagnostic()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            [ExcludeFromCodeCoverage]
            public class C
            {
                public int M(bool a, bool b) { if (a) return 1; if (b) return 2; return 0; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_ExcludeFromCodeCoverageAsyncMethod_NoDiagnostic()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using System.Threading.Tasks;
            namespace N;
            public class C
            {
                [ExcludeFromCodeCoverage]
                public async Task<int> M(bool a, bool b) { if (a) return 1; if (b) return 2; return 0; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_ExcludeFromCodeCoverageLocalFunction_NoDiagnostic()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            public class C
            {
                public int M(bool a, bool b)
                {
                    [ExcludeFromCodeCoverage]
                    int Local(bool x, bool y) { if (x) return 1; if (y) return 2; return 0; }
                    return Local(a, b);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    [Test]
    public async Task MultipleExitsPerLine_IteratorYieldReturnOnlySameLine_NoDiagnostic()
    {
        const string source = IteratorUsings + """
            namespace N;
            public class C
            {
                public IEnumerable<int> M() { yield return 1; yield return 2; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<MultipleExitsPerLineAnalyzer>(source);
    }

    private const string Usings = "using System;\n";
}
