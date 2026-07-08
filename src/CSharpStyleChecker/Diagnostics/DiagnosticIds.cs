// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleChecker.Diagnostics;

/// <summary>Diagnostic identifier constants.</summary>
public static class DiagnosticIds
{
    /// <summary>Line exceeds maximum length.</summary>
    public const string LineLength = "CSC001";

    /// <summary>Implicit or explicit <c>var</c> usage.</summary>
    public const string NoVar = "CSC002";

    /// <summary>Private member naming violation.</summary>
    public const string PrivateNaming = "CSC003";

    /// <summary>Blocking task usage.</summary>
    public const string TaskBlocking = "CSC004";

    /// <summary>Using directive outside global usings file.</summary>
    public const string GlobalUsingsOnly = "CSC005";

    /// <summary>Multiple exit points on the same source line.</summary>
    public const string MultipleExitsPerLine = "CSC006";

    /// <summary>Non-atomic access to a volatile field.</summary>
    public const string VolatileFieldAccess = "CSC007";

    /// <summary>Redundant explicit type in object or collection creation.</summary>
    public const string TargetTypedCreation = "CSC008";
}
