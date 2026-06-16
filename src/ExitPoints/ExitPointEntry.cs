// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints;

/// <summary>A single callable exit point in source.</summary>
/// <param name="ExitPointId">Stable identifier for reports.</param>
/// <param name="FilePath">Source file path.</param>
/// <param name="Line">One-based line number.</param>
/// <param name="Column">One-based column number.</param>
/// <param name="MethodId">Callable metadata identifier.</param>
/// <param name="MethodDisplayName">Human-readable callable name.</param>
/// <param name="Kind">Exit classification.</param>
public sealed record ExitPointEntry(
    string ExitPointId,
    string FilePath,
    int Line,
    int Column,
    string MethodId,
    string MethodDisplayName,
    ExitKind Kind);
