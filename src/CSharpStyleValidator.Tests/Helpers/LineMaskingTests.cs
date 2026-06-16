// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleValidator.Helpers;
using TUnit.Assertions;

namespace CSharpStyleValidator.Tests.Helpers;

/// <summary>Tests for <see cref="LineMasking"/>.</summary>
internal sealed class LineMaskingTests
{
    [Test]
    public async Task MaskLine_Null_Throws()
    {
        await Assert.That(() => LineMasking.MaskLine(null!, false)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task MaskLine_LongStringLiteral_DoesNotCountStringContent()
    {
        string line = "int x = \"" + new string('a', 200) + "\";";
        (string masked, _) = LineMasking.MaskLine(line, false);

        await Assert.That(masked.TrimEnd().Length).IsLessThanOrEqualTo(20);
    }

    [Test]
    public async Task MaskLine_LineComment_MasksRemainder()
    {
        (string masked, _) = LineMasking.MaskLine("int x = 1; // comment with var keyword", false);

        await Assert.That(masked.Contains("var", System.StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task MaskLine_BlockComment_RemovesContent()
    {
        (string masked, bool inBlock) = LineMasking.MaskLine("int x = 1; /* block */ int y = 2;", false);

        await Assert.That(masked).IsEqualTo("int x = 1;  int y = 2;");
        await Assert.That(inBlock).IsFalse();
    }

    [Test]
    public async Task MaskLine_BlockCommentContinues_FromPriorLine()
    {
        (string masked, bool inBlock) = LineMasking.MaskLine("still comment */ int x = 1;", true);

        await Assert.That(masked).IsEqualTo(" int x = 1;");
        await Assert.That(inBlock).IsFalse();
    }

    [Test]
    public async Task MaskLine_BlockCommentSpansLines_LeavesInBlock()
    {
        (string masked, bool inBlock) = LineMasking.MaskLine("before /* start", false);

        await Assert.That(masked).IsEqualTo("before ");
        await Assert.That(inBlock).IsTrue();
    }

    [Test]
    public async Task MaskLine_VerbatimString_CollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine(@"int x = @""hello"";", false);

        await Assert.That(masked.Length).IsLessThanOrEqualTo(12);
    }

    [Test]
    public async Task MaskLine_VerbatimDoubledQuote_CollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine(@"int x = @"""""";", false);

        await Assert.That(masked.Length).IsLessThanOrEqualTo(12);
    }

    [Test]
    public async Task MaskLine_InterpolatedString_CollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine("int x = $\"{1}\";", false);

        await Assert.That(masked).IsEqualTo("int x = $\"\";");
    }

    [Test]
    public async Task MaskLine_VerbatimInterpolated_CollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine("int x = @$\"{1}\";", false);

        await Assert.That(masked).IsEqualTo("int x = @$\"\";");
    }

    [Test]
    public async Task MaskLine_CharLiteral_CollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine("char c = 'a';", false);

        await Assert.That(masked).IsEqualTo("char c = '';");
    }

    [Test]
    public async Task MaskLine_EscapedString_SkipsEscapeSequence()
    {
        (string masked, _) = LineMasking.MaskLine("string s = \"a\\t\";", false);

        await Assert.That(masked).IsEqualTo("string s = \"\";");
    }

    [Test]
    public async Task MaskLine_AtWithoutString_AppendsAt()
    {
        (string masked, _) = LineMasking.MaskLine("int x = @;", false);

        await Assert.That(masked).IsEqualTo("int x = @;");
    }

    [Test]
    public async Task MaskLine_EscapeAtEndOfString_ClosesString()
    {
        (string masked, _) = LineMasking.MaskLine("string s = \"a\\", false);

        await Assert.That(masked).IsEqualTo("string s = \"");
    }

    [Test]
    public async Task MaskLine_UnclosedVerbatimString_LeavesOpenState()
    {
        (string masked, _) = LineMasking.MaskLine("string s = @\"open", false);

        await Assert.That(masked).IsEqualTo("string s = @\"");
    }

    [Test]
    public async Task MaskLine_AtEndOfLine_UsesDefaultNextChar()
    {
        (string masked, _) = LineMasking.MaskLine("int x = @", false);

        await Assert.That(masked).IsEqualTo("int x = @");
    }

    [Test]
    public async Task MaskLine_UnclosedBlockComment_LeavesInBlock()
    {
        (string masked, bool inBlock) = LineMasking.MaskLine("code /* unclosed", false);

        await Assert.That(masked).IsEqualTo("code ");
        await Assert.That(inBlock).IsTrue();
    }
}
