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

namespace SonarAnalyzer.Test.Rules;

[TestClass]
public class SpecifyRouteAttributeTest
{
    private readonly VerifierBuilder builder = new VerifierBuilder<SpecifyRouteAttribute>()
        .WithBasePath("AspNet")
        .WithOptions(LanguageOptions.FromCSharp12)
        .AddReferences([
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcCore,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcViewFeatures,
            AspNetCoreMetadataReference.MicrosoftAspNetCoreMvcAbstractions
        ]);

    [TestMethod]
    public void SpecifyRouteAttribute_CSharp12() =>
        builder.AddPaths("SpecifyRouteAttribute.CSharp12.cs").Verify();

    [TestMethod]
    public void SpecifyRouteAttribute_PartialClasses_CSharp12() =>
        builder
            .AddSnippet("""
                using Microsoft.AspNetCore.Mvc;

                public partial class HomeController : Controller       // Noncompliant [first]
                {
                    [HttpGet("Test")]
                    public IActionResult Index() => View();            // Secondary [first, second]
                }
                """)
            .AddSnippet("""
                using Microsoft.AspNetCore.Mvc;

                public partial class HomeController : Controller { }   // Noncompliant [second]
                """)
            .Verify();

    [TestMethod]
    public void SpecifyRouteAttribute_PartialClasses_OneGenerated_CSharp12() =>
        builder
            .AddSnippet("""
                // <auto-generated/>
                using Microsoft.AspNetCore.Mvc;

                public partial class HomeController : Controller
                {
                    [HttpGet("Test")]
                    public IActionResult ActionInGeneratedCode() => View();     // Secondary
                }
                """)
            .AddSnippet("""
                using Microsoft.AspNetCore.Mvc;

                public partial class HomeController : Controller                // Noncompliant
                {
                    [HttpGet("Test")]
                    public IActionResult Index() => View();                     // Secondary
                }
                """)
            .Verify();
}

#endif
