// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleValidator.Analyzers;
using ExitPoints;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions;

namespace CSharpStyleValidator.Tests.Analyzers;

/// <summary>Direct tests for <see cref="MultipleExitsPerLineAnalyzer"/> location mapping.</summary>
internal sealed class MultipleExitsPerLineAnalyzerLocationTests
{
    [Test]
    public async Task TryCreateLocation_MissingTree_ReturnsNull()
    {
        const string source = "namespace N; public class C { public int M() => 1; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        Compilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        ExitPointEntry entry = new ExitPointEntry(
            "M:1:1:Return",
            @"C:\other\File.cs",
            1,
            1,
            "C.M()",
            "M",
            ExitKind.Return);

        Location location = MultipleExitsPerLineAnalyzer.TryCreateLocation(compilation, entry);

        await Assert.That(location).IsNull();
    }

    [Test]
    public async Task TryCreateLocation_LineOutOfRange_ReturnsNull()
    {
        const string source = "namespace N; public class C { public int M() => 1; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        Compilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        ExitPointEntry entry = new ExitPointEntry(
            "M:99:1:Return",
            @"C:\proj\Main.cs",
            99,
            1,
            "C.M()",
            "M",
            ExitKind.Return);

        Location location = MultipleExitsPerLineAnalyzer.TryCreateLocation(compilation, entry);

        await Assert.That(location).IsNull();
    }

    [Test]
    public async Task TryCreateLocation_ColumnBeyondLineLength_UsesLineStart()
    {
        const string source = "namespace N; public class C { public int M() => 1; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        Compilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        ExitPointEntry entry = new ExitPointEntry(
            "M:1:999:Return",
            @"C:\proj\Main.cs",
            1,
            999,
            "C.M()",
            "M",
            ExitKind.Return);

        Location location = MultipleExitsPerLineAnalyzer.TryCreateLocation(compilation, entry);

        await Assert.That(location).IsNotNull();
    }

    [Test]
    public async Task TryCreateLocation_PositionBeyondTextLength_UsesLineStart()
    {
        const string source = "x";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: @"C:\proj\Main.cs");
        Compilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        ExitPointEntry entry = new ExitPointEntry(
            "M:1:99:Return",
            @"C:\proj\Main.cs",
            1,
            99,
            "M",
            "M",
            ExitKind.Return);

        Location location = MultipleExitsPerLineAnalyzer.TryCreateLocation(compilation, entry);

        await Assert.That(location).IsNotNull();
    }
}
