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

namespace SonarAnalyzer.CSharp.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CreatingHashAlgorithms : CreatingHashAlgorithmsBase<SyntaxKind>
{
    protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

    public CreatingHashAlgorithms() : this(AnalyzerConfiguration.Hotspot) { }

    internal /*for testing*/ CreatingHashAlgorithms(IAnalyzerConfiguration configuration) : base(configuration) { }

    protected override bool IsUnsafeAlgorithm(SyntaxNode argumentNode, SemanticModel model) =>
        argumentNode as ArgumentSyntax is { } argument
        && argument.Expression as MemberAccessExpressionSyntax is { } memberAccess
        && memberAccess.Name.ToString() is "SHA1" or "MD5"
        && model.GetSymbolInfo(memberAccess.Expression).Symbol.GetSymbolType().Is(KnownType.System_Security_Cryptography_HashAlgorithmName);
}
