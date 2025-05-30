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
    public sealed class SwitchCasesMinimumThree : SwitchCasesMinimumThreeBase
    {
        private enum SwitchExpressionType
        {
            SingleReturnValue,
            TwoReturnValues,
            ManyReturnValues
        }

        private const string SwitchStatementMessage = "Replace this 'switch' statement with 'if' statements to increase readability.";

        private const string TwoReturnValueSwitchExpressionMessage = "Replace this 'switch' expression with a ternary conditional operator to increase readability.";

        private const string SingleReturnValueSwitchExpressionMessage = "Remove this 'switch' expression to increase readability.";

        private static readonly DiagnosticDescriptor rule = DescriptorFactory.Create(DiagnosticId, "{0}");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(
                c =>
                {
                    var switchNode = (SwitchStatementSyntax)c.Node;
                    if (!HasAtLeastThreeLabels(switchNode))
                    {
                        c.ReportIssue(rule, switchNode.SwitchKeyword, SwitchStatementMessage);
                    }
                },
                SyntaxKind.SwitchStatement);

            context.RegisterNodeAction(
                c =>
                {
                    var switchNode = (SwitchExpressionSyntaxWrapper)c.Node;
                    var message = EvaluateType(switchNode) switch
                    {
                        SwitchExpressionType.SingleReturnValue => SingleReturnValueSwitchExpressionMessage,
                        SwitchExpressionType.TwoReturnValues => TwoReturnValueSwitchExpressionMessage,
                        _ => string.Empty
                    };
                    if (message != string.Empty)
                    {
                        c.ReportIssue(rule, switchNode.SwitchKeyword, message);
                    }
                },
                SyntaxKindEx.SwitchExpression);
        }

        private static bool HasAtLeastThreeLabels(SwitchStatementSyntax node) =>
            node.Sections.Sum(section => section.Labels.Count) >= 3;

        private static SwitchExpressionType EvaluateType(SwitchExpressionSyntaxWrapper switchExpression)
        {
            var numberOfArms = switchExpression.Arms.Count;
            if (numberOfArms > 2)
            {
                return SwitchExpressionType.ManyReturnValues;
            }
            var hasDiscardValue = switchExpression.HasDiscardPattern();
            if (numberOfArms == 2)
            {
                return hasDiscardValue ? SwitchExpressionType.TwoReturnValues : SwitchExpressionType.ManyReturnValues;
            }
            if (numberOfArms == 1)
            {
                return hasDiscardValue ? SwitchExpressionType.SingleReturnValue : SwitchExpressionType.TwoReturnValues;
            }
            return SwitchExpressionType.SingleReturnValue;
        }
    }
}
