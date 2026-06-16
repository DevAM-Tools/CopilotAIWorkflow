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
        description: "Private fields, methods, and properties must be named _PascalCase.");

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
        "Place using directives in GlobalUsings.cs only",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Using directives must appear only in GlobalUsings.cs.");

    /// <summary>CSV006 multiple exits per line.</summary>
    public static readonly DiagnosticDescriptor MultipleExitsPerLine = new(
        DiagnosticIds.MultipleExitsPerLine,
        "Multiple exit points on one line",
        "Callable '{0}' has {1} exit points on line {2} ({3}); use at most one exit per line",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each callable may have at most one exit point per source line.",
        customTags: "CompilationEnd");

    /// <summary>CSV007 volatile field access.</summary>
    public static readonly DiagnosticDescriptor VolatileFieldAccess = new(
        DiagnosticIds.VolatileFieldAccess,
        "Plain volatile field access",
        "Use Volatile.Read, Volatile.Write, or Interlocked for volatile field '{0}'",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Volatile fields must be accessed through Volatile or Interlocked APIs.");

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
    };
}
