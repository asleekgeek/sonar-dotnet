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

namespace SonarAnalyzer.CSharp.Styling.Rules.Test;

[TestClass]
public class HelperInTypeNameTest
{
    private readonly VerifierBuilder builder = StylingVerifierBuilder.Create<HelperInTypeName>().WithConcurrentAnalysis(false);

    [TestMethod]
    public void HelperInTypeName() =>
        builder.AddPaths("HelperInTypeName.cs").Verify();

    [TestMethod]
    public void HelperInTypeName_FileScopedNamespace() =>
        builder.AddSnippet("namespace Something.Helpers;    // Noncompliant").Verify();
}
