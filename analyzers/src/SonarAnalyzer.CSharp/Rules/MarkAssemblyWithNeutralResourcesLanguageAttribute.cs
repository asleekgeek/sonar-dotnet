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
    public sealed class MarkAssemblyWithNeutralResourcesLanguageAttribute : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S4026";
        private const string MessageFormat = "Provide a 'System.Resources.NeutralResourcesLanguageAttribute' attribute for assembly '{0}'.";
        private const string StronglyTypedResourceBuilder = "System.Resources.Tools.StronglyTypedResourceBuilder";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat, isCompilationEnd: true);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterCompilationStartAction(c =>
                {
                    var hasResx = false;
                    c.RegisterNodeActionInAllFiles(cc =>
                        hasResx = hasResx || IsResxGeneratedFile(cc.Model, (ClassDeclarationSyntax)cc.Node),
                        SyntaxKind.ClassDeclaration);

                    c.RegisterCompilationEndAction(cc =>
                        {
                            if (hasResx && !HasNeutralResourcesLanguageAttribute(cc.Compilation.Assembly))
                            {
                                cc.ReportIssue(Rule, (Location)null, cc.Compilation.AssemblyName);
                            }
                        });
                });

        private static bool IsDesignerFile(SyntaxTree tree) =>
            tree.FilePath?.IndexOf(".Designer.", StringComparison.OrdinalIgnoreCase) > 0;

        private static bool HasGeneratedCodeAttributeWithStronglyTypedResourceBuilderValue(SemanticModel semanticModel, ClassDeclarationSyntax classSyntax) =>
            classSyntax.AttributeLists
                .GetAttributes(KnownType.System_CodeDom_Compiler_GeneratedCodeAttribute, semanticModel)
                .Where(x => x.ArgumentList.Arguments.Count > 0)
                .Select(x => semanticModel.GetConstantValue(x.ArgumentList.Arguments[0].Expression))
                .Any(constant => string.Equals(constant.Value as string, StronglyTypedResourceBuilder, StringComparison.OrdinalIgnoreCase));

        private static bool IsResxGeneratedFile(SemanticModel semanticModel, ClassDeclarationSyntax classSyntax) =>
            IsDesignerFile(semanticModel.SyntaxTree) && HasGeneratedCodeAttributeWithStronglyTypedResourceBuilderValue(semanticModel, classSyntax);

        private static bool HasNeutralResourcesLanguageAttribute(IAssemblySymbol assemblySymbol) =>
            assemblySymbol.GetAttributes(KnownType.System_Resources_NeutralResourcesLanguageAttribute)
                .Any(attribute => attribute.ConstructorArguments.Any(arg => arg.Type.Is(KnownType.System_String) && !string.IsNullOrWhiteSpace((string)arg.Value)));
    }
}
