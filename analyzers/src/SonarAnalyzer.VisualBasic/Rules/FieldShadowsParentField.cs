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
    public sealed class FieldShadowsParentField : FieldShadowsParentFieldBase<SyntaxKind, ModifiedIdentifierSyntax>
    {
        protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(c =>
                {
                    var fieldDeclaration = (FieldDeclarationSyntax)c.Node;
                    if (!fieldDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.ShadowsKeyword)))
                    {
                        foreach (var diagnostics in fieldDeclaration.Declarators.SelectMany(x => x.Names).SelectMany(x => CheckFields(c.Model, x)))
                        {
                            c.ReportIssue(diagnostics);
                        }
                    }
                },
                SyntaxKind.FieldDeclaration);
    }
}
