// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Threading.Tasks;
using CSharpStyleChecker;
using TUnit.Assertions;

namespace CSharpStyleChecker.Tests;

/// <summary>Tests for <see cref="AnalyzerGuard"/>.</summary>
internal sealed class AnalyzerGuardTests
{
    [Test]
    public async Task RequireContext_Null_Throws()
    {
        await Assert.That(() => AnalyzerGuard.RequireContext(null)).Throws<ArgumentNullException>();
    }
}
