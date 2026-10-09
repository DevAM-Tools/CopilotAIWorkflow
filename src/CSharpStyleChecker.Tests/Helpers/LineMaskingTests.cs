// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleChecker.Helpers;
using TUnit.Assertions;

namespace CSharpStyleChecker.Tests.Helpers;

/// <summary>Tests for <see cref="LineMasking"/>.</summary>
internal sealed class LineMaskingTests
{
    [Test]
    public async Task MaskLineNullThrows()
    {
        await Assert.That(() => LineMasking.MaskLine(null!, default)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task MaskLineLongStringLiteralDoesNotCountStringContent()
    {
        string line = "int x = \"" + new string('a', 200) + "\";";
        (string masked, _) = LineMasking.MaskLine(line, default);

        await Assert.That(masked.TrimEnd().Length).IsLessThanOrEqualTo(20);
    }

    [Test]
    public async Task MaskLineLineCommentCountsRemainder()
    {
        (string masked, LineMaskState state) = LineMasking.MaskLine("int x = 1; // comment with var keyword", default);

        await Assert.That(masked).IsEqualTo("int x = 1; // comment with var keyword");
        await Assert.That(state.InBlockComment).IsFalse();
        await Assert.That(state.OpenString).IsEqualTo(LineStringKind.None);
    }

    [Test]
    public async Task MaskLineBlockCommentKeepsContent()
    {
        (string masked, LineMaskState state) = LineMasking.MaskLine("int x = 1; /* block */ int y = 2;", default);

        await Assert.That(masked).IsEqualTo("int x = 1; /* block */ int y = 2;");
        await Assert.That(state.InBlockComment).IsFalse();
    }

    [Test]
    public async Task MaskLineBlockCommentContinuesFromPriorLine()
    {
        LineMaskState prior = new LineMaskState(inBlockComment: true, LineStringKind.None, rawDelimiterLength: 0);
        (string masked, LineMaskState state) = LineMasking.MaskLine("still comment */ int x = 1;", prior);

        await Assert.That(masked).IsEqualTo("still comment */ int x = 1;");
        await Assert.That(state.InBlockComment).IsFalse();
    }

    [Test]
    public async Task MaskLineBlockCommentSpansLinesLeavesInBlock()
    {
        (string masked, LineMaskState state) = LineMasking.MaskLine("before /* start", default);

        await Assert.That(masked).IsEqualTo("before /* start");
        await Assert.That(state.InBlockComment).IsTrue();
    }

    [Test]
    public async Task MaskLineVerbatimStringCollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine(@"int x = @""hello"";", default);

        await Assert.That(masked.Length).IsLessThanOrEqualTo(12);
    }

    [Test]
    public async Task MaskLineVerbatimDoubledQuoteCollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine(@"int x = @"""""";", default);

        await Assert.That(masked.Length).IsLessThanOrEqualTo(12);
    }

    [Test]
    public async Task MaskLineInterpolatedStringCountsHoleContent()
    {
        (string masked, _) = LineMasking.MaskLine("int x = $\"{1}\";", default);

        await Assert.That(masked).IsEqualTo("int x = $\"{1}\";");
    }

    [Test]
    public async Task MaskLineVerbatimInterpolatedCountsHoleContent()
    {
        (string masked, _) = LineMasking.MaskLine("int x = @$\"{1}\";", default);

        await Assert.That(masked).IsEqualTo("int x = @$\"{1}\";");
    }

    [Test]
    public async Task MaskLineInterpolatedStringDoesNotCountLiteralPayload()
    {
        string line = "int x = $\"" + new string('a', 200) + "\";";
        (string masked, _) = LineMasking.MaskLine(line, default);

        await Assert.That(masked.TrimEnd().Length).IsLessThanOrEqualTo(20);
    }

    [Test]
    public async Task MaskLineCharLiteralCollapsesContent()
    {
        (string masked, _) = LineMasking.MaskLine("char c = 'a';", default);

        await Assert.That(masked).IsEqualTo("char c = '';");
    }

    [Test]
    public async Task MaskLineEscapedStringSkipsEscapeSequence()
    {
        (string masked, _) = LineMasking.MaskLine("string s = \"a\\t\";", default);

        await Assert.That(masked).IsEqualTo("string s = \"\";");
    }

    [Test]
    public async Task MaskLineAtWithoutStringAppendsAt()
    {
        (string masked, _) = LineMasking.MaskLine("int x = @;", default);

        await Assert.That(masked).IsEqualTo("int x = @;");
    }

    [Test]
    public async Task MaskLineEscapeAtEndOfStringClosesString()
    {
        (string masked, _) = LineMasking.MaskLine("string s = \"a\\", default);

        await Assert.That(masked).IsEqualTo("string s = \"");
    }

    [Test]
    public async Task MaskLineUnclosedVerbatimStringLeavesOpenState()
    {
        (string masked, LineMaskState state) = LineMasking.MaskLine("string s = @\"open", default);

        await Assert.That(masked).IsEqualTo("string s = @\"");
        await Assert.That(state.OpenString).IsEqualTo(LineStringKind.Verbatim);
    }

    [Test]
    public async Task MaskLineAtEndOfLineUsesDefaultNextChar()
    {
        (string masked, _) = LineMasking.MaskLine("int x = @", default);

        await Assert.That(masked).IsEqualTo("int x = @");
    }

    [Test]
    public async Task MaskLineUnclosedBlockCommentLeavesInBlock()
    {
        (string masked, LineMaskState state) = LineMasking.MaskLine("code /* unclosed", default);

        await Assert.That(masked).IsEqualTo("code /* unclosed");
        await Assert.That(state.InBlockComment).IsTrue();
    }

    [Test]
    public async Task MaskLineVerbatimContinuesSecondLineIsNotCode()
    {
        (string firstMasked, LineMaskState state) = LineMasking.MaskLine("string s = @\"", default);
        await Assert.That(firstMasked).IsEqualTo("string s = @\"");
        await Assert.That(state.OpenString).IsEqualTo(LineStringKind.Verbatim);

        string secondLine = new string('a', 200);
        (string secondMasked, LineMaskState secondState) = LineMasking.MaskLine(secondLine, state);

        await Assert.That(secondMasked.TrimEnd().Length).IsLessThanOrEqualTo(20);
        await Assert.That(secondState.OpenString).IsEqualTo(LineStringKind.Verbatim);
    }

    [Test]
    public async Task MaskLineRawStringCollapsesContent()
    {
        string line = "string s = \"\"\"" + new string('a', 200) + "\"\"\";";
        (string masked, LineMaskState state) = LineMasking.MaskLine(line, default);

        await Assert.That(masked.TrimEnd().Length).IsLessThanOrEqualTo(20);
        await Assert.That(state.OpenString).IsEqualTo(LineStringKind.None);
    }

    [Test]
    public async Task MaskLineQuoteInsideBlockCommentDoesNotOpenString()
    {
        (string masked, LineMaskState state) = LineMasking.MaskLine("/* \"not a string\" */ int x = 1;", default);

        await Assert.That(masked).IsEqualTo("/* \"not a string\" */ int x = 1;");
        await Assert.That(state.OpenString).IsEqualTo(LineStringKind.None);
        await Assert.That(state.InBlockComment).IsFalse();
    }
}
