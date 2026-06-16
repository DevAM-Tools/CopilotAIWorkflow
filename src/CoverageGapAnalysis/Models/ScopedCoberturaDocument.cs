// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Models;

/// <summary>A Cobertura document with package scope for branch analysis.</summary>
public sealed record ScopedCoberturaDocument(CoberturaDocument Document, BranchGapScope Scope);
