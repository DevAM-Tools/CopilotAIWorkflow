// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis.Models;

/// <summary>An uncovered branch condition in source.</summary>
public sealed record BranchGap(
    int Priority,
    string Package,
    string ClassName,
    string FilePath,
    int Line,
    int ConditionIndex,
    double ConditionCoverage,
    int LineHits,
    string? Method,
    string? Snippet);
