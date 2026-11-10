// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace;

/// <summary>Process entry point.</summary>
internal static class Program
{
    /// <summary>Runs the tool.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Perf: Console.Out flushes on every write. One buffered writer, flushed once, avoids a system call per line.
        using StreamWriter output = new(Console.OpenStandardOutput(), new UTF8Encoding(false), 65536);
        using StreamWriter error = new(Console.OpenStandardError(), new UTF8Encoding(false), 4096);
        return TraceApp.Run(args, Environment.CurrentDirectory, output, error);
    }
}
