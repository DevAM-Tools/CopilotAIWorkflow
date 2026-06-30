// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleValidator.Diagnostics;

/// <summary>Diagnostic identifier constants.</summary>
public static class DiagnosticIds
{
    /// <summary>Line exceeds maximum length.</summary>
    public const string LineLength = "CSV001";

    /// <summary>Implicit or explicit <c>var</c> usage.</summary>
    public const string NoVar = "CSV002";

    /// <summary>Private member naming violation.</summary>
    public const string PrivateNaming = "CSV003";

    /// <summary>Blocking task usage.</summary>
    public const string TaskBlocking = "CSV004";

    /// <summary>Using directive outside global usings file.</summary>
    public const string GlobalUsingsOnly = "CSV005";

    /// <summary>Multiple exit points on the same source line.</summary>
    public const string MultipleExitsPerLine = "CSV006";

    /// <summary>Plain access to a volatile field.</summary>
    public const string VolatileFieldAccess = "CSV007";

    /// <summary>Redundant explicit type in object or collection creation.</summary>
    public const string TargetTypedCreation = "CSV008";
}
