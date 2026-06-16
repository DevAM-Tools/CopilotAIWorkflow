// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints.Tests;

/// <summary>Tests for <see cref="ExitPointExclusion"/>.</summary>
internal sealed class ExitPointExclusionTests
{
    [Test]
    public async Task IsExcludedAtPosition_TypeLevelAttribute_ReturnsTrue()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            [ExcludeFromCodeCoverage]
            public class C
            {
                public int M() => 1;
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(_CreateCompilation(tree));

        await Assert.That(ExitPointExclusion.IsExcludedAtPosition(tree, exits[0].Line, exits[0].Column)).IsTrue();
    }

    [Test]
    public async Task IsExcludedAtPosition_MethodLevelAttribute_ReturnsTrue()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            public class C
            {
                [ExcludeFromCodeCoverage]
                public int M() => 1;
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(_CreateCompilation(tree));

        await Assert.That(ExitPointExclusion.IsExcludedAtPosition(tree, exits[0].Line, exits[0].Column)).IsTrue();
    }

    [Test]
    public async Task IsExcludedAtPosition_NoAttribute_ReturnsFalse()
    {
        const string source = """
            namespace N;
            public class C
            {
                public int M() => 1;
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(_CreateCompilation(tree));

        await Assert.That(ExitPointExclusion.IsExcludedAtPosition(tree, exits[0].Line, exits[0].Column)).IsFalse();
    }

    [Test]
    public async Task IsExcludedAtPosition_NullTree_Throws()
    {
        await Assert.That(() => ExitPointExclusion.IsExcludedAtPosition(null!, 1, 1)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task HasExcludeAttribute_DetectsFullyQualifiedName()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public class C { }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        SyntaxNode root = await tree.GetRootAsync();
        ClassDeclarationSyntax classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        await Assert.That(ExitPointExclusion.HasExcludeAttribute(classDeclaration.AttributeLists)).IsTrue();
    }

    [Test]
    public async Task IsExcludedAtPosition_LocalFunctionAttribute_ReturnsTrue()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            public class C
            {
                public void M()
                {
                    [ExcludeFromCodeCoverage]
                    int Local() => 1;
                    _ = Local();
                }
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(
            _CreateCompilation(tree),
            new ExitPointCollectorOptions { IncludeLocalFunctions = true });

        ExitPointEntry localExit = exits.Single(exit => exit.MethodDisplayName == "Local");
        await Assert.That(ExitPointExclusion.IsExcludedAtPosition(tree, localExit.Line, localExit.Column)).IsTrue();
    }

    [Test]
    public async Task IsExcludedAtPosition_PropertyAccessorAttribute_ReturnsTrue()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            public class C
            {
                public int P
                {
                    [ExcludeFromCodeCoverage]
                    get => 1;
                }
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(_CreateCompilation(tree));
        ExitPointEntry getterExit = exits.Single(exit => exit.Kind == ExitKind.ExpressionCompletion);

        await Assert.That(ExitPointExclusion.IsExcludedAtPosition(tree, getterExit.Line, getterExit.Column)).IsTrue();
    }

    [Test]
    public async Task IsExcludedAtPosition_InvalidLine_ReturnsFalse()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("namespace N; class C { }", path: @"C:\proj\Main.cs");

        await Assert.That(ExitPointExclusion.IsExcludedAtPosition(tree, 99, 1)).IsFalse();
    }

    [Test]
    public async Task IsExcludedAtPosition_ColumnBeyondLineEnd_UsesLineStart()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            public class C
            {
                [ExcludeFromCodeCoverage]
                public int M() => 1;
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        IReadOnlyList<ExitPointEntry> exits = ExitPointCollector.Collect(_CreateCompilation(tree));

        await Assert.That(ExitPointExclusion.IsExcludedAtPosition(tree, exits[0].Line, 999)).IsTrue();
    }

    [Test]
    public async Task HasExcludeAttribute_SecondAttributeInList_ReturnsTrue()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            [Obsolete, ExcludeFromCodeCoverage]
            public class C { }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        SyntaxNode root = await tree.GetRootAsync();
        ClassDeclarationSyntax classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        await Assert.That(ExitPointExclusion.HasExcludeAttribute(classDeclaration.AttributeLists)).IsTrue();
    }

    [Test]
    public async Task HasExcludeAttribute_SecondAttributeList_ReturnsTrue()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            namespace N;
            [Obsolete]
            [ExcludeFromCodeCoverage]
            public class C { }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        SyntaxNode root = await tree.GetRootAsync();
        ClassDeclarationSyntax classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        await Assert.That(ExitPointExclusion.HasExcludeAttribute(classDeclaration.AttributeLists)).IsTrue();
    }

    [Test]
    public async Task HasExcludeAttribute_NoMatchingName_ReturnsFalse()
    {
        const string source = """
            using System;
            namespace N;
            [Obsolete]
            public class C { }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        SyntaxNode root = await tree.GetRootAsync();
        ClassDeclarationSyntax classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        await Assert.That(ExitPointExclusion.HasExcludeAttribute(classDeclaration.AttributeLists)).IsFalse();
    }

    private static CSharpCompilation _CreateCompilation(SyntaxTree tree)
    {
        MetadataReference reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        return CSharpCompilation.Create("TestAssembly", new[] { tree }, new[] { reference });
    }
}
