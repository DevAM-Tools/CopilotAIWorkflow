// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleChecker.Tests.Helpers;

/// <summary>Verifier helpers for analyzer tests.</summary>
internal static class AnalyzerVerifier
{
    /// <summary>Verifies analyzer diagnostics for in-memory source.</summary>
    public static async Task VerifyAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await VerifyAsync<TAnalyzer>(source, null, expected).ConfigureAwait(false);
    }

    /// <summary>Verifies analyzer diagnostics for in-memory source compiled against the given framework references.</summary>
    /// <param name="source">The test source, with diagnostic markup.</param>
    /// <param name="referenceAssemblies">The framework the source is compiled against, for example <c>netstandard2.0</c>.</param>
    /// <param name="expected">The diagnostics the analyzer must report.</param>
    public static async Task VerifyOnFrameworkAsync<TAnalyzer>(
        string source,
        ReferenceAssemblies referenceAssemblies,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = source,
            ReferenceAssemblies = referenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync().ConfigureAwait(false);
    }

    /// <summary>Verifies analyzer diagnostics for in-memory source compiled with a fixed C# language version.</summary>
    /// <param name="source">The test source, with diagnostic markup.</param>
    /// <param name="languageVersion">The language version the source is parsed and compiled with.</param>
    /// <param name="expected">The diagnostics the analyzer must report.</param>
    public static async Task VerifyOnLanguageVersionAsync<TAnalyzer>(
        string source,
        LanguageVersion languageVersion,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = source,
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            // Invariant: the test project is a C# project, so project and parse options exist.
            CSharpParseOptions parseOptions = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(languageVersion));
        });

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync().ConfigureAwait(false);
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
