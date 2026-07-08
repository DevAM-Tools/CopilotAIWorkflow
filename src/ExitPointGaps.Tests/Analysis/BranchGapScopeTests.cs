// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

/// <summary>Tests for <see cref="BranchGapScope"/>.</summary>
public sealed class BranchGapScopeTests
{
    [Test]
    public async Task IncludesPackage_MatchingSuffix_ReturnsTrue()
    {
        BranchGapScope scope = new BranchGapScope(["Sample"]);

        await Assert.That(scope.IncludesPackage("My.Sample")).IsTrue();
    }

    [Test]
    public async Task IncludesPackage_NonMatchingSuffix_ReturnsFalse()
    {
        BranchGapScope scope = new BranchGapScope(["Sample"]);

        await Assert.That(scope.IncludesPackage("Other")).IsFalse();
    }

    [Test]
    public async Task IncludesPackage_NotSample_ReturnsFalse()
    {
        BranchGapScope scope = new BranchGapScope(["Sample"]);

        await Assert.That(scope.IncludesPackage("NotSample")).IsFalse();
    }

    [Test]
    public async Task IncludesPackage_NotCSharpStyleChecker_ReturnsFalse()
    {
        BranchGapScope scope = new BranchGapScope(["CSharpStyleChecker"]);

        await Assert.That(scope.IncludesPackage("NotCSharpStyleChecker")).IsFalse();
    }

    [Test]
    public async Task IncludesPackage_ExactName_ReturnsTrue()
    {
        BranchGapScope scope = new BranchGapScope(["CSharpStyleChecker"]);

        await Assert.That(scope.IncludesPackage("CSharpStyleChecker")).IsTrue();
    }

    [Test]
    public async Task Constructor_EmptySuffixes_Throws()
    {
        await Assert.That(() => new BranchGapScope([])).Throws<ArgumentException>();
    }
}
