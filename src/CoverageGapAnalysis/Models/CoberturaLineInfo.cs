// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Models;

/// <summary>Runtime coverage for a single source line in Cobertura.</summary>
public sealed class CoberturaLineInfo
{
    /// <summary>Initializes line coverage metadata.</summary>
    /// <param name="hits">Execution hit count.</param>
    /// <param name="isBranch">Whether the line records branch conditions.</param>
    /// <param name="conditions">Per-condition coverage ratios in 0–1.</param>
    /// <param name="methodName">Owning method name when known.</param>
    public CoberturaLineInfo(int hits, bool isBranch, IReadOnlyList<double> conditions, string? methodName)
    {
        Hits = hits;
        IsBranch = isBranch;
        Conditions = conditions;
        MethodName = methodName;
    }

    /// <summary>Execution hit count.</summary>
    public int Hits { get; }

    /// <summary>Whether branch conditions are recorded for this line.</summary>
    public bool IsBranch { get; }

    /// <summary>Per-condition coverage ratios from 0 to 1.</summary>
    public IReadOnlyList<double> Conditions { get; }

    /// <summary>Method name from Cobertura when available.</summary>
    public string? MethodName { get; }
}
