// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Reflection;
using System.Threading.Tasks;
using CSharpStyleChecker.Analyzers;
using CSharpStyleChecker.Diagnostics;
using CSharpStyleChecker.Tests.Helpers;
using InvariantStringHelper;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Analyzers;

/// <summary>Tests for <see cref="InvariantCultureFormattingAnalyzer"/>.</summary>
public sealed class InvariantCultureFormattingAnalyzerTests
{
    private const string Usings =
        "using System;\nusing System.Globalization;\nusing System.Text;\nusing static System.FormattableString;\n";

    // Targets with string.Create(IFormatProvider, handler), package not referenced: bare interpolation points at the package.
    private const string BareMessage =
        "Add the InvariantStringHelper package and use Invariant($\"…\") (StringBuilder.Append with CultureInfo.InvariantCulture "
        + "for builders) instead of a bare interpolated string";

    // Targets or language versions without interpolated string handlers (netstandard2.0, C# 9): FormattableString.Invariant is the only option.
    private const string LegacyBareMessage =
        "Use FormattableString.Invariant($\"…\") (for example Invariant($\"…\") with 'using static System.FormattableString;') "
        + "instead of a bare interpolated string; this target has no allocation-free alternative";

    // Targets with the overload, package not referenced: the FormattableString wrapper is the defect.
    private const string WrapperMessage =
        "FormattableString.Invariant allocates a FormattableString and boxes its arguments; "
        + "add the InvariantStringHelper package and use Invariant($\"…\") instead";

    // The InvariantStringHelper package is referenced (stub below): both bare interpolation and the FormattableString wrapper name its helper.
    private const string PackageBareMessage =
        "Use Invariant($\"…\") from the InvariantStringHelper package (StringBuilder.Append with CultureInfo.InvariantCulture "
        + "for builders) instead of a bare interpolated string";

    private const string PackageWrapperMessage =
        "FormattableString.Invariant allocates a FormattableString and boxes its arguments; "
        + "use Invariant($\"…\") from the InvariantStringHelper package instead";

    // Both helpers are named Invariant; importing both statics would make the call ambiguous, so package tests import only the package.
    private const string PackageUsings =
        "using System;\nusing System.Globalization;\nusing System.Text;\nusing static InvariantStringHelper.InvariantText;\n";

    // Same namespace, type, and member names as the real package, so the analyzer's metadata-name lookup resolves it in a compilation
    // without the real assembly (the real assembly targets a newer framework than the verifier's reference set).
    private const string PackageStub = """
        namespace InvariantStringHelper
        {
            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public ref struct InvariantInterpolatedStringHandler
            {
                private System.Runtime.CompilerServices.DefaultInterpolatedStringHandler _inner;

                public InvariantInterpolatedStringHandler(int literalLength, int formattedCount)
                {
                    _inner = new(literalLength, formattedCount, System.Globalization.CultureInfo.InvariantCulture);
                }

                public void AppendLiteral(string value) { _inner.AppendLiteral(value); }

                public void AppendFormatted<T>(T value) { _inner.AppendFormatted(value); }

                public string ToStringAndClear() { return _inner.ToStringAndClear(); }
            }

            public static class InvariantText
            {
                public static string Invariant(ref InvariantInterpolatedStringHandler handler) { return handler.ToStringAndClear(); }
            }
        }

        namespace Other
        {
            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public ref struct OtherHandler
            {
                private System.Runtime.CompilerServices.DefaultInterpolatedStringHandler _inner;

                public OtherHandler(int literalLength, int formattedCount)
                {
                    _inner = new(literalLength, formattedCount);
                }

                public void AppendLiteral(string value) { _inner.AppendLiteral(value); }

                public void AppendFormatted<T>(T value) { _inner.AppendFormatted(value); }

                public string ToStringAndClear() { return _inner.ToStringAndClear(); }
            }

            public static class OtherText
            {
                public static string Invariant(ref OtherHandler handler) { return handler.ToStringAndClear(); }
            }
        }
        """;

    private static readonly (string FileName, string Content)[] _PackageSources = [("InvariantStringHelper.cs", PackageStub)];

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
                .WithArguments(BareMessage));
    }

    [Test]
    public async Task Interpolation_BareWithoutStringCreate_ReportsLegacyCsc009()
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

        await AnalyzerVerifier.VerifyOnFrameworkAsync<InvariantCultureFormattingAnalyzer>(
            source,
            ReferenceAssemblies.NetStandard.NetStandard20,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(LegacyBareMessage));
    }

    [Test]
    public async Task Interpolation_Invariant_ReportsCsc009()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return Invariant({|#0:$"value={value}"|});
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(WrapperMessage));
    }

    [Test]
    public async Task Interpolation_FormattableStringInvariant_ReportsCsc009()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return FormattableString.Invariant({|#0:$"value={value}"|});
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(WrapperMessage));
    }

    [Test]
    public async Task Interpolation_InvariantWithoutStringCreate_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyOnFrameworkAsync<InvariantCultureFormattingAnalyzer>(
            source,
            ReferenceAssemblies.NetStandard.NetStandard20);
    }

    [Test]
    public async Task Interpolation_FormattableStringInvariantWithoutStringCreate_NoDiagnostic()
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

        await AnalyzerVerifier.VerifyOnFrameworkAsync<InvariantCultureFormattingAnalyzer>(
            source,
            ReferenceAssemblies.NetStandard.NetStandard20);
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
                .WithArguments(BareMessage));
    }

    [Test]
    public async Task Interpolation_NestedBareInsideInvariantWithoutStringCreate_ReportsInner()
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

        await AnalyzerVerifier.VerifyOnFrameworkAsync<InvariantCultureFormattingAnalyzer>(
            source,
            ReferenceAssemblies.NetStandard.NetStandard20,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(LegacyBareMessage));
    }

    [Test]
    public async Task Interpolation_NestedBareInsideStringCreate_ReportsInner()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return string.Create(CultureInfo.InvariantCulture, $"outer={{|#0:$"inner={value}"|}}");
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(BareMessage));
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

    [Test]
    public async Task Interpolation_PackageInvariant_NoDiagnostic()
    {
        const string source = PackageUsings + """
            namespace N;
            public class C
            {
                public string Accepted(int value)
                {
                    return Invariant($"value={value}");
                }

                public string Rejected(int value)
                {
                    return {|#0:$"value={value}"|};
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            _PackageSources,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(PackageBareMessage));
    }

    [Test]
    public async Task Interpolation_PackageInvariantWrittenInOtherForms_NoDiagnostic()
    {
        // Every spelling of the call that the compiler accepts must pass: the analyzer keys on the bound method, not on the syntax.
        const string source = PackageUsings + """"
            namespace N;
            public class C
            {
                public string FullyQualified(int value)
                {
                    return InvariantStringHelper.InvariantText.Invariant($"value={value}");
                }

                public string NamedArgument(int value)
                {
                    return Invariant(handler: $"value={value}");
                }

                public string RawLiteral(int value)
                {
                    return Invariant($"""value={value}""");
                }

                public string VerbatimLiteral(int value)
                {
                    return Invariant($@"value={value}");
                }

                public string Parenthesized(int value)
                {
                    return Invariant(($"value={value}"));
                }

                public string InsideLambda(int value)
                {
                    System.Func<int, string> format = static item => Invariant($"item={item}");
                    return format(value);
                }

                public string AsExpressionBody(int value) => Invariant($"value={value}");

                public string ParenthesizedStringCreate(int value)
                {
                    return string.Create(CultureInfo.InvariantCulture, ($"value={value}"));
                }

                public string ParenthesizedAppend(int value)
                {
                    StringBuilder builder = new();
                    builder.Append(CultureInfo.InvariantCulture, (($"value={value}")));
                    return builder.ToString();
                }
            }
            """";

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source, _PackageSources);
    }

    [Test]
    public async Task Interpolation_BareWithPackage_NamesHelper()
    {
        const string source = PackageUsings + """
            namespace N;
            public class C
            {
                public string Plain(int value)
                {
                    return {|#0:$"value={value}"|};
                }

                public string Verbatim(int value)
                {
                    return {|#1:$@"value={value}"|};
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            _PackageSources,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(PackageBareMessage),
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(1)
                .WithArguments(PackageBareMessage));
    }

    [Test]
    public async Task Interpolation_FormattableStringWithPackage_NamesHelper()
    {
        const string source = PackageUsings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return FormattableString.Invariant({|#0:$"value={value}"|});
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            _PackageSources,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(PackageWrapperMessage));
    }

    [Test]
    public async Task Interpolation_StaticInvariantWithPackage_NamesHelper()
    {
        // Usings imports System.FormattableString statically; the package stub is in the compilation but not imported.
        const string source = Usings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return Invariant({|#0:$"value={value}"|});
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            _PackageSources,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(PackageWrapperMessage));
    }

    [Test]
    public async Task Interpolation_LookalikeInvariant_ReportsCsc009()
    {
        const string source = PackageUsings + """
            namespace N;
            public class C
            {
                public string M(int value)
                {
                    return Other.OtherText.Invariant({|#0:$"value={value}"|});
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(
            source,
            _PackageSources,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(PackageBareMessage));
    }

    [Test]
    public async Task Interpolation_NestedBareInsidePackageInvariant_ReportsInner()
    {
        const string source = PackageUsings + """
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
            _PackageSources,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(PackageBareMessage));
    }

    [Test]
    public async Task Interpolation_PackageTypesWithoutMatchingMethod_UsesPlainMessage()
    {
        // InvariantText has only a by-value overload and a two-parameter overload: neither is the helper, so the package is not recognized.
        const string stub = """
            namespace InvariantStringHelper
            {
                [System.Runtime.CompilerServices.InterpolatedStringHandler]
                public ref struct InvariantInterpolatedStringHandler
                {
                    private System.Runtime.CompilerServices.DefaultInterpolatedStringHandler _inner;

                    public InvariantInterpolatedStringHandler(int literalLength, int formattedCount)
                    {
                        _inner = new(literalLength, formattedCount);
                    }

                    public void AppendLiteral(string value) { _inner.AppendLiteral(value); }

                    public void AppendFormatted<T>(T value) { _inner.AppendFormatted(value); }

                    public string ToStringAndClear() { return _inner.ToStringAndClear(); }
                }

                public static class InvariantText
                {
                    public static string Invariant(InvariantInterpolatedStringHandler handler) { return handler.ToStringAndClear(); }

                    public static string Invariant(ref InvariantInterpolatedStringHandler handler, int extra) { return handler.ToStringAndClear(); }
                }
            }
            """;
        const string source = PackageUsings + """
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
            [("InvariantStringHelper.cs", stub)],
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(BareMessage));
    }

    [Test]
    public async Task Interpolation_PackageTextWithoutHandlerType_UsesPlainMessage()
    {
        // The helper class exists but the handler type does not: the package is not recognized.
        const string stub = """
            namespace InvariantStringHelper
            {
                public static class InvariantText
                {
                    public static string Invariant(string value) { return value; }
                }
            }
            """;
        const string source = PackageUsings + """
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
            [("InvariantStringHelper.cs", stub)],
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(BareMessage));
    }

    [Test]
    public async Task ToString_CustomFormatProviderOverloadCalledWithoutProvider_ReportsCsc009()
    {
        // Money is not IFormattable; its ToString(IFormatProvider) overload alone makes the type culture-sensitive.
        const string source = Usings + """
            namespace N;
            public sealed class Money
            {
                public string ToString(IFormatProvider provider) { return string.Empty; }
            }

            public class C
            {
                public string M(Money money)
                {
                    return money.{|#0:ToString|}();
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
    public async Task ToString_CustomFormatProviderOverloadWithInvariantCulture_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public sealed class Money
            {
                public string ToString(IFormatProvider provider) { return string.Empty; }
            }

            public class C
            {
                public string M(Money money)
                {
                    return money.ToString(CultureInfo.InvariantCulture);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task ToString_CustomCultureInfoOverloadWithInvariantCulture_NoDiagnostic()
    {
        // The parameter type is CultureInfo itself, not IFormatProvider.
        const string source = Usings + """
            namespace N;
            public sealed class Money
            {
                public string ToString(CultureInfo culture) { return string.Empty; }
            }

            public class C
            {
                public string M(Money money)
                {
                    return money.ToString(CultureInfo.InvariantCulture);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<InvariantCultureFormattingAnalyzer>(source);
    }

    [Test]
    public async Task ToString_OptionalProviderOmitted_ReportsCsc009()
    {
        // The call binds to the derived overload with a defaulted provider, so the argument list is shorter than the parameter list.
        const string source = Usings + """
            namespace N;
            public sealed class Money
            {
                public string ToString(IFormatProvider provider = null) { return string.Empty; }
            }

            public class C
            {
                public string M(Money money)
                {
                    return money.{|#0:ToString|}();
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
    public async Task Interpolation_BareOnNet6WithCSharp9_PointsAtFormattableString()
    {
        // The framework has string.Create with a handler, but C# 9 cannot build handlers: the package and string.Create are unusable.
        const string source = Usings + """
            namespace N
            {
                public class C
                {
                    public string M(int value)
                    {
                        return {|#0:$"value={value}"|};
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyOnLanguageVersionAsync<InvariantCultureFormattingAnalyzer>(
            source,
            LanguageVersion.CSharp9,
            new DiagnosticResult(DiagnosticDescriptors.InvariantCultureFormatting)
                .WithLocation(0)
                .WithArguments(LegacyBareMessage));
    }

    [Test]
    public async Task Interpolation_FormattableStringOnNet6WithCSharp9_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N
            {
                public class C
                {
                    public string M(int value)
                    {
                        return Invariant($"value={value}");
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyOnLanguageVersionAsync<InvariantCultureFormattingAnalyzer>(source, LanguageVersion.CSharp9);
    }

    [Test]
    public async Task Interpolation_HandlerTypeWithoutStringCreateOverload_KeepsFormattableStringInvariant()
    {
        // netstandard2.0 has no DefaultInterpolatedStringHandler; a source-defined type of that name must not make string.Create look available.
        const string source = Usings + """
            namespace System.Runtime.CompilerServices
            {
                public ref struct DefaultInterpolatedStringHandler
                {
                }
            }

            namespace N
            {
                public class C
                {
                    public string M(int value)
                    {
                        return Invariant($"value={value}");
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyOnFrameworkAsync<InvariantCultureFormattingAnalyzer>(
            source,
            ReferenceAssemblies.NetStandard.NetStandard20);
    }

    [Test]
    public async Task Package_NamesMatchAnalyzerLookup()
    {
        Type text = typeof(InvariantText);
        MethodInfo method = text.GetMethod(InvariantCultureFormattingAnalyzer.InvariantMethodName)!; // Invariant: the method exists, else the next line throws.
        ParameterInfo parameter = method.GetParameters().Single();

        await Assert.That(text.FullName).IsEqualTo(InvariantCultureFormattingAnalyzer.InvariantTextMetadataName);
        await Assert.That(parameter.ParameterType.IsByRef).IsTrue();
        await Assert.That(parameter.ParameterType.GetElementType()!.FullName) // Invariant: a by-reference parameter has an element type.
            .IsEqualTo(InvariantCultureFormattingAnalyzer.InvariantHandlerMetadataName);
    }
}
