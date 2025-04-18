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
    public sealed class DoNotTestThisWithIsOperator : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S3060";
        private const string MessageFormat = "Offload the code that's conditional on this type test to the appropriate subclass and remove the condition.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(AnalyzeIsExpression, SyntaxKind.IsExpression);
            context.RegisterNodeAction(AnalyzeIsPatternExpression, SyntaxKindEx.IsPatternExpression);
            context.RegisterNodeAction(AnalyzeSwitchExpression, SyntaxKindEx.SwitchExpression);
            context.RegisterNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        }

        private static void AnalyzeIsExpression(SonarSyntaxNodeReportingContext context)
        {
            if (IsThisExpressionSyntax(((BinaryExpressionSyntax)context.Node).Left))
            {
                ReportDiagnostic(context, context.Node);
            }
        }

        private static void AnalyzeIsPatternExpression(SonarSyntaxNodeReportingContext context)
        {
            if (IsThisExpressionSyntax(((IsPatternExpressionSyntaxWrapper)context.Node).Expression) && ContainsTypeCheckInPattern(context.Node))
            {
                ReportDiagnostic(context, context.Node);
            }
        }

        private static void AnalyzeSwitchStatement(SonarSyntaxNodeReportingContext context)
        {
            var switchStatement = (SwitchStatementSyntax)context.Node;
            if (IsThisExpressionSyntax(switchStatement.Expression))
            {
                ReportDiagnostic(context, switchStatement.Expression, CollectSecondaryLocations(switchStatement));
            }
        }

        private static void AnalyzeSwitchExpression(SonarSyntaxNodeReportingContext context)
        {
            var switchExpression = (SwitchExpressionSyntaxWrapper)context.Node;
            if (IsThisExpressionSyntax(switchExpression.GoverningExpression))
            {
                 ReportDiagnostic(context, switchExpression.GoverningExpression, CollectSecondaryLocations(switchExpression));
            }
        }

        private static IList<SecondaryLocation> CollectSecondaryLocations(SwitchStatementSyntax switchStatement) =>
            switchStatement.Sections
                .SelectMany(section => section.Labels
                    .Where(label => ContainsTypeCheckInPattern(label) || ContainsTypeCheckInCaseSwitchLabel(label))
                    .Select(label => new SecondaryLocation(TypeMatchLocation(label), string.Empty)))
                .ToList();

        private static IList<SecondaryLocation> CollectSecondaryLocations(SwitchExpressionSyntaxWrapper switchExpression) =>
            switchExpression.Arms.Where(arm => ContainsTypeCheckInPattern(arm.Pattern.SyntaxNode))
                .Select(arm => new SecondaryLocation(arm.Pattern.SyntaxNode.GetLocation(), string.Empty))
                .ToList();

        private static bool ContainsTypeCheckInCaseSwitchLabel(SwitchLabelSyntax switchLabel) =>
            switchLabel is CaseSwitchLabelSyntax caseSwitchLabel && caseSwitchLabel.Value.IsKind(SyntaxKind.IdentifierName);

        private static bool ContainsTypeCheckInPattern(SyntaxNode node) =>
            node.DescendantNodesAndSelf()
                .Any(x =>
                    x.Kind() is SyntaxKindEx.ConstantPattern or SyntaxKindEx.DeclarationPattern or SyntaxKindEx.RecursivePattern or SyntaxKindEx.ListPattern
                    && IsTypeCheckOnThis(x));

        private static bool IsTypeCheckOnThis(SyntaxNode pattern)
        {
            if (ConstantPatternSyntaxWrapper.IsInstance(pattern))
            {
                return ((ConstantPatternSyntaxWrapper)pattern).Expression.IsKind(SyntaxKind.IdentifierName) && IsNotInSubPattern(pattern);
            }
            else if (DeclarationPatternSyntaxWrapper.IsInstance(pattern))
            {
                return IsNotInSubPattern(pattern);
            }
            else if (RecursivePatternSyntaxWrapper.IsInstance(pattern))
            {
                return ((RecursivePatternSyntaxWrapper)pattern).Type != null && IsNotInSubPattern(pattern);
            }
            else if (ListPatternSyntaxWrapper.IsInstance(pattern))
            {
                return IsNotInSubPattern(pattern);
            }
            else
            {
                return false;
            }
        }

        private static bool IsNotInSubPattern(SyntaxNode node) =>
            !node.FirstAncestorOrSelf<SyntaxNode>(x => x?.Kind() is
                SyntaxKindEx.IsPatternExpression or
                SyntaxKindEx.SwitchExpression or
                SyntaxKind.SwitchStatement or
                SyntaxKindEx.Subpattern)
            .IsKind(SyntaxKindEx.Subpattern);

        private static void ReportDiagnostic(SonarSyntaxNodeReportingContext context, SyntaxNode node) =>
            context.ReportIssue(Rule, node);

        private static void ReportDiagnostic(SonarSyntaxNodeReportingContext context, SyntaxNode node, IList<SecondaryLocation> secondaryLocations)
        {
            if (secondaryLocations.Any())
            {
                context.ReportIssue(Rule, node, secondaryLocations);
            }
        }

        private static Location TypeMatchLocation(SwitchLabelSyntax label)
        {
            if (label is CaseSwitchLabelSyntax caseSwitchLabel)
            {
                return caseSwitchLabel.Value.GetLocation();
            }
            else if (CasePatternSwitchLabelSyntaxWrapper.IsInstance(label))
            {
                return ((CasePatternSwitchLabelSyntaxWrapper)label).Pattern.SyntaxNode.GetLocation();
            }
            else
            {
                return Location.None;
            }
        }

        private static bool IsThisExpressionSyntax(SyntaxNode syntaxNode) =>
            syntaxNode.RemoveParentheses() is ThisExpressionSyntax;
    }
}
