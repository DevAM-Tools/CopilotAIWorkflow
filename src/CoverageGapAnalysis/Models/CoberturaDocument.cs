// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Models;

/// <summary>Parsed Cobertura coverage document.</summary>
public sealed class CoberturaDocument
{
    /// <summary>Initializes a parsed document.</summary>
    /// <param name="sourcePath">Original file path.</param>
    /// <param name="branchRate">Root branch coverage ratio.</param>
    /// <param name="packageBranchRates">Scoped package branch rates by package name.</param>
    /// <param name="classBranchRates">Class branch rates by source file path.</param>
    /// <param name="lines">Line coverage keyed by absolute or report file path.</param>
    public CoberturaDocument(
        string sourcePath,
        double branchRate,
        IReadOnlyDictionary<string, double> packageBranchRates,
        IReadOnlyDictionary<string, double> classBranchRates,
        IReadOnlyDictionary<string, IReadOnlyDictionary<int, CoberturaLineInfo>> lines)
    {
        SourcePath = sourcePath;
        BranchRate = branchRate;
        PackageBranchRates = packageBranchRates;
        ClassBranchRates = classBranchRates;
        Lines = lines;
    }

    /// <summary>Original Cobertura file path.</summary>
    public string SourcePath { get; }

    /// <summary>Document-level branch coverage ratio.</summary>
    public double BranchRate { get; }

    /// <summary>Package branch rates keyed by package name.</summary>
    public IReadOnlyDictionary<string, double> PackageBranchRates { get; }

    /// <summary>Class branch rates keyed by filename from Cobertura.</summary>
    public IReadOnlyDictionary<string, double> ClassBranchRates { get; }

    /// <summary>Line map keyed by file path then one-based line number.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<int, CoberturaLineInfo>> Lines { get; }
}
