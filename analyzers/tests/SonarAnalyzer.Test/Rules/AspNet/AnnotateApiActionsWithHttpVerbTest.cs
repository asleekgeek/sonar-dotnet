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

#if NET
using SonarAnalyzer.CSharp.Rules;

namespace SonarAnalyzer.Test.Rules.AspNet;

[TestClass]
public class AnnotateApiActionsWithHttpVerbTest
{
    private static readonly VerifierBuilder Builder = new VerifierBuilder<AnnotateApiActionsWithHttpVerb>()
        .WithBasePath("AspNet")
        .AddReferences(
            [
                AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcCore,             // ControllerBase, ApiController, etc
                AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcViewFeatures,     // Controller
                AspNetCoreMetadataReference.MicrosoftAspNetCoreHttpAbstractions,    // StatusCodes
            ]);

    [TestMethod]
    public void AnnotateApiActionsWithHttpVerb_CS() =>
        Builder
        .AddPaths("AnnotateApiActionsWithHttpVerb.cs")
        .Verify();
}
#endif
