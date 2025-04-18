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

namespace SonarAnalyzer.CSharp.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnaryPrefixOperatorRepeated :
        UnaryPrefixOperatorRepeatedBase<SyntaxKind, PrefixUnaryExpressionSyntax>
    {
        protected override DiagnosticDescriptor Rule { get; } =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        protected override ISet<SyntaxKind> SyntaxKinds { get; } = new HashSet<SyntaxKind>
        {
            SyntaxKind.LogicalNotExpression,
            SyntaxKind.BitwiseNotExpression,
        };

        protected override GeneratedCodeRecognizer GeneratedCodeRecognizer { get; } =
            CSharpGeneratedCodeRecognizer.Instance;

        protected override SyntaxNode GetOperand(PrefixUnaryExpressionSyntax unarySyntax) =>
            unarySyntax.Operand;

        protected override SyntaxToken GetOperatorToken(PrefixUnaryExpressionSyntax unarySyntax) =>
            unarySyntax.OperatorToken;

        protected override bool SameOperators(PrefixUnaryExpressionSyntax expression1, PrefixUnaryExpressionSyntax expression2) =>
            expression1.OperatorToken.IsKind(expression2.OperatorToken.Kind());
    }
}
