// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleChecker.Analyzers;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Analyzers;

/// <summary>Tests for <see cref="NoVarAnalyzer"/>.</summary>
public sealed class NoVarAnalyzerTests
{
    private const string Usings = "using System;\nusing System.Collections.Generic;\n";

    [Test]
    public async Task NoVar_ExplicitType_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public void M()
                {
                    int x = 1;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<NoVarAnalyzer>(source);
    }

    [Test]
    public async Task NoVar_LocalDeclaration_ReportsCsc002()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public void M()
                {
                    {|#0:var|} x = 1;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<NoVarAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.NoVar).WithLocation(0));
    }

    [Test]
    public async Task NoVar_ForeachVar_ReportsCsc002()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public void M()
                {
                    foreach ({|#0:var|} item in new List<int>())
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<NoVarAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.NoVar).WithLocation(0));
    }

    [Test]
    public async Task NoVar_DeconstructionVar_ReportsCsc002()
    {
        const string source = Usings + """
            namespace N;
            class C
            {
                void M()
                {
                    var (a, b) = (1, 2);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<NoVarAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.NoVar).WithSpan(8, 9, 8, 12));
    }
}
