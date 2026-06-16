// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints.Tests;

/// <summary>Tests for <see cref="ExitPointCollector"/>.</summary>
internal sealed class ExitPointCollectorTests
{
    [Test]
    public async Task Collect_NullCompilation_Throws()
    {
        await Assert.That(() => ExitPointCollector.Collect(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Collect_SameCompilation_ReturnsCachedInstance()
    {
        Compilation compilation = _CreateCompilation("""
            namespace N;
            public class C
            {
                public int M() => 1;
            }
            """);

        IReadOnlyList<ExitPointEntry> first = ExitPointCollector.Collect(compilation);
        IReadOnlyList<ExitPointEntry> second = ExitPointCollector.Collect(compilation);

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task Collect_DifferentOptions_ReturnsDistinctInstances()
    {
        const string source = """
            namespace N;
            public class C
            {
                public void M()
                {
                    int Local() => 1;
                    _ = Local();
                }
            }
            """;

        Compilation compilation = _CreateCompilation(source);
        IReadOnlyList<ExitPointEntry> withoutLocals = ExitPointCollector.Collect(
            compilation,
            new ExitPointCollectorOptions { IncludeLocalFunctions = false });
        IReadOnlyList<ExitPointEntry> withLocals = ExitPointCollector.Collect(
            compilation,
            new ExitPointCollectorOptions { IncludeLocalFunctions = true });

        await Assert.That(ReferenceEquals(withoutLocals, withLocals)).IsFalse();
        await Assert.That(withLocals.Any(exit => exit.MethodDisplayName == "Local")).IsTrue();
    }

    [Test]
    public async Task Collect_SwitchExpressionBody_CountsArmExits()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(int x) => x switch
                {
                    1 => 1,
                    _ => 0,
                };
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count(exit => exit.Kind == ExitKind.SwitchArmCompletion)).IsEqualTo(2);
    }

    [Test]
    public async Task Collect_AssignmentSwitch_OnlyReturnExit()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(int x)
                {
                    int y = x switch { 1 => 1, _ => 0 };
                    return y;
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.SwitchArmCompletion)).IsFalse();
        await Assert.That(exits.Count(exit => exit.Kind == ExitKind.Return)).IsEqualTo(1);
    }

    [Test]
    public async Task Collect_MultipleReturnsSameLine_DetectsMultipleOnLine()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(bool a, bool b) { if (a) return 1; if (b) return 2; return 0; }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);
        int line = exits[0].Line;
        int onSameLine = exits.Count(exit => exit.Line == line);

        await Assert.That(onSameLine).IsGreaterThan(1);
    }

    [Test]
    public async Task Collect_ThrowStatement_CountsThrow()
    {
        const string source = """
            namespace N;
            public class C
            {
                public void M() { throw new System.Exception(); }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.Throw)).IsTrue();
    }

    [Test]
    public async Task Collect_ThrowExpression_CountsThrowExpression()
    {
        const string source = """
            namespace N;
            public class C
            {
                public object M(bool b) => b ? 1 : throw new System.Exception();
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ThrowExpression)).IsTrue();
    }

    [Test]
    public async Task Collect_ConditionalExpression_CountsArmExits()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(bool b) => b ? 1 : 2;
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count(exit => exit.Kind == ExitKind.ConditionalArmCompletion)).IsEqualTo(2);
    }

    [Test]
    public async Task Collect_VoidImplicitEnd_CountsImplicitEnd()
    {
        const string source = """
            namespace N;
            public class C
            {
                public void M() { }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ImplicitEnd)).IsTrue();
    }

    [Test]
    public async Task Collect_YieldBreak_CountsYieldBreak()
    {
        const string source = """
            namespace N;
            using System.Collections.Generic;
            public class C
            {
                public IEnumerable<int> M() { yield break; }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source, typeof(IEnumerable<int>));

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.YieldBreak)).IsTrue();
    }

    [Test]
    public async Task Collect_SwitchStatementReturn_CountsReturn()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(int x)
                {
                    switch (x)
                    {
                        case 1: return 1;
                        default: return 0;
                    }
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count(exit => exit.Kind == ExitKind.Return)).IsEqualTo(2);
    }

    [Test]
    public async Task Collect_IfElse_ReturnsOnBranches()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(bool b)
                {
                    if (b) return 1;
                    else return 2;
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count(exit => exit.Kind == ExitKind.Return)).IsEqualTo(2);
    }

    [Test]
    public async Task Collect_IfWithoutElse_WalksThenBranch()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(bool b)
                {
                    if (b) return 1;
                    return 0;
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count(exit => exit.Kind == ExitKind.Return)).IsEqualTo(2);
    }

    [Test]
    public async Task Collect_Constructor_CountsImplicitEnd()
    {
        const string source = """
            namespace N;
            public class C
            {
                public C() { }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.MethodDisplayName == ".ctor" && exit.Kind == ExitKind.ImplicitEnd)).IsTrue();
    }

    [Test]
    public async Task Collect_PropertyAccessor_CountsReturn()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int P { get => 1; }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Collect_ExpressionBodiedProperty_CountsExpressionCompletion()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int P => 1;
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion && exit.MethodDisplayName == "P")).IsTrue();
    }

    [Test]
    public async Task Collect_SkipsObjAndGeneratedTrees()
    {
        const string source = "namespace N; public class C { public int M() => 1; }";
        SyntaxTree main = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        SyntaxTree generated = CSharpSyntaxTree.ParseText("class G {}", path: @"C:\proj\obj\Debug\File.g.cs");
        SyntaxTree designer = CSharpSyntaxTree.ParseText("class D {}", path: @"C:\proj\Main.designer.cs");
        SyntaxTree binTree = CSharpSyntaxTree.ParseText("class B {}", path: @"C:\proj\bin\Release\Main.cs");

        Compilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { main, generated, designer, binTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(compilation);

        await Assert.That(exits.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Collect_LocalFunction_ExcludedByDefault()
    {
        const string source = """
            namespace N;
            public class C
            {
                public void M()
                {
                    int Local() => 1;
                    _ = Local();
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(
            _CreateCompilation(source),
            new ExitPointCollectorOptions { IncludeLocalFunctions = false });

        await Assert.That(exits.Any(exit => exit.MethodDisplayName == "Local")).IsFalse();
    }

    [Test]
    public async Task Collect_LocalFunction_UsesOwnMethodDisplayNameWhenEnabled()
    {
        const string source = """
            namespace N;
            public class C
            {
                public void M()
                {
                    int Local() { return 1; }
                    _ = Local();
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(
            _CreateCompilation(source),
            new ExitPointCollectorOptions { IncludeLocalFunctions = true });

        await Assert.That(exits.Any(exit => exit.MethodDisplayName == "Local" && exit.Kind == ExitKind.Return)).IsTrue();
    }

    [Test]
    public async Task Collect_InvocationExpression_CompletesAtMember()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M() => System.Math.Abs(-1);
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    [Test]
    public async Task Collect_Destructor_CountsImplicitEnd()
    {
        const string source = """
            namespace N;
            public class C
            {
                ~C() { }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ImplicitEnd)).IsTrue();
    }

    [Test]
    public async Task Collect_Operator_CountsExpressionCompletion()
    {
        const string source = """
            namespace N;
            public sealed class C
            {
                public static C operator +(C left, C right) => left;
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    [Test]
    public async Task Collect_ConversionOperator_CountsExpressionCompletion()
    {
        const string source = """
            namespace N;
            public sealed class C
            {
                public static explicit operator int(C value) => 1;
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    [Test]
    public async Task Collect_CoalesceThrow_CountsBothExits()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M(int? value) => value ?? throw new System.Exception();
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ThrowExpression)).IsTrue();
    }

    [Test]
    public async Task Collect_NestedBlockWithReturn_WalksChildStatements()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M()
                {
                    {
                        return 1;
                    }
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.Return)).IsTrue();
    }

    [Test]
    public async Task Collect_ReturnInvocation_UsesMemberExit()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M() => System.Math.Abs(-1);
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    [Test]
    public async Task Collect_ReturnIdentifierName_UsesReturnKeyword()
    {
        const string source = """
            namespace N;
            public class C
            {
                private int _Value = 1;
                public int M()
                {
                    return _Value;
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.Return)).IsTrue();
    }

    [Test]
    public async Task Collect_ExpressionBodiedLocalFunction_WalksBodyWhenEnabled()
    {
        const string source = """
            namespace N;
            public class C
            {
                public void M()
                {
                    int Local() => 1;
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(
            _CreateCompilation(source),
            new ExitPointCollectorOptions { IncludeLocalFunctions = true });

        await Assert.That(exits.Count).IsGreaterThan(1);
    }

    [Test]
    public async Task Collect_SetAccessor_CountsImplicitEnd()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int P { set { } }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ImplicitEnd)).IsTrue();
    }

    [Test]
    public async Task Collect_VoidReturnStatement_CountsReturn()
    {
        const string source = """
            namespace N;
            public class C
            {
                public void M()
                {
                    return;
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.Return)).IsTrue();
    }

    [Test]
    public async Task Collect_ReturnParenthesizedExpression_WalksCompletion()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M() { return (1 + 2); }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    [Test]
    public async Task Collect_LockStatementWithReturn_WalksNestedStatement()
    {
        const string source = """
            namespace N;
            public class C
            {
                private readonly object _Gate = new object();
                public int M()
                {
                    lock (_Gate)
                    {
                        return 1;
                    }
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.Return)).IsTrue();
    }

    [Test]
    public async Task Collect_ReturnStaticIdentifierCall_CompletesAtIdentifier()
    {
        const string source = """
            namespace N;
            public class C
            {
                private static int Helper() => 1;
                public int M() { return Helper(); }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    [Test]
    public async Task Collect_InMemoryTreeWithoutPath_IsNotSkipped()
    {
        const string source = "namespace N; public class C { public int M() => 1; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        Compilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(compilation);

        await Assert.That(exits.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Collect_AbstractMethodWithNullBody_DoesNotThrow()
    {
        const string source = """
            namespace N;
            public abstract class C
            {
                public abstract void M();
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Collect_ReturnElementAccess_WalksDefaultExpression()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M() { return new int[] { 1 }[0]; }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    [Test]
    public async Task Collect_PartialMethodWithoutBody_UsesNameFallback()
    {
        const string source = """
            namespace N;
            public partial class C
            {
                partial void M();
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source);

        await Assert.That(exits.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Collect_ReturnDelegateInvoke_CompletesAtFirstToken()
    {
        const string source = """
            namespace N;
            using System;
            public class C
            {
                public int M() { return new Func<int>(() => 1)(); }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = _Collect(source, typeof(Func<int>));

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.ExpressionCompletion)).IsTrue();
    }

    private static IReadOnlyList<ExitPointEntry> _Collect(string source, params Type[] extraTypes)
    {
        return ExitPointCollector.Collect(_CreateCompilation(source, extraTypes));
    }

    private static CSharpCompilation _CreateCompilation(string source, params Type[] extraTypes)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        MetadataReference[] references = new[] { typeof(object).Assembly.Location }
            .Concat(extraTypes.Select(type => type.Assembly.Location))
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        return CSharpCompilation.Create("TestAssembly", new[] { tree }, references);
    }
}
