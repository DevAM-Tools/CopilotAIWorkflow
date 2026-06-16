// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Manifest export command.</summary>
/// <remarks>CLI surface verified by CLI integration tests.</remarks>
[ExcludeFromCodeCoverage]
internal static class ManifestCommand
{
    private static readonly JsonSerializerOptions _JsonOptions = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (!CommandLineOptions.TryParseProjectCommand(args, out string? projectPath, out CommandLineFlags flags, out string? parseError))
        {
            await Console.Error.WriteLineAsync(parseError).ConfigureAwait(false);
            return 1;
        }

        if (!CommandLineOptions.IsValidManifestFormat(flags.Format))
        {
            await Console.Error.WriteLineAsync($"Unknown manifest format: {flags.Format}. Use agent or text.").ConfigureAwait(false);
            return 1;
        }

        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            await Console.Error.WriteLineAsync($"Project file not found: {projectPath}").ConfigureAwait(false);
            return 1;
        }

        (Compilation Compilation, IReadOnlyList<ExitPointEntry> Exits)? loaded =
            await ProjectExitLoader.TryLoadAsync(projectPath, flags, cancellationToken).ConfigureAwait(false);
        if (loaded is null)
        {
            return 1;
        }

        IReadOnlyList<ExitPointEntry> exits = loaded.Value.Exits;
        string output = flags.Format.Equals("text", StringComparison.OrdinalIgnoreCase)
            ? _FormatText(exits)
            : JsonSerializer.Serialize(exits, _JsonOptions);

        await OutputWriter.WriteAsync(output, flags.OutputPath, appendNewLine: true, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static string _FormatText(IReadOnlyList<ExitPointEntry> exits)
    {
        StringBuilder builder = new StringBuilder();
        for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
        {
            ExitPointEntry exit = exits[exitIndex];
            builder.Append(exit.ExitPointId)
                .Append(' ')
                .Append(exit.FilePath)
                .Append(':')
                .Append(exit.Line)
                .Append(':')
                .Append(exit.Column)
                .Append(' ')
                .Append(exit.MethodDisplayName)
                .Append(' ')
                .AppendLine(exit.Kind.ToString());
        }

        return builder.ToString();
    }
}

