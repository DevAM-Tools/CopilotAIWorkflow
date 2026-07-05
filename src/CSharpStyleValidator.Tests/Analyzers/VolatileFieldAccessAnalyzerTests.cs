// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using CSharpStyleValidator.Analyzers;
using CSharpStyleValidator.Diagnostics;
using CSharpStyleValidator.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleValidator.Tests.Analyzers;

/// <summary>Tests for <see cref="VolatileFieldAccessAnalyzer"/>.</summary>
public sealed class VolatileFieldAccessAnalyzerTests
{
    private const string Usings = "using System;\nusing System.Threading;\n";

    [Test]
    public async Task VolatileFieldAccess_PlainRead_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public int M()
                {
                    return _Counter;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(source);
    }

    [Test]
    public async Task VolatileFieldAccess_PlainWrite_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public void M()
                {
                    _Counter = 1;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(source);
    }

    [Test]
    public async Task VolatileFieldAccess_TernaryPlainRead_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public int M(bool flag)
                {
                    return flag ? _Counter : 0;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(source);
    }

    [Test]
    public async Task VolatileFieldAccess_VolatileRead_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public int M()
                {
                    return Volatile.Read(ref _Counter);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(source);
    }

    [Test]
    public async Task VolatileFieldAccess_InterlockedIncrement_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public void M()
                {
                    Interlocked.Increment(ref _Counter);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(source);
    }

    [Test]
    public async Task VolatileFieldAccess_Increment_ReportsCsv007()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public void M()
                {
                    {|#0:_Counter|}++;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.VolatileFieldAccess).WithLocation(0).WithArguments("_Counter"));
    }

    [Test]
    public async Task VolatileFieldAccess_CompoundAdd_ReportsCsv007()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public void M()
                {
                    {|#0:_Counter|} += 1;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.VolatileFieldAccess).WithLocation(0).WithArguments("_Counter"));
    }

    [Test]
    public async Task VolatileFieldAccess_PrefixIncrement_ReportsCsv007()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public void M()
                {
                    ++{|#0:_Counter|};
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.VolatileFieldAccess).WithLocation(0).WithArguments("_Counter"));
    }

    [Test]
    public async Task VolatileFieldAccess_CompareExchange_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private volatile int _Counter;

                public int M()
                {
                    return Interlocked.CompareExchange(ref _Counter, 1, 0);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(source);
    }

    [Test]
    public async Task VolatileFieldAccess_NonVolatileIncrement_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private int _Counter;

                public void M()
                {
                    _Counter++;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<VolatileFieldAccessAnalyzer>(source);
    }
}
