// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleChecker.Analyzers;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Analyzers;

/// <summary>Tests for <see cref="GlobalUsingsAnalyzer"/>.</summary>
public sealed class GlobalUsingsAnalyzerTests
{
    [Test]
    public async Task GlobalUsings_InNonGlobalFile_ReportsCsc005()
    {
        const string source = """
            {|#0:using System;|}
            namespace N;
            public class C
            {
            }
            """;

        await AnalyzerVerifier.VerifyAsync<GlobalUsingsAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.GlobalUsingsOnly).WithLocation(0));
    }

    [Test]
    public async Task GlobalUsings_InGlobalUsingsFile_NoDiagnostic()
    {
        const string source = """
            namespace N;
            public class C
            {
            }
            """;

        await AnalyzerVerifier.VerifyAsync<GlobalUsingsAnalyzer>(
            source,
            new[] { ("GlobalUsings.cs", "using System;") });
    }

    [Test]
    public async Task TypeAlias_InNonGlobalFile_NoDiagnostic()
    {
        const string source = """
            using ListInt = System.Collections.Generic.List<int>;
            namespace N;
            public class C
            {
                public int M()
                {
                    ListInt values = new ListInt { 1 };
                    return values.Count;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<GlobalUsingsAnalyzer>(source);
    }

    [Test]
    public async Task NamespaceUsing_InNonGlobalFile_StillReportsCsc005_WhenTypeAliasPresent()
    {
        const string source = """
            {|#0:using System;|}
            using ListInt = System.Collections.Generic.List<int>;
            namespace N;
            public class C
            {
            }
            """;

        await AnalyzerVerifier.VerifyAsync<GlobalUsingsAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.GlobalUsingsOnly).WithLocation(0));
    }

    [Test]
    public async Task GlobalUsings_InObjGeneratedPath_NoDiagnostic()
    {
        const string source = """
            namespace N;
            public class C
            {
            }
            """;

        await AnalyzerVerifier.VerifyAsync<GlobalUsingsAnalyzer>(
            source,
            new[]
            {
                (@"C:\proj\obj\Debug\Generated.cs", "using System; namespace N; public class G { }"),
                (@"C:\proj\bin\Release\Generated.cs", "using System; namespace N; public class B { }"),
                (@"C:\proj\Main.g.cs", "using System; namespace N; public class H { }"),
            });
    }
}
