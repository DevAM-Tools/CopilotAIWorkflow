// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleValidator.Analyzers;
using CSharpStyleValidator.Diagnostics;
using CSharpStyleValidator.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleValidator.Tests.Analyzers;

/// <summary>Tests for <see cref="PrivateNamingAnalyzer"/>.</summary>
public sealed class PrivateNamingAnalyzerTests
{
    private const string Usings = "using System;\n";

    [Test]
    public async Task PrivateNaming_ValidField_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private int _Count;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_InvalidField_ReportsCsv003()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private int {|#0:count|};
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.PrivateNaming).WithLocation(0).WithArguments("field", "count"));
    }

    [Test]
    public async Task PrivateNaming_PublicMember_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int Count { get; set; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_InvalidMethod_ReportsCsv003()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private void {|#0:method|}() { }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.PrivateNaming).WithLocation(0).WithArguments("method", "method"));
    }

    [Test]
    public async Task PrivateNaming_InvalidProperty_ReportsCsv003()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private int {|#0:Prop|} { get; set; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.PrivateNaming).WithLocation(0).WithArguments("property", "Prop"));
    }

    [Test]
    public async Task PrivateNaming_PrivateConstructor_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private C() { }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_ImplicitlyDeclared_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M() => 1;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_AutoPropertyBackingField_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int Count { get; private set; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }
}
