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
    [Obsolete("This rule has been deprecated since 9.14")]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SerializationConstructorsShouldBeSecured : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4212";
        private const string MessageFormat = "Secure this serialization constructor.";

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        private static readonly AttributeComparer attributeComparer = new AttributeComparer();

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterNodeAction(FindPossibleViolations, SyntaxKind.ConstructorDeclaration);
        }

        private static void FindPossibleViolations(SonarSyntaxNodeReportingContext c)
        {
            var constructorSyntax = (ConstructorDeclarationSyntax)c.Node;
            var reportLocation = constructorSyntax?.Identifier.GetLocation();
            if (reportLocation == null)
            {
                return;
            }

            var serializationConstructor = c.Model.GetDeclaredSymbol(constructorSyntax);
            if (!serializationConstructor.IsSerializationConstructor())
            {
                return;
            }

            var classSymbol = serializationConstructor.ContainingType;
            if (!classSymbol.Implements(KnownType.System_Runtime_Serialization_ISerializable))
            {
                return;
            }

            var isAssemblyIsPartiallyTrusted = c.Model.Compilation.Assembly
                .HasAttribute(KnownType.System_Security_AllowPartiallyTrustedCallersAttribute);
            if (!isAssemblyIsPartiallyTrusted)
            {
                return;
            }

            var serializationConstructorAttributes = GetCASAttributes(serializationConstructor).ToHashSet();

            bool isConstructorMissingAttributes = classSymbol.Constructors
                .SelectMany(m => GetCASAttributes(m))
                .Any(attr => !serializationConstructorAttributes.Contains(attr, attributeComparer));

            if (isConstructorMissingAttributes)
            {
                c.ReportIssue(rule, reportLocation);
            }
        }

        private static IEnumerable<AttributeData> GetCASAttributes(IMethodSymbol methodSymbol)
        {
            return methodSymbol.GetAttributes().Where(IsCASAttribute);

            bool IsCASAttribute(AttributeData data) =>
                data?.AttributeClass.DerivesFrom(KnownType.System_Security_Permissions_CodeAccessSecurityAttribute)
                    ?? false;
        }

        private class AttributeComparer : IEqualityComparer<AttributeData>
        {
            public bool Equals(AttributeData x, AttributeData y)
            {
                return Equals(x.AttributeConstructor, y.AttributeConstructor) &&
                    Enumerable.SequenceEqual(x.ConstructorArguments, y.ConstructorArguments) &&
                    AreNamedArgumentsEqual(x.NamedArguments, y.NamedArguments);
            }

            private bool AreNamedArgumentsEqual(
                IEnumerable<KeyValuePair<string, TypedConstant>> argumentsX,
                IEnumerable<KeyValuePair<string, TypedConstant>> argumentsY)
            {
                var dictX = argumentsX.ToDictionary(p => p.Key, p => p.Value);
                var dictY = argumentsY.ToDictionary(p => p.Key, p => p.Value);

                if (dictX.Count != dictY.Count)
                {
                    return false;
                }

                foreach (var key in dictX.Keys)
                {
                    if (!dictX.TryGetValue(key, out TypedConstant itemX) ||
                        !dictY.TryGetValue(key, out TypedConstant itemY) ||
                        !Equals(itemX, itemY))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(AttributeData obj) => 1;
        }
    }
}
