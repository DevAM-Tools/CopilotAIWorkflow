// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CSharpStyleValidator;

internal static class AnalyzerGuard
{
    internal static void RequireContext(Microsoft.CodeAnalysis.Diagnostics.AnalysisContext context)
    {
        if (context is null)
        {
            throw new System.ArgumentNullException(nameof(context));
        }
    }
}
