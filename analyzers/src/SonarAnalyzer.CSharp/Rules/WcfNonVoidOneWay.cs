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
    public sealed class WcfNonVoidOneWay : WcfNonVoidOneWayBase<MethodDeclarationSyntax, SyntaxKind>
    {
        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override GeneratedCodeRecognizer GeneratedCodeRecognizer =>
            CSharpGeneratedCodeRecognizer.Instance;

        protected override SyntaxKind MethodDeclarationKind =>
            SyntaxKind.MethodDeclaration;

        protected override Location GetReturnTypeLocation(MethodDeclarationSyntax method) =>
            method.ReturnType.GetLocation();
    }
}
