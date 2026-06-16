// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Linq;
using System.Threading.Tasks;
using CSharpStyleValidator.Diagnostics;
using Microsoft.CodeAnalysis;
using TUnit.Assertions;

namespace CSharpStyleValidator.Tests.Diagnostics;

/// <summary>Tests for <see cref="DiagnosticDescriptors"/>.</summary>
public sealed class DiagnosticDescriptorsTests
{
    [Test]
    public async Task All_ContainsSevenUniqueErrorDescriptors()
    {
        await Assert.That(DiagnosticDescriptors.All.Count).IsEqualTo(7);

        string[] ids = DiagnosticDescriptors.All.Select(static descriptor => descriptor.Id).ToArray();
        await Assert.That(ids.Distinct().Count()).IsEqualTo(7);

        foreach (DiagnosticDescriptor descriptor in DiagnosticDescriptors.All)
        {
            await Assert.That(descriptor.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        }
    }
}
