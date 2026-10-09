// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints;

#region Public API

/// <summary>
/// A single callable exit point in source.
/// Immutable value; no shared mutable state.
/// </summary>
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
public readonly record struct ExitPointEntry(
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

#endregion
