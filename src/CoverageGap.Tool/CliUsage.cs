// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>CLI usage text.</summary>
[ExcludeFromCodeCoverage]
internal static class CliUsage
{
    public static async Task WriteAsync()
    {
        await Console.Error.WriteLineAsync(
            """
            Usage:
              coveragegap [run] [target] [options]
              coveragegap plan [target] [options]

            Targets (default: solution in --repo-root):
              (none)                    Auto-discover .slnx or .sln in repo root
              solution [path.slnx]      Named or auto-discovered solution
              project <path.csproj>...  One or more production projects

            Options:
              --repo-root <path>        Repository root (default: current directory)
              --configuration <cfg>     Build/test configuration (default: Release)
              --format agent|compact|text
              -o <path>                 Output file or directory (relative paths under --work-dir)
              --work-dir <path>         Isolated run directory (default: unique temp folder)
              --test-project <path>     Override paired test project (single project target)
              --cobertura <file>        Skip test run; use Cobertura file (run only, repeatable)
              --include-snippet         Include source snippets in gap output
              --no-fail                 Exit 0 even when gate fails
              --no-build                Skip dotnet build before test/compile
              --skip-no-tests           On run only: skip production projects without a paired test project
              --allow-empty-coverage    Non-release bootstrap only
              --keep-work-dir           Do not delete the work directory after completion
            """).ConfigureAwait(false);
    }
}
