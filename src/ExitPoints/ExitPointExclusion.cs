// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ExitPoints;

/// <summary>Detects Roslyn syntax excluded from code coverage.</summary>
public static class ExitPointExclusion
{
    /// <summary>Returns whether an exit at the given position is excluded by attribute.</summary>
    /// <param name="tree">Syntax tree containing the exit.</param>
    /// <param name="line">One-based line of the exit.</param>
    /// <param name="column">One-based column of the exit.</param>
    /// <returns><see langword="true"/> when excluded.</returns>
    /// <remarks>Columns beyond the line end resolve to the line start for attribute lookup.</remarks>
    public static bool IsExcludedAtPosition(SyntaxTree tree, int line, int column)
    {
        if (tree is null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        SourceText text = tree.GetText();
        int lineIndex = line - 1;
        if (lineIndex < 0 || lineIndex >= text.Lines.Count)
        {
            return false;
        }

        TextLine textLine = text.Lines[lineIndex];
        // Columns beyond the line end resolve to the line start for attribute lookup.
        int position = textLine.Start + Math.Max(0, column - 1);
        if (position > text.Length)
        {
            position = textLine.Start;
        }

        SyntaxNode node = tree.GetRoot().FindNode(TextSpan.FromBounds(position, position));

        foreach (SyntaxNode ancestor in node.AncestorsAndSelf())
        {
            if (ancestor is BaseTypeDeclarationSyntax typeDeclaration
                && _HasExcludeAttribute(typeDeclaration.AttributeLists))
            {
                return true;
            }

            if (ancestor is MethodDeclarationSyntax method
                && _HasExcludeAttribute(method.AttributeLists))
            {
                return true;
            }

            if (ancestor is LocalFunctionStatementSyntax localFunction
                && _HasExcludeAttribute(localFunction.AttributeLists))
            {
                return true;
            }

            if (ancestor is AccessorDeclarationSyntax accessor
                && _HasExcludeAttribute(accessor.AttributeLists))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns whether attribute lists contain <c>ExcludeFromCodeCoverage</c>.</summary>
    /// <param name="attributeLists">Syntax attribute lists.</param>
    /// <returns><see langword="true"/> when excluded.</returns>
    public static bool HasExcludeAttribute(SyntaxList<AttributeListSyntax> attributeLists)
    {
        return _HasExcludeAttribute(attributeLists);
    }

    private static bool _HasExcludeAttribute(SyntaxList<AttributeListSyntax> attributeLists)
    {
        for (int listIndex = 0; listIndex < attributeLists.Count; listIndex++)
        {
            SeparatedSyntaxList<AttributeSyntax> attributes = attributeLists[listIndex].Attributes;
            for (int attributeIndex = 0; attributeIndex < attributes.Count; attributeIndex++)
            {
                string name = attributes[attributeIndex].Name.ToString();
                if (name.EndsWith("ExcludeFromCodeCoverage", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

