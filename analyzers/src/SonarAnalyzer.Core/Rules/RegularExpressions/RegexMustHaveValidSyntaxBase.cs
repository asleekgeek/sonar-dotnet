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

using SonarAnalyzer.Core.RegularExpressions;

namespace SonarAnalyzer.Core.Rules;

public abstract class RegexMustHaveValidSyntaxBase<TSyntaxKind> : SonarDiagnosticAnalyzer<TSyntaxKind>
    where TSyntaxKind : struct
{
    private const string DiagnosticId = "S5856";

    protected sealed override string MessageFormat => "Fix the syntax error inside this regex: {0}";

    protected RegexMustHaveValidSyntaxBase() : base(DiagnosticId) { }

    protected override void Initialize(SonarAnalysisContext context)
    {
        context.RegisterNodeAction(
            Language.GeneratedCodeRecognizer,
            c => Analyze(c, RegexContext.FromCtor(Language, c.Model, c.Node)),
            Language.SyntaxKind.ObjectCreationExpressions);

        context.RegisterNodeAction(
            Language.GeneratedCodeRecognizer,
            c => Analyze(c, RegexContext.FromMethod(Language, c.Model, c.Node)),
            Language.SyntaxKind.InvocationExpression);

        context.RegisterNodeAction(
            Language.GeneratedCodeRecognizer,
            c => Analyze(c, RegexContext.FromAttribute(Language, c.Model, c.Node)),
            Language.SyntaxKind.Attribute);
    }

    private void Analyze(SonarSyntaxNodeReportingContext c, RegexContext context)
    {
        if (context?.ParseError is { } error)
        {
            c.ReportIssue(Rule, context.PatternNode, error.Message);
        }
    }
}
