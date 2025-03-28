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

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace SonarAnalyzer.Core.Rules
{
    public abstract class CheckFileLicenseBase : ParametrizedDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1451";
        internal const string HeaderFormatPropertyKey = nameof(HeaderFormat);
        internal const string IsRegularExpressionPropertyKey = nameof(IsRegularExpression);
        protected const string HeaderFormatRuleParameterKey = "headerFormat";
        private const string IsRegularExpressionRuleParameterKey = "isRegularExpression";
        private const string IsRegularExpressionDefaultValue = "false";
        private const string MessageFormat = "Add or update the header of this file.";

        private readonly DiagnosticDescriptor rule;

        protected abstract ILanguageFacade Language { get; }
        public abstract string HeaderFormat { get; set; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        [RuleParameter(IsRegularExpressionRuleParameterKey, PropertyType.Boolean, "Whether the headerFormat is a regular expression.", IsRegularExpressionDefaultValue)]
        public bool IsRegularExpression { get; set; } = bool.Parse(IsRegularExpressionDefaultValue);

        protected CheckFileLicenseBase() =>
            rule = Language.CreateDescriptor(DiagnosticId, MessageFormat, isEnabledByDefault: false);

        protected override void Initialize(SonarParametrizedAnalysisContext context) =>
            context.RegisterTreeAction(Language.GeneratedCodeRecognizer, c =>
                {
                    if (HeaderFormat == null)
                    {
                        return;
                    }

                    if (IsRegularExpression && !IsRegexPatternValid(HeaderFormat))
                    {
                        throw new InvalidOperationException($"Invalid regular expression: {HeaderFormat}");
                    }

                    var firstNode = c.Tree.GetRoot().ChildTokens().FirstOrDefault().Parent;
                    if (!HasValidLicenseHeader(firstNode))
                    {
                        var properties = CreateDiagnosticProperties();
                        c.ReportIssue(rule, Location.Create(c.Tree, TextSpan.FromBounds(0, 0)), properties);
                    }
                });

        private static bool IsRegexPatternValid(string pattern)
        {
            try
            {
                Regex.Match(string.Empty, pattern, RegexOptions.None, Constants.DefaultRegexTimeout);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private bool HasValidLicenseHeader(SyntaxNode node)
        {
            if (node == null || !node.HasLeadingTrivia)
            {
                return false;
            }

            var trivias = node.GetLeadingTrivia();
            var header = trivias.ToString();
            return header != null && AreHeadersEqual(header);
        }

        private bool AreHeadersEqual(string currentHeader)
        {
            var unixEndingHeader = currentHeader.Replace("\r\n", "\n");
            var unixEndingHeaderFormat = HeaderFormat.Replace("\r\n", "\n").Replace("\\r\\n", "\n");
            if (!IsRegularExpression && !unixEndingHeaderFormat.EndsWith("\n"))
            {
                // In standard text mode, we want to be sure that the matched header is on its own line, with nothing else on the same line.
                unixEndingHeaderFormat += "\n";
            }
            return IsRegularExpression
                ? SafeRegex.IsMatch(unixEndingHeader, unixEndingHeaderFormat, RegexOptions.Singleline)
                : unixEndingHeader.StartsWith(unixEndingHeaderFormat, StringComparison.Ordinal);
        }

        private ImmutableDictionary<string, string> CreateDiagnosticProperties() =>
            ImmutableDictionary<string, string>.Empty
                .Add(HeaderFormatPropertyKey, HeaderFormat)
                .Add(IsRegularExpressionPropertyKey, IsRegularExpression.ToString());
    }
}
