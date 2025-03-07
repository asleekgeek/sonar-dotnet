﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2014-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Sonar Source-Available License Version 1, as published by SonarSource SA.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the Sonar Source-Available License for more details.
 *
 * You should have received a copy of the Sonar Source-Available License
 * along with this program; if not, see https://sonarsource.com/license/ssal/
 */

namespace SonarAnalyzer.Core.Syntax.Utilities;

/// <summary>
/// Class to determine if visually one line is more indented than another.
/// </summary>
/// <remarks>
/// If the strings contain a mix of tab and non-tab characters then the text that appears most indented will depend on what the tab spacing used by IDE is.
/// </remarks>
internal static class VisualIndentComparer
{
    public static bool IsSecondIndentLonger(SyntaxNode node1, SyntaxNode node2)
    {
        var node1LinePosition = node1.GetLocation().GetLineSpan().StartLinePosition;
        var node2LinePosition = node2.GetLocation().GetLineSpan().StartLinePosition;

        var lines = node1.SyntaxTree.GetText().Lines;
        var indentText1 = lines[node1LinePosition.Line].ToString().Substring(0, node1LinePosition.Character);
        var indentText2 = lines[node2LinePosition.Line].ToString().Substring(0, node2LinePosition.Character);

        return IsSecondIndentLonger(indentText1, indentText2);
    }

    /// <summary>
    /// Returns true if it seems likely that the second indent will appear visually longer than the first. The method will only return false if it there is a high
    /// degree of confidence that the second input is definitely the same length or shorter (i.e. low number of false positives).
    /// </summary>
    public static bool IsSecondIndentLonger(string indent1, string indent2)
    {
        var tabCount1 = TabCount(indent1);
        var tabCount2 = TabCount(indent2);

        // If the number of indent tabs is the same then it's safe just to use the absolute number of charaters
        if (tabCount1 == tabCount2)
        {
            return indent2.Length > indent1.Length;
        }
        else if (tabCount1 > tabCount2 && indent1.Length >= indent2.Length)
        {
            // More tabs in first and same or more characters overall -> second definitely shorter
            return false;
        }
        else
        {
            // The second string has more tabs. If it is also longer overall then we'll return true.
            // However, if it has more tabs but fewer letters, we're not sure - it depends on what the tab setting is.
            // Since we're only returning false if we're sure, we'll return true in that case too.
            return true;
        }
    }

    private static int TabCount(string text) =>
        text.Count(x => x == '\t');
}
