// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests;

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
    public async Task IncludesPackage_NotCSharpStyleValidator_ReturnsFalse()
    {
        BranchGapScope scope = new BranchGapScope(["CSharpStyleValidator"]);

        await Assert.That(scope.IncludesPackage("NotCSharpStyleValidator")).IsFalse();
    }

    [Test]
    public async Task IncludesPackage_ExactName_ReturnsTrue()
    {
        BranchGapScope scope = new BranchGapScope(["CSharpStyleValidator"]);

        await Assert.That(scope.IncludesPackage("CSharpStyleValidator")).IsTrue();
    }

    [Test]
    public async Task Constructor_EmptySuffixes_Throws()
    {
        await Assert.That(() => new BranchGapScope([])).Throws<ArgumentException>();
    }
}
