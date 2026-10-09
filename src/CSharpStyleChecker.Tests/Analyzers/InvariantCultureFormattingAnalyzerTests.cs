// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleChecker.Analyzers;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Analyzers;

/// <summary>Tests for <see cref="InvariantCultureFormattingAnalyzer"/>.</summary>
public sealed class InvariantCultureFormattingAnalyzerTests
{
    private const string Usings =
        "using System;\nusing System.Globalization;\nusing System.Text;\nusing static System.FormattableString;\n";

    [Test]
    public async Task Interpolation_Bare_ReportsCsc009()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return {|#0:$"value={value}"|};
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(
                    "Use Invariant($\"…\") (or string.Create / StringBuilder.Append with CultureInfo.InvariantCulture) "
                    + "instead of a bare interpolated string"));
    }

    [Test]
    public async Task Interpolation_Invariant_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return Invariant($"value={value}");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task Interpolation_FormattableStringInvariant_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return FormattableString.Invariant($"value={value}");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task Interpolation_StringCreateInvariant_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return string.Create(CultureInfo.InvariantCulture, $"value={value}");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task Interpolation_StringBuilderAppendInvariant_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    StringBuilder builder = new();
                    builder.Append(CultureInfo.InvariantCulture, $"value={value}");
                    builder.AppendLine(CultureInfo.InvariantCulture, $"count={value}");
                    return builder.ToString();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task Interpolation_Verbatim_ReportsCsc009()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return {|#0:$@"value={value}"|};
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(
                    "Use Invariant($\"…\") (or string.Create / StringBuilder.Append with CultureInfo.InvariantCulture) "
                    + "instead of a bare interpolated string"));
    }

    [Test]
    public async Task Interpolation_NestedBareInsideInvariant_ReportsInner()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return Invariant($"outer={{|#0:$"inner={value}"|}}");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(
                    "Use Invariant($\"…\") (or string.Create / StringBuilder.Append with CultureInfo.InvariantCulture) "
                    + "instead of a bare interpolated string"));
    }

    [Test]
    public async Task ToString_IntWithoutCulture_ReportsCsc009()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return value.{|#0:ToString|}();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments("Pass CultureInfo.InvariantCulture to ToString"));
    }

    [Test]
    public async Task ToString_IntWithFormatWithoutCulture_ReportsCsc009()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return value.{|#0:ToString|}("D4");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments("Pass CultureInfo.InvariantCulture to ToString"));
    }

    [Test]
    public async Task ToString_IntWithInvariantCulture_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return value.ToString(CultureInfo.InvariantCulture);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task ToString_IntWithFormatAndInvariantCulture_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(double value)
                {
                    return value.ToString("P1", CultureInfo.InvariantCulture);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task ToString_CurrentCulture_ReportsCsc009()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return value.{|#0:ToString|}(CultureInfo.CurrentCulture);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments("Pass CultureInfo.InvariantCulture to ToString"));
    }

    [Test]
    public async Task ToString_Bool_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(bool value)
                {
                    return value.ToString();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task ToString_Enum_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public enum Kind { A, B }
            public class C
            {
                public string M(Kind value)
                {
                    return value.ToString();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task ToString_StringBuilder_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M()
                {
                    StringBuilder builder = new();
                    builder.Append('x');
                    return builder.ToString();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }
}
