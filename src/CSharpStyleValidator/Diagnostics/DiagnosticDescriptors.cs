// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CSharpStyleValidator.Diagnostics;

/// <summary>Diagnostic descriptors for CSharpStyleValidator rules.</summary>
public static class DiagnosticDescriptors
{
    /// <summary>CSV001 line length.</summary>
    public static readonly DiagnosticDescriptor LineLength = new(
        DiagnosticIds.LineLength,
        "Line exceeds maximum length",
        "Line exceeds maximum length of {0} characters (visible length {1})",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Source lines must not exceed the configured maximum visible length.");

    /// <summary>CSV002 no var.</summary>
    public static readonly DiagnosticDescriptor NoVar = new(
        DiagnosticIds.NoVar,
        "Do not use var",
        "Use an explicit type instead of 'var'",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Explicit types are required instead of var.");

    /// <summary>CSV003 private naming.</summary>
    public static readonly DiagnosticDescriptor PrivateNaming = new(
        DiagnosticIds.PrivateNaming,
        "Private member naming violation",
        "Private {0} '{1}' must use _PascalCase",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Private fields, methods, and properties on the declaring type must use _PascalCase. "
            + "Exempt: members inside private nested types, explicit interface implementations, and local functions.");

    /// <summary>CSV004 task blocking.</summary>
    public static readonly DiagnosticDescriptor TaskBlocking = new(
        DiagnosticIds.TaskBlocking,
        "Blocking task usage",
        "Do not use .Wait() or .Result on Task; use async and await",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Blocking on tasks with .Wait() or .Result is not allowed.");

    /// <summary>CSV005 global usings only.</summary>
    public static readonly DiagnosticDescriptor GlobalUsingsOnly = new(
        DiagnosticIds.GlobalUsingsOnly,
        "Using directive outside GlobalUsings.cs",
        "Place namespace using directives in GlobalUsings.cs only; type aliases may remain in the file",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Namespace using directives must appear only in GlobalUsings.cs. File-local type aliases are allowed.");

    /// <summary>CSV006 multiple exits per line.</summary>
    public static readonly DiagnosticDescriptor MultipleExitsPerLine = new(
        DiagnosticIds.MultipleExitsPerLine,
        "Multiple exit points on one line",
        "Callable '{0}' has {1} exit points on line {2} ({3}); use at most one exit per line",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each callable may have at most one exit point per source line. Conditional, switch, null-coalescing (??), and null-coalescing assignment (??=) arms count as separate exits even when split across lines (grouped by operator).",
        customTags: "CompilationEnd");

    /// <summary>CSV007 volatile non-atomic access.</summary>
    public static readonly DiagnosticDescriptor VolatileFieldAccess = new(
        DiagnosticIds.VolatileFieldAccess,
        "Non-atomic volatile field access",
        "Use Interlocked for read-modify-write on volatile field '{0}'",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Plain volatile read and write are allowed. Increment, decrement, and compound assignments require Interlocked.");

    /// <summary>CSV008 target-typed creation.</summary>
    public static readonly DiagnosticDescriptor TargetTypedCreation = new(
        DiagnosticIds.TargetTypedCreation,
        "Use target-typed new() or collection expression",
        "Use target-typed 'new()' or '[]' instead of explicit type '{0}'",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Prefer target-typed new() or [] over repeating the type name when the type is known from context. "
            + "Exempt when the target cannot use a collection expression (e.g. ReadOnlyMemory<T>) or differs from the created type.");

    /// <summary>All rule descriptors.</summary>
    public static IReadOnlyList<DiagnosticDescriptor> All { get; } = new DiagnosticDescriptor[]
    {
        LineLength,
        NoVar,
        PrivateNaming,
        TaskBlocking,
        GlobalUsingsOnly,
        MultipleExitsPerLine,
        VolatileFieldAccess,
        TargetTypedCreation,
    };
}
