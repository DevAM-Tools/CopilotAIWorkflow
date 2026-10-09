// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Globalization;
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
    public async Task LineLengthUnderLimitNoDiagnostic()
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
    public async Task LineLengthOver160ReportsCsc001()
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
    public async Task LineLengthLongStringMaskedNoDiagnostic()
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
    public async Task LineLengthCommentOnlyLineReportsCsc001()
    {
        const string comment =
            "// this is a very long comment that would exceed one hundred sixty characters if it were counted as code on the line and comments now count toward the limitxxxxxx";
        string source = Usings + "namespace N;\n" + comment + "\npublic class C\n{\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(3, 1, 3, comment.Length + 1)
                .WithArguments("160", comment.Length.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task LineLengthCustomMaxLengthReportsCsc001()
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
    public async Task LineLengthWhitespaceOnlyLineNoDiagnostic()
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
    public async Task LineLengthBlockCommentLineReportsCsc001()
    {
        const string longStarLine =
            " * very long block comment line that exceeds one hundred sixty characters if counted as code and comments now count toward the configured maximum length limitxxxx";
        string source = Usings + "namespace N;\n/*\n" + longStarLine + "\n*/\npublic class C\n{\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(4, 1, 4, longStarLine.Length + 1)
                .WithArguments("160", longStarLine.Length.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task LineLengthInvalidMaxLengthUsesDefault160()
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

    [Test]
    public async Task LineLengthRawStringNextLineNoDiagnostic()
    {
        string longContent = new string('a', 200);
        string source = Usings + "namespace N;\npublic class C\n{\n    string s = \"\"\"\n" + longContent + "\n\"\"\";\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLengthVerbatimNextLineNoDiagnostic()
    {
        string longContent = new string('a', 200);
        string source = Usings + "namespace N;\npublic class C\n{\n    string s = @\"\n" + longContent + "\n\";\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLengthStarPrefixedCodeOver160ReportsCsc001()
    {
        // Trimmed line starts with '*'; must still report CSC001 (not treated as a doc comment).
        const string starLine =
            "        *p = 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1;";
        string source = Usings + "namespace N;\npublic unsafe class C\n{\n    public void M()\n    {\n        int value = 1;\n        int* p = &value;\n" + starLine + "\n    }\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(9, 1, 9, starLine.Length + 1)
                .WithArguments("160", starLine.Length.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task LineLengthInterpolationHoleOver160ReportsCsc001()
    {
        // Hole is code and must count; keep other lines short.
        string hole = string.Join("+", System.Linq.Enumerable.Repeat("1", 90));
        string line = "        string s = $\"{" + hole + "}\";";
        string source = Usings + "namespace N;\npublic class C\n{\n    public void M()\n    {\n" + line + "\n    }\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(7, 1, 7, line.Length + 1)
                .WithArguments("160", line.TrimEnd().Length.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task LineLengthInterpolatedLiteralPayloadMaskedNoDiagnostic()
    {
        string longLiteral = new string('a', 200);
        string source = Usings + "namespace N;\npublic class C\n{\n    string s = $\"" + longLiteral + "\";\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }
}




