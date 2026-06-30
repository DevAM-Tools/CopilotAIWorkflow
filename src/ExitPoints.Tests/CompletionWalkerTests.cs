// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints.Tests;

/// <summary>Direct branch tests for <see cref="CompletionWalker"/>.</summary>
internal sealed class CompletionWalkerTests
{
    [Test]
    public async Task WalkCallableBody_NullBodyAndExpression_DoesNotAddExits()
    {
        const string source = "namespace N; public class C { }";
        CSharpCompilation compilation = _CreateCompilation(source);
        SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        List<ExitPointEntry> results = [];

        CompletionWalker.WalkCallableBody(
            null,
            null,
            "C.M()",
            "M",
            results,
            isLocal: false,
            model,
            new ExitPointCollectorOptions());

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WalkStatement_DefaultChildStatement_WalksNestedReturn()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M()
                {
                    lock (new object()) { return 1; }
                }
            }
            """;

        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(_CreateCompilation(source));

        await Assert.That(exits.Any(exit => exit.Kind == ExitKind.Return)).IsTrue();
    }

    private static CSharpCompilation _CreateCompilation(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
    }
}
