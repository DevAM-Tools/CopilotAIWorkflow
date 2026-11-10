// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ReqTrace.Tests.Helpers;

/// <summary>A throw-away repository for one test. Parallel-safe: each instance has its own folder.</summary>
public sealed class TraceWorkspace : IDisposable
{
    /// <summary>Quoted property name of a marker, kept apart so that the scanner does not read this file as a test.</summary>
    public const string CardProperty = "\"Card\"";

    /// <summary>Creates a repository with an empty spec.</summary>
    public TraceWorkspace()
    {
        Root = Path.Join(Path.GetTempPath(), "reqtrace-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(Path.Join(Root, "spec", "requirements"));
        Directory.CreateDirectory(Path.Join(Root, "spec", "tests"));
        File.WriteAllText(Path.Join(Root, "spec", "index.md"), "# Spec\n");
    }

    /// <summary>Gets the repository root.</summary>
    public string Root { get; }

    /// <summary>Returns a well-formed requirement entry.</summary>
    /// <param name="id">Requirement ID.</param>
    /// <param name="text">Obligation.</param>
    /// <param name="met">Met mark.</param>
    /// <param name="proof">Proof value.</param>
    /// <returns>The entry text.</returns>
    public static string Requirement(string id, string text = "The system shall work.", string met = "⬜", string proof = "test")
    {
        return Invariant($"- **{id}** — {text}\n  - **Done when:** A person sees it.\n  - **State:** active · **Met:** {met}\n  - **Proof:** {proof}\n");
    }

    /// <summary>Returns a well-formed test card.</summary>
    /// <param name="id">Card ID.</param>
    /// <param name="proves">Requirement ID.</param>
    /// <returns>The card text.</returns>
    public static string Card(string id, string proves)
    {
        return Invariant($"### {id}\n\n- **Proves:** {proves}\n- **Content:** A scenario.\n- **State:** active · **Status:** ⬜\n\n");
    }

    /// <summary>Returns a marked C# test.</summary>
    /// <param name="cardId">Card ID.</param>
    /// <param name="method">Method name.</param>
    /// <returns>The source text.</returns>
    public static string MarkedTest(string cardId, string method)
    {
        return Invariant($"[Test, Property({CardProperty}, \"{cardId}\")]\npublic async Task {method}()\n{{\n}}\n\n");
    }

    /// <summary>Writes the requirement file of an area.</summary>
    /// <param name="content">File body after the area heading.</param>
    /// <param name="area">Area file name.</param>
    public void WriteRequirements(string content, string area = "a")
    {
        Write(Invariant($"spec/requirements/{area}.md"), "### Area\n\n" + content);
    }

    /// <summary>Writes the test-card file of an area.</summary>
    /// <param name="content">File body.</param>
    /// <param name="area">Area file name.</param>
    public void WriteCards(string content, string area = "a")
    {
        Write(Invariant($"spec/tests/{area}.md"), content);
    }

    /// <summary>Writes a file below the root.</summary>
    /// <param name="relative">Relative path with forward slashes.</param>
    /// <param name="content">File content.</param>
    public void Write(string relative, string content)
    {
        string path = Path.Join(Root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    /// <summary>Deletes a file below the root.</summary>
    /// <param name="relative">Relative path with forward slashes.</param>
    public void Delete(string relative)
    {
        File.Delete(Path.Join(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>Runs the tool in this repository.</summary>
    /// <param name="args">Arguments.</param>
    /// <returns>Exit code and captured streams.</returns>
    public (int ExitCode, string Output, string Error) Run(params string[] args)
    {
        return RunFrom(Root, args);
    }

    /// <summary>Runs the tool from a given directory.</summary>
    /// <param name="directory">Working directory.</param>
    /// <param name="args">Arguments.</param>
    /// <returns>Exit code and captured streams.</returns>
    public static (int ExitCode, string Output, string Error) RunFrom(string directory, params string[] args)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = TraceApp.Run(args, directory, output, error);
        return (exitCode, output.ToString(), error.ToString());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(Root, true);
    }
}
