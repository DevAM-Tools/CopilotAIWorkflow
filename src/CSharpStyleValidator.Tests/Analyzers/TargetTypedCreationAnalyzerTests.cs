// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleValidator.Analyzers;
using CSharpStyleValidator.Diagnostics;
using CSharpStyleValidator.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleValidator.Tests.Analyzers;

/// <summary>Tests for <see cref="TargetTypedCreationAnalyzer"/>.</summary>
public sealed class TargetTypedCreationAnalyzerTests
{
    private const string Usings = "using System;\nusing System.Collections.Generic;\n";

    [Test]
    public async Task TargetTypedCreation_ExplicitParameterlessType_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public sealed class TypeA
            {
                public static TypeA M()
                {
                    TypeA a = new {|#0:TypeA|}();
                    return a;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("TypeA"));
    }

    [Test]
    public async Task TargetTypedCreation_TargetTypedNew_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public sealed class TypeA
            {
                public static TypeA M()
                {
                    TypeA a = new();
                    return a;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_ConstructorArguments_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public sealed class Foo
            {
                public Foo(int value) { }
                public static Foo M()
                {
                    Foo x = new Foo(1);
                    return x;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_ObjectInitializerWithMembers_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public sealed class TypeA
            {
                public int Prop { get; set; }
                public static TypeA M()
                {
                    TypeA a = new TypeA() { Prop = 1 };
                    return a;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_EmptyObjectInitializer_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public sealed class TypeA
            {
                public static TypeA M()
                {
                    TypeA a = new {|#0:TypeA|}() { };
                    return a;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("TypeA"));
    }

    [Test]
    public async Task TargetTypedCreation_ExplicitEmptyList_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static List<int> M()
                {
                    List<int> x = new {|#0:List<int>|}();
                    return x;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("List<int>"));
    }

    [Test]
    public async Task TargetTypedCreation_CollectionExpression_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static List<int> M()
                {
                    List<int> x = [];
                    return x;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_CollectionInitializer_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static List<int> M()
                {
                    List<int> x = new {|#0:List<int>|} { 1 };
                    return x;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("List<int>"));
    }

    [Test]
    public async Task TargetTypedCreation_ArrayInitializer_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static int[] M()
                {
                    int[] a = new {|#0:int[]|} { 1, 2 };
                    return a;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("int[]"));
    }

    [Test]
    public async Task TargetTypedCreation_RankSizedArray_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static int[] M()
                {
                    int[] a = new int[10];
                    return a;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_DerivedAssignedToBase_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class Base { }
            public sealed class Derived : Base
            {
                public static Base M()
                {
                    Base b = new Derived();
                    return b;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_InterfaceTarget_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static IList<int> M()
                {
                    IList<int> x = new List<int>();
                    return x;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_TypeParameter_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static T M<T>() where T : new()
                {
                    T t = new T();
                    return t;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_ThrowStatement_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public static class C
            {
                public static void M()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_ExplicitTypeInReturn_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public sealed class TypeA
            {
                public static TypeA M()
                {
                    return new {|#0:TypeA|}();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("TypeA"));
    }

    [Test]
    public async Task TargetTypedCreation_ExplicitTypeInAssignment_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public sealed class TypeA
            {
                public static void M()
                {
                    TypeA a = new();
                    a = new {|#0:TypeA|}();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("TypeA"));
    }

    [Test]
    public async Task TargetTypedCreation_ExplicitTypeInArgument_ReportsCsv008()
    {
        const string source = Usings + """
            namespace N;
            public sealed class TypeA
            {
                public static void Accept(TypeA value) { }
                public static void M()
                {
                    Accept(new {|#0:TypeA|}());
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TargetTypedCreation).WithLocation(0).WithArguments("TypeA"));
    }

    [Test]
    public async Task TargetTypedCreation_CustomInterfaceTarget_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public interface IA
            {
            }

            public sealed class A : IA
            {
            }

            public static class C
            {
                public static IA M()
                {
                    IA a = new A();
                    return a;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_ArrayAssignedToReadOnlyMemory_NoDiagnostic()
    {
        const string source = "using System;\n" + """
            namespace N;
            public static class C
            {
                public static ReadOnlyMemory<byte> M()
                {
                    ReadOnlyMemory<byte> memory = new byte[] { 1, 2 };
                    return memory;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_ArrayReturnedAsReadOnlyMemory_NoDiagnostic()
    {
        const string source = "using System;\n" + """
            namespace N;
            public static class C
            {
                public static ReadOnlyMemory<byte> M()
                {
                    return new byte[] { 1, 2 };
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }

    [Test]
    public async Task TargetTypedCreation_ArrayPassedToReadOnlyMemoryParameter_NoDiagnostic()
    {
        const string source = "using System;\n" + """
            namespace N;
            public static class C
            {
                public static void Accept(ReadOnlyMemory<byte> memory) { }

                public static void M()
                {
                    Accept(new byte[] { 1, 2 });
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TargetTypedCreationAnalyzer>(source);
    }
}
