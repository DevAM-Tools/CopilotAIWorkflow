// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Text;

namespace CSharpStyleChecker.Helpers;

/// <summary>Masks strings and comments for line-length measurement.</summary>
public static class LineMasking
{
    /// <summary>Masks strings and comments; string/comment content does not contribute to length.</summary>
    /// <param name="line">The source line.</param>
    /// <param name="inBlockComment">Whether the line continues a block comment from a prior line.</param>
    /// <returns>Masked line and updated block-comment state.</returns>
    public static (string Masked, bool InBlockComment) MaskLine(string line, bool inBlockComment)
    {
        if (line is null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        StringBuilder masked = new StringBuilder(line.Length);
        bool inBlock = inBlockComment;
        char? inString = null;
        bool verbatim = false;

        for (int i = 0; i < line.Length; i++)
        {
            if (inBlock)
            {
                if (i + 1 < line.Length && line[i] == '*' && line[i + 1] == '/')
                {
                    i++;
                    inBlock = false;
                }

                continue;
            }

            if (inString.HasValue)
            {
                char ch = line[i];
                if (verbatim)
                {
                    if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (ch == '"')
                    {
                        masked.Append('"');
                        inString = null;
                        verbatim = false;
                        continue;
                    }

                    continue;
                }

                if (ch == '\\')
                {
                    i += i + 1 < line.Length ? 1 : 0;
                    continue;
                }

                if (ch == inString.Value)
                {
                    masked.Append(ch);
                    inString = null;
                }

                continue;
            }

            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
            {
                break;
            }

            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
            {
                i++;
                inBlock = true;
                continue;
            }

            char current = line[i];
            if (current == '@')
            {
                char next = i + 1 < line.Length ? line[i + 1] : '\0';
                if (next == '"')
                {
                    masked.Append('@');
                    masked.Append('"');
                    inString = '"';
                    verbatim = true;
                    i++;
                    continue;
                }

                if (next == '$' && i + 2 < line.Length && line[i + 2] == '"')
                {
                    masked.Append('@');
                    masked.Append('$');
                    masked.Append('"');
                    inString = '"';
                    verbatim = true;
                    i += 2;
                    continue;
                }
            }

            if (current == '$' && i + 1 < line.Length && line[i + 1] == '"')
            {
                masked.Append('$');
                masked.Append('"');
                inString = '"';
                i++;
                continue;
            }

            if (current == '"')
            {
                masked.Append('"');
                inString = '"';
                continue;
            }

            if (current == '\'')
            {
                masked.Append('\'');
                inString = '\'';
                continue;
            }

            masked.Append(current);
        }

        return (masked.ToString(), inBlock);
    }
}

