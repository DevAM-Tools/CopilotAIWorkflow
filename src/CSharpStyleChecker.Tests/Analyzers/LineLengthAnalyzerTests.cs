// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleChecker.Analyzers;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Analyzers;

/// <summary>Tests for <see cref="LineLengthAnalyzer"/>.</summary>
public sealed class LineLengthAnalyzerTests
{
    private const string Usings = "using System;\n";

    [Test]
    public async Task LineLength_UnderLimit_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M() => 1;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLength_Over160_ReportsCsc001()
    {
        string longName = new string('x', 170);
        string source = Usings + $$"""
            namespace N;
            public class C
            {
                public int {{longName}} = 1;
            }
            """;

        int lineLength = 4 + "public int ".Length + 170 + " = 1;".Length;
        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(5, 1, 5, lineLength + 1)
                .WithArguments("160", "190"));
    }

    [Test]
    public async Task LineLength_LongStringMasked_NoDiagnostic()
    {
        string longLiteral = new string('a', 200);
        string source = Usings + $$"""
            namespace N;
            public class C
            {
                string s = "{{longLiteral}}";
            }
            """;

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLength_CommentOnlyLine_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            // this is a very long comment that would exceed one hundred sixty characters if it were counted as code on the line but it should be ignored entirely by the analyzer
            public class C
            {
            }
            """;

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLength_CustomMaxLength_ReportsCsc001()
    {
        const string source = """
            class C { int value = 1; }
            """;

        const string editorConfig = """
            [*.cs]
            build_property.csc_max_line_length = 10
            """;

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            null,
            new[] { ("/.editorconfig", editorConfig) },
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(1, 1, 1, 27)
                .WithArguments("10", "26"));
    }

    [Test]
    public async Task LineLength_WhitespaceOnlyLine_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                
                public int M() => 1;
            }
            """;

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLength_BlockCommentLine_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            /*
             * very long block comment line that exceeds one hundred sixty characters if counted as code but should be ignored because it starts with an asterisk after trimming
             */
            public class C
            {
            }
            """;

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLength_InvalidMaxLength_UsesDefault160()
    {
        string longName = new string('x', 170);
        string source = Usings + $$"""
            namespace N;
            public class C
            {
                public int {{longName}} = 1;
            }
            """;

        const string editorConfig = """
            [*.cs]
            build_property.csc_max_line_length = not-a-number
            """;

        int lineLength = 4 + "public int ".Length + 170 + " = 1;".Length;
        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            null,
            new[] { ("/.editorconfig", editorConfig) },
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(5, 1, 5, lineLength + 1)
                .WithArguments("160", "190"));
    }
}
