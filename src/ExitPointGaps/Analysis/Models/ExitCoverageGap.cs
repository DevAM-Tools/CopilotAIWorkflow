// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Analysis.Models;

/// <summary>An exit point with insufficient line coverage.</summary>
public sealed record ExitCoverageGap(
    int Priority,
    string ExitPointId,
    string FilePath,
    int Line,
    int Column,
    string Kind,
    string MethodDisplayName,
    int Hits,
    string? Snippet);
