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

using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.CSharp.Metrics;

namespace SonarAnalyzer.CSharp.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FunctionComplexity : ParametrizedDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S1541";
        private const string MessageFormat = "The Cyclomatic Complexity of this {2} is {1} which is greater than {0} authorized.";
        private const int DefaultValueMaximum = 10;

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat, isEnabledByDefault: false);

        [RuleParameter("maximumFunctionComplexityThreshold", PropertyType.Integer, "The maximum authorized complexity.", DefaultValueMaximum)]
        public int Maximum { get; set; } = DefaultValueMaximum;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarParametrizedAnalysisContext context)
        {
            context.RegisterNodeAction(c =>
                {
                    if (c.IsTopLevelMain())
                    {
                        CheckComplexity<CompilationUnitSyntax>(c, m => Location.Create(c.Node.SyntaxTree, TextSpan.FromBounds(0, 0)), "top-level file", true);
                    }
                },
                SyntaxKind.CompilationUnit);

            context.RegisterNodeAction(
                c => CheckComplexity<MethodDeclarationSyntax>(c, m => m.Identifier.GetLocation(), "method"),
                SyntaxKind.MethodDeclaration);

            context.RegisterNodeAction(
                c => CheckComplexity<PropertyDeclarationSyntax>(c, p => p.Identifier.GetLocation(), p => p.ExpressionBody, "property"),
                SyntaxKind.PropertyDeclaration);

            context.RegisterNodeAction(
                c => CheckComplexity<OperatorDeclarationSyntax>(c, o => o.OperatorKeyword.GetLocation(), "operator"),
                SyntaxKind.OperatorDeclaration);

            context.RegisterNodeAction(
                c => CheckComplexity<ConstructorDeclarationSyntax>(c, co => co.Identifier.GetLocation(), "constructor"),
                SyntaxKind.ConstructorDeclaration);

            context.RegisterNodeAction(
                c => CheckComplexity<DestructorDeclarationSyntax>(c, d => d.Identifier.GetLocation(), "destructor"),
                SyntaxKind.DestructorDeclaration);

            context.RegisterNodeAction(c =>
            {
                if (((LocalFunctionStatementSyntaxWrapper)c.Node).Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    CheckComplexity<SyntaxNode>(c, d => ((LocalFunctionStatementSyntaxWrapper)d).Identifier.GetLocation(), "static local function");
                }
            },
            SyntaxKindEx.LocalFunctionStatement);

            context.RegisterNodeAction(
                c => CheckComplexity<AccessorDeclarationSyntax>(c, a => a.Keyword.GetLocation(), "accessor"),
                SyntaxKind.GetAccessorDeclaration,
                SyntaxKind.SetAccessorDeclaration,
                SyntaxKind.AddAccessorDeclaration,
                SyntaxKind.RemoveAccessorDeclaration,
                SyntaxKindEx.InitAccessorDeclaration);
        }

        private void CheckComplexity<TSyntax>(SonarSyntaxNodeReportingContext context, Func<TSyntax, Location> getLocation, string declarationType, bool onlyGlobalStatements = false)
            where TSyntax : SyntaxNode =>
            CheckComplexity(context, getLocation, n => n, declarationType, onlyGlobalStatements);

        private void CheckComplexity<TSyntax>(
            SonarSyntaxNodeReportingContext context,
            Func<TSyntax, Location> getLocation,
            Func<TSyntax, SyntaxNode> getNodeToCheck,
            string declarationType,
            bool onlyGlobalStatements = false)
            where TSyntax : SyntaxNode
        {
            var node = (TSyntax)context.Node;

            var nodeToCheck = getNodeToCheck(node);
            if (nodeToCheck == null)
            {
                return;
            }

            var complexityMetric = CSharpCyclomaticComplexityMetric.GetComplexity(nodeToCheck, onlyGlobalStatements);
            if (complexityMetric.Complexity > Maximum)
            {
                context.ReportIssue(Rule, getLocation(node), complexityMetric.Locations, Maximum.ToString(), complexityMetric.Complexity.ToString(), declarationType);
            }
        }
    }
}
