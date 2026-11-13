// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>The commands of the tool.</summary>
internal enum TraceCommand
{
    /// <summary>Checks the repository.</summary>
    Check,

    /// <summary>Shows one entry and its links.</summary>
    Show,

    /// <summary>Prints the report for a person.</summary>
    Report,

    /// <summary>Records the current state as accepted.</summary>
    Accept,

    /// <summary>Confirms one test card after a requirement change.</summary>
    Confirm,

    /// <summary>Prints the expected spec forms; reads and writes no file.</summary>
    Syntax,
}

/// <summary>The sections the syntax command prints.</summary>
internal enum SyntaxKind
{
    /// <summary>All four sections, in the order below.</summary>
    All,

    /// <summary>A requirement entry and its field values.</summary>
    Requirement,

    /// <summary>A test card and its field values.</summary>
    Card,

    /// <summary>The test marker per language.</summary>
    Marker,

    /// <summary>A line of <c>spec/trace.ignore</c>.</summary>
    Ignore,
}

/// <summary>Parsed command line.</summary>
/// <param name="Command">The command.</param>
/// <param name="Id">The ID for show and confirm.</param>
/// <param name="ResultsPath">Result file or folder; null when none was given.</param>
/// <param name="Json">Whether check writes JSON.</param>
/// <param name="AllowGapReason">Reason that lets accept record new gaps.</param>
/// <param name="Note">What was checked, for confirm.</param>
/// <param name="Syntax">The sections for syntax; <see cref="SyntaxKind.All"/> for every other command.</param>
/// <remarks>Sync: docs/agents.md § Commands.</remarks>
internal sealed record CliOptions(
    TraceCommand Command,
    string? Id,
    string? ResultsPath,
    bool Json,
    string? AllowGapReason,
    string? Note,
    SyntaxKind Syntax)
{
    #region Exit codes and usage

    /// <summary>No error.</summary>
    public const int ExitOk = 0;

    /// <summary>At least one error finding.</summary>
    public const int ExitFindings = 1;

    /// <summary>Usage error or unreadable input.</summary>
    public const int ExitUsage = 2;

    /// <summary>Help text.</summary>
    public const string Usage = """
        reqtrace [check] [--results <file-or-folder>] [--json]
        reqtrace show <ID>
        reqtrace report
        reqtrace accept [--allow-gap "<reason>"]
        reqtrace confirm <TEST-ID> --note "<what was checked>"
        reqtrace syntax [requirement|card|marker|ignore]

        Examples:
          reqtrace check --results artifacts/trace-results
          reqtrace show REQ-EXPORT-CREATE
          reqtrace report
          reqtrace accept --allow-gap "test follows in the next slice"
          reqtrace confirm TEST-EXPORT-CREATE --note "still proves the new wording"
          reqtrace syntax card

        syntax prints the expected forms; it reads and writes no file and runs anywhere.
        Every other command runs from anywhere inside a repository that has spec/index.md.
        --results is relative to the repository root; a folder means every .trx below it.
        Exit codes: 0 no error, 1 at least one error, 2 usage or unreadable input.
        """;

    #endregion

    #region Parsing

    /// <summary>Parses the command line.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="options">Receives the options on success.</param>
    /// <param name="error">Receives the reason on failure.</param>
    /// <returns>Whether parsing succeeded.</returns>
    public static bool TryParse(string[] args, out CliOptions? options, out string error)
    {
        options = null;
        error = string.Empty;
        ArgumentNullException.ThrowIfNull(args);

        int index = 0;
        TraceCommand command = TraceCommand.Check;
        if (args.Length > 0 && !args[0].StartsWith('-'))
        {
            if (!_TryParseCommand(args[0], out command))
            {
                error = Invariant($"Unknown command: {args[0]}.");
                return false;
            }

            index = 1;
        }

        // syntax takes at most one kind and no option; anything else is a usage error, never a silent "print all".
        if (command == TraceCommand.Syntax)
        {
            return _TryParseSyntax(args, index, out options, out error);
        }

        string? id = null;
        if (command is TraceCommand.Show or TraceCommand.Confirm)
        {
            if (index >= args.Length || args[index].StartsWith('-'))
            {
                error = "This command needs an ID.";
                return false;
            }

            id = args[index];
            index++;
        }

        string? results = null;
        string? allowGap = null;
        string? note = null;
        bool json = false;
        for (; index < args.Length; index++)
        {
            string argument = args[index];
            if (command == TraceCommand.Check && string.Equals(argument, "--json", StringComparison.Ordinal))
            {
                json = true;
                continue;
            }

            string? name = command switch
            {
                TraceCommand.Check when string.Equals(argument, "--results", StringComparison.Ordinal) => argument,
                TraceCommand.Accept when string.Equals(argument, "--allow-gap", StringComparison.Ordinal) => argument,
                TraceCommand.Confirm when string.Equals(argument, "--note", StringComparison.Ordinal) => argument,
                _ => null,
            };
            if (name is null)
            {
                error = Invariant($"Unknown option: {argument}.");
                return false;
            }

            index++;
            if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            {
                error = Invariant($"{name} needs a value.");
                return false;
            }

            results = string.Equals(name, "--results", StringComparison.Ordinal) ? args[index] : results;
            allowGap = string.Equals(name, "--allow-gap", StringComparison.Ordinal) ? args[index] : allowGap;
            note = string.Equals(name, "--note", StringComparison.Ordinal) ? args[index] : note;
        }

        if (command == TraceCommand.Confirm && note is null)
        {
            error = "confirm needs --note \"what was checked\".";
            return false;
        }

        options = new(command, id, results, json, allowGap, note, SyntaxKind.All);
        return true;
    }

    /// <summary>Parses the arguments after the syntax command word.</summary>
    /// <param name="args">All arguments.</param>
    /// <param name="index">Index of the first argument after the command word.</param>
    /// <param name="options">Receives the options on success.</param>
    /// <param name="error">Receives the reason on failure; it names the four kinds.</param>
    /// <returns>Whether parsing succeeded.</returns>
    private static bool _TryParseSyntax(string[] args, int index, out CliOptions? options, out string error)
    {
        options = null;
        error = string.Empty;
        SyntaxKind kind = SyntaxKind.All;
        if (index < args.Length && !_TryParseSyntaxKind(args[index], out kind))
        {
            error = Invariant($"Unknown kind: {args[index]}. Use requirement, card, marker, or ignore.");
            return false;
        }

        if (index + 1 < args.Length)
        {
            error = Invariant($"Unknown option: {args[index + 1]}. ")
                + "syntax takes one kind: requirement, card, marker, or ignore.";
            return false;
        }

        options = new(TraceCommand.Syntax, null, null, false, null, null, kind);
        return true;
    }

    /// <summary>Maps a kind word of the syntax command.</summary>
    /// <param name="word">The word; compared ordinal, so only lowercase matches.</param>
    /// <param name="kind">Receives the kind.</param>
    /// <returns>Whether the word is a kind.</returns>
    private static bool _TryParseSyntaxKind(string word, out SyntaxKind kind)
    {
        (bool known, SyntaxKind parsed) = word switch
        {
            "requirement" => (true, SyntaxKind.Requirement),
            "card" => (true, SyntaxKind.Card),
            "marker" => (true, SyntaxKind.Marker),
            "ignore" => (true, SyntaxKind.Ignore),
            _ => (false, SyntaxKind.All),
        };
        kind = parsed;
        return known;
    }

    /// <summary>Maps a command word.</summary>
    /// <param name="word">The word.</param>
    /// <param name="command">Receives the command.</param>
    /// <returns>Whether the word is a command.</returns>
    private static bool _TryParseCommand(string word, out TraceCommand command)
    {
        (bool known, TraceCommand parsed) = word switch
        {
            "check" => (true, TraceCommand.Check),
            "show" => (true, TraceCommand.Show),
            "report" => (true, TraceCommand.Report),
            "accept" => (true, TraceCommand.Accept),
            "confirm" => (true, TraceCommand.Confirm),
            "syntax" => (true, TraceCommand.Syntax),
            _ => (false, TraceCommand.Check),
        };
        command = parsed;
        return known;
    }

    #endregion
}
