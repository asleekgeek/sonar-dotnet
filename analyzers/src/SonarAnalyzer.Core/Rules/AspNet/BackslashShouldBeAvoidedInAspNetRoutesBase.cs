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

namespace SonarAnalyzer.Core.Rules;

public abstract class BackslashShouldBeAvoidedInAspNetRoutesBase<TSyntaxKind> : SonarDiagnosticAnalyzer<TSyntaxKind>
    where TSyntaxKind : struct
{
    private const string DiagnosticId = "S6930";

    protected abstract TSyntaxKind[] SyntaxKinds { get; }
    protected abstract bool IsNamedAttributeArgument(SyntaxNode node);

    protected override string MessageFormat => @"Replace '\' with '/'.";

    protected BackslashShouldBeAvoidedInAspNetRoutesBase() : base(DiagnosticId) { }

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            if (compilationStartContext.Compilation.ReferencesNetCoreControllers()
                || compilationStartContext.Compilation.ReferencesNetFrameworkControllers())
            {
                compilationStartContext.RegisterNodeAction(Language.GeneratedCodeRecognizer, Check, SyntaxKinds);
            }
        });

    protected void Check(SonarSyntaxNodeReportingContext c)
    {
        if (!IsNamedAttributeArgument(c.Node)
            && Language.Syntax.NodeExpression(c.Node) is { } expression
            && Language.FindConstantValue(c.Model, expression) is string constantRouteTemplate
            && ContainsBackslash(constantRouteTemplate)
            && IsRouteTemplate(c.Model, c.Node))
        {
            c.ReportIssue(Rule, expression);
        }
    }

    private bool IsRouteTemplate(SemanticModel model, SyntaxNode node) =>
        node.Parent.Parent is var invocation // can be a method invocation or a tuple expression
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol
        && Language.MethodParameterLookup(invocation, methodSymbol) is { } parameterLookup
        && parameterLookup.TryGetSymbol(node, out var parameter)
        && (HasStringSyntaxAttributeOfTypeRoute(parameter) || IsRouteTemplateBeforeAspNet6(parameter, methodSymbol));

    private static bool HasStringSyntaxAttributeOfTypeRoute(IParameterSymbol parameter) =>
        parameter.GetAttributes(KnownType.System_Diagnostics_CodeAnalysis_StringSyntaxAttribute).FirstOrDefault() is { } syntaxAttribute
        && syntaxAttribute.TryGetAttributeValue<string>("syntax", out var syntaxParameter)
        && string.Equals(syntaxParameter, "Route", StringComparison.Ordinal);

    private static bool IsRouteTemplateBeforeAspNet6(IParameterSymbol parameter, IMethodSymbol method) =>
        // Remark: route templates cannot be specified via HttpXAttribute in ASP.NET 4.x
        (method.ContainingType.IsAny(KnownType.RouteAttributes)
            || method.ContainingType.DerivesFrom(KnownType.Microsoft_AspNetCore_Mvc_Routing_HttpMethodAttribute))
        && method.IsConstructor() && parameter.Name == "template";

    private static bool ContainsBackslash(string value)
    {
        var firstBackslashIndex = value.IndexOf('\\');
        if (firstBackslashIndex < 0)
        {
            return false;
        }

        var firstRegexIndex = value.IndexOf("regex", StringComparison.Ordinal);
        return firstRegexIndex < 0 || firstBackslashIndex < firstRegexIndex;
    }
}
