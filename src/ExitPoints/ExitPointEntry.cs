// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints;

/// <summary>A single callable exit point in source.</summary>
/// <param name="ExitPointId">Stable identifier for reports.</param>
/// <param name="FilePath">Source file path.</param>
/// <param name="Line">One-based line number of the exit token.</param>
/// <param name="Column">One-based column number of the exit token.</param>
/// <param name="MethodId">Callable metadata identifier.</param>
/// <param name="MethodDisplayName">Human-readable callable name.</param>
/// <param name="Kind">Exit classification.</param>
/// <param name="OperatorGroupId">Shared id for multi-arm <c>?:</c>, <c>??</c>, <c>??=</c>, and <c>switch</c> exits.</param>
/// <param name="OperatorLine">One-based line of the grouping operator when <paramref name="OperatorGroupId"/> is set.</param>
/// <param name="OperatorColumn">One-based column of the grouping operator when <paramref name="OperatorGroupId"/> is set.</param>
public sealed record ExitPointEntry(
    string ExitPointId,
    string FilePath,
    int Line,
    int Column,
    string MethodId,
    string MethodDisplayName,
    ExitKind Kind,
    string? OperatorGroupId = null,
    int? OperatorLine = null,
    int? OperatorColumn = null);
