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
    public sealed class StringOperationWithoutCulture : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1449";
        private const string MessageFormat = "{0}";
        internal const string MessageDefineLocale = "Define the locale to be used in this string operation.";
        internal const string MessageChangeCompareTo = "Use 'CompareOrdinal' or 'Compare' with the locale specified instead of 'CompareTo'.";

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(
                ReportOnViolation,
                SyntaxKind.InvocationExpression);
        }

        private static void ReportOnViolation(SonarSyntaxNodeReportingContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            if (!(context.Model.GetSymbolInfo(invocation).Symbol is IMethodSymbol calledMethod))
            {
                return;
            }

            if (context.IsInExpressionTree())
            {
                return; // We cannot specify the culture in an expression tree
            }

            if (calledMethod.IsInType(KnownType.System_String) &&
                CommonCultureSpecificMethodNames.Contains(calledMethod.Name) &&
                !calledMethod.Parameters.Any(param => param.Type.IsAny(StringCultureSpecifierNames)))
            {
                context.ReportIssue(rule, memberAccess.Name, MessageDefineLocale);
                return;
            }

            if (calledMethod.IsInType(KnownType.System_String) &&
                IndexLookupMethodNames.Contains(calledMethod.Name) &&
                calledMethod.Parameters.Any(param => param.Type.SpecialType == SpecialType.System_String) &&
                !calledMethod.Parameters.Any(param => param.Type.IsAny(StringCultureSpecifierNames)))
            {
                context.ReportIssue(rule, memberAccess.Name, MessageDefineLocale);
                return;
            }

            if (IsMethodOnNonIntegralOrDateTime(calledMethod) &&
                calledMethod.Name == ToStringMethodName &&
                calledMethod.Parameters.Length == 0)
            {
                context.ReportIssue(rule, memberAccess.Name, MessageDefineLocale);
                return;
            }

            if (calledMethod.IsInType(KnownType.System_String) &&
                calledMethod.Name == CompareToMethodName)
            {
                context.ReportIssue(rule, memberAccess.Name, MessageChangeCompareTo);
            }
        }

        private static bool IsMethodOnNonIntegralOrDateTime(IMethodSymbol methodSymbol)
        {
            return methodSymbol.IsInType(KnownType.NonIntegralNumbers) ||
                methodSymbol.IsInType(KnownType.System_DateTime);
        }

        private static readonly ISet<string> CommonCultureSpecificMethodNames = new HashSet<string> { "ToLower", "ToUpper", "Compare" };

        private static readonly ISet<string> IndexLookupMethodNames = new HashSet<string> { "IndexOf", "LastIndexOf" };

        private const string CompareToMethodName = "CompareTo";
        private const string ToStringMethodName = "ToString";

        private static readonly ImmutableArray<KnownType> StringCultureSpecifierNames =
            ImmutableArray.Create(
                KnownType.System_Globalization_CultureInfo,
                KnownType.System_Globalization_CompareOptions,
                KnownType.System_StringComparison
            );
    }
}
