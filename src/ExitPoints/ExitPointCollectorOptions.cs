// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPoints;

/// <summary>Options controlling exit point collection.</summary>
public sealed class ExitPointCollectorOptions
{
    /// <summary>When <see langword="true"/>, includes local function exit points.</summary>
    public bool IncludeLocalFunctions { get; init; }
}
