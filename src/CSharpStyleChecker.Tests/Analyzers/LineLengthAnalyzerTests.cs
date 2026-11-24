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
    public async Task LineLengthAtDefaultLimitNoDiagnostic()
    {
        string source = Usings + "namespace N;\n// " + new string('x', 120 - "// ".Length) + "\npublic class C\n{\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }

    [Test]
    public async Task LineLengthOneAboveDefaultLimitReportsCsc001()
    {
        string comment = "// " + new string('x', 121 - "// ".Length);
        string source = Usings + "namespace N;\n" + comment + "\npublic class C\n{\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(3, 1, 3, comment.Length + 1)
                .WithArguments("120", "121"));
    }

    [Test]
    public async Task LineLengthOverDefaultReportsCsc001()
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
                .WithArguments("120", "190"));
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
            "// This comment crosses the default width because the checker counts the words in a comment "
            + "even when the entire source line is a comment.";
        string source = Usings + "namespace N;\n" + comment + "\npublic class C\n{\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(3, 1, 3, comment.Length + 1)
                .WithArguments("120", comment.Length.ToString(CultureInfo.InvariantCulture)));
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
            " * This block comment crosses the default width because the checker counts its contents "
            + "even on a continuation line that starts with an asterisk.";
        string source = Usings + "namespace N;\n/*\n" + longStarLine + "\n*/\npublic class C\n{\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(4, 1, 4, longStarLine.Length + 1)
                .WithArguments("120", longStarLine.Length.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task LineLengthInvalidMaxLengthUsesDefault120()
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
                .WithArguments("120", "190"));
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
    public async Task LineLengthStarPrefixedCodeOverDefaultReportsCsc001()
    {
        // Trimmed line starts with '*'; must still report CSC001 (not treated as a doc comment).
        const string starLine =
            "        *p = 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1;";
        string source = Usings + "namespace N;\npublic unsafe class C\n{\n    public void M()\n    {\n        int value = 1;\n        int* p = &value;\n" + starLine + "\n    }\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(9, 1, 9, starLine.Length + 1)
                .WithArguments("120", starLine.Length.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task LineLengthInterpolationHoleOverDefaultReportsCsc001()
    {
        // Hole is code and must count; keep other lines short.
        string hole = string.Join("+", System.Linq.Enumerable.Repeat("1", 90));
        string line = "        string s = $\"{" + hole + "}\";";
        string source = Usings + "namespace N;\npublic class C\n{\n    public void M()\n    {\n" + line + "\n    }\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.LineLength)
                .WithSpan(7, 1, 7, line.Length + 1)
                .WithArguments("120", line.TrimEnd().Length.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task LineLengthInterpolatedLiteralPayloadMaskedNoDiagnostic()
    {
        string longLiteral = new string('a', 200);
        string source = Usings + "namespace N;\npublic class C\n{\n    string s = $\"" + longLiteral + "\";\n}\n";

        await AnalyzerVerifier.VerifyAsync<LineLengthAnalyzer>(source);
    }
}




