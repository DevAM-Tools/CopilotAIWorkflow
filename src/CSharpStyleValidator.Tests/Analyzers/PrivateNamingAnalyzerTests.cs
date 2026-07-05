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
    private const string CollectionUsings = "using System;\nusing System.Collections;\nusing System.Collections.Generic;\n";

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

    [Test]
    public async Task PrivateNaming_PrivateNestedTypeName_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public sealed class C
            {
                private sealed class Enumerator
                {
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_FieldInsidePrivateNestedType_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public sealed class C
            {
                private sealed class Nested
                {
                    private int index;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_ManualIEnumerableEnumerator_NoDiagnostic()
    {
        const string source = CollectionUsings + """
            namespace N;
            public sealed class C : IEnumerable<int>
            {
                private readonly List<int> _Items;

                public C(List<int> items)
                {
                    _Items = items;
                }

                public IEnumerator<int> GetEnumerator() => new Enumerator(_Items);

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                private sealed class Enumerator : IEnumerator<int>
                {
                    private readonly List<int> items;
                    private int index = -1;

                    public Enumerator(List<int> items)
                    {
                        this.items = items;
                    }

                    public int Current => items[index];

                    object IEnumerator.Current => Current;

                    public bool MoveNext()
                    {
                        index++;
                        return index < items.Count;
                    }

                    public void Reset()
                    {
                        index = -1;
                    }

                    public void Dispose()
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_YieldIterator_NoDiagnostic()
    {
        const string source = CollectionUsings + """
            namespace N;
            public sealed class C
            {
                public IEnumerable<int> M()
                {
                    yield return 1;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_ExplicitInterfaceGetEnumerator_NoDiagnostic()
    {
        const string source = CollectionUsings + """
            namespace N;
            public sealed class C : IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator()
                {
                    yield break;
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_LocalFunction_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public sealed class C
            {
                public int M()
                {
                    int LocalAdd(int left, int right)
                    {
                        return left + right;
                    }

                    return LocalAdd(1, 2);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(source);
    }

    [Test]
    public async Task PrivateNaming_FieldInsidePublicNestedType_ReportsCsv003()
    {
        const string source = Usings + """
            namespace N;
            public sealed class C
            {
                public sealed class Nested
                {
                    private int {|#0:count|};
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<PrivateNamingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.PrivateNaming).WithLocation(0).WithArguments("field", "count"));
    }
}
