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

namespace SonarAnalyzer.VisualBasic.Rules;

[DiagnosticAnalyzer(LanguageNames.VisualBasic)]
public sealed class ToStringShouldNotReturnNull : ToStringShouldNotReturnNullBase<SyntaxKind>
{
    protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;

    protected override SyntaxKind MethodKind => SyntaxKind.FunctionBlock;

    protected override IEnumerable<SyntaxNode> Conditionals(SyntaxNode expression) =>
        expression is TernaryConditionalExpressionSyntax conditional
            ? new SyntaxNode[] { conditional.WhenTrue, conditional.WhenFalse }
            : Array.Empty<SyntaxNode>();

    protected override bool IsLocalOrLambda(SyntaxNode node) =>
        node.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression);
}
