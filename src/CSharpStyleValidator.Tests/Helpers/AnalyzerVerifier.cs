// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleValidator.Tests.Helpers;

/// <summary>Verifier helpers for analyzer tests.</summary>
internal static class AnalyzerVerifier
{
    /// <summary>Verifies analyzer diagnostics for in-memory source.</summary>
    public static async Task VerifyAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await VerifyAsync<TAnalyzer>(source, null, expected).ConfigureAwait(false);
    }

    /// <summary>Verifies analyzer diagnostics with optional additional source files.</summary>
    public static async Task VerifyAsync<TAnalyzer>(
        string source,
        IReadOnlyList<(string FileName, string Content)>? additionalSources,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await VerifyAsync<TAnalyzer>(source, additionalSources, null, expected).ConfigureAwait(false);
    }

    /// <summary>Verifies analyzer diagnostics with optional sources and analyzer config.</summary>
    public static async Task VerifyAsync<TAnalyzer>(
        string source,
        IReadOnlyList<(string FileName, string Content)>? additionalSources,
        IReadOnlyList<(string FileName, string Content)>? analyzerConfigFiles,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = source,
        };

        if (additionalSources is not null)
        {
            foreach ((string fileName, string content) in additionalSources)
            {
                test.TestState.Sources.Add((fileName, content));
            }
        }

        if (analyzerConfigFiles is not null)
        {
            foreach ((string fileName, string content) in analyzerConfigFiles)
            {
                test.TestState.AnalyzerConfigFiles.Add((fileName, content));
            }
        }

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync().ConfigureAwait(false);
    }
}
