// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints;

/// <summary>How control leaves a callable at an exit point.</summary>
public enum ExitKind
{
    /// <summary>Explicit <c>return</c> statement.</summary>
    Return,

    /// <summary>Explicit <c>throw</c> statement.</summary>
    Throw,

    /// <summary><c>throw</c> expression on a completion path.</summary>
    ThrowExpression,

    /// <summary><c>yield break</c> terminating an iterator.</summary>
    YieldBreak,

    /// <summary>Implicit completion at end of a void callable body.</summary>
    ImplicitEnd,

    /// <summary>Switch expression arm completing the callable.</summary>
    SwitchArmCompletion,

    /// <summary>Conditional <c>?:</c> arm completing the callable.</summary>
    ConditionalArmCompletion,

    /// <summary>Leaf expression completing the callable.</summary>
    ExpressionCompletion,
}
