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

namespace SonarAnalyzer.VisualBasic.Rules
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public sealed class BooleanCheckInverted : BooleanCheckInvertedBase<BinaryExpressionSyntax>
    {
        private static readonly ISet<SyntaxKind> ignoredNullableOperators =
            new HashSet<SyntaxKind>
            {
                SyntaxKind.GreaterThanToken,
                SyntaxKind.GreaterThanEqualsToken,
                SyntaxKind.LessThanToken,
                SyntaxKind.LessThanEqualsToken,
            };

        private static readonly Dictionary<SyntaxKind, string> oppositeTokens =
            new Dictionary<SyntaxKind, string>
            {
                { SyntaxKind.GreaterThanToken, "<=" },
                { SyntaxKind.GreaterThanEqualsToken, "<" },
                { SyntaxKind.LessThanToken, ">=" },
                { SyntaxKind.LessThanEqualsToken, ">" },
                { SyntaxKind.EqualsToken, "<>" },
                { SyntaxKind.LessThanGreaterThanToken, "=" },
            };

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(
                GetAnalysisAction(rule),
                SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanOrEqualExpression,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression);

        protected override bool IsIgnoredNullableOperation(BinaryExpressionSyntax expression, SemanticModel semanticModel) =>
            expression.OperatorToken.IsAnyKind(ignoredNullableOperators) &&
            (IsNullable(expression.Left, semanticModel) || IsNullable(expression.Right, semanticModel) ||
            IsConditionalAccessExpression(expression.Left) || IsConditionalAccessExpression(expression.Right));

        private static bool IsConditionalAccessExpression(ExpressionSyntax expression) =>
            expression.RemoveParentheses().IsKind(SyntaxKind.ConditionalAccessExpression);

        protected override bool IsLogicalNot(BinaryExpressionSyntax expression, out SyntaxNode logicalNot)
        {
            var parenthesizedParent = expression.GetSelfOrTopParenthesizedExpression().Parent;
            var unaryExpression = parenthesizedParent as UnaryExpressionSyntax;

            logicalNot = unaryExpression;

            return unaryExpression != null
                && unaryExpression.OperatorToken.IsKind(SyntaxKind.NotKeyword);
        }

        protected override string GetSuggestedReplacement(BinaryExpressionSyntax expression) =>
            oppositeTokens[expression.OperatorToken.Kind()];
    }
}
