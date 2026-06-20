// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Production project paired with an optional test project.</summary>
/// <param name="ProjectPath">Absolute path to the production <c>.csproj</c>.</param>
/// <param name="Name">Project file name without extension.</param>
/// <param name="TestProjectPath">Absolute path to paired test project, if any.</param>
internal sealed record ProductionProjectRecord(string ProjectPath, string Name, string? TestProjectPath);
