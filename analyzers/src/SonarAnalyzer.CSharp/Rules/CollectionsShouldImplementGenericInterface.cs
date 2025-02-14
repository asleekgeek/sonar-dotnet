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
    public sealed class CollectionsShouldImplementGenericInterface : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S3909";
        private const string MessageFormat = "Refactor this collection to implement '{0}'.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        private static readonly Dictionary<KnownType, string> NonGenericToGenericMapping = new()
        {
            { KnownType.System_Collections_ICollection, "System.Collections.Generic.ICollection<T>" },
            { KnownType.System_Collections_IList, "System.Collections.Generic.IList<T>" },
            { KnownType.System_Collections_IEnumerable, "System.Collections.Generic.IEnumerable<T>" },
            { KnownType.System_Collections_CollectionBase, "System.Collections.ObjectModel.Collection<T>" }
        };

        private static readonly ImmutableArray<KnownType> GenericTypes = ImmutableArray.Create(
            KnownType.System_Collections_Generic_ICollection_T,
            KnownType.System_Collections_Generic_IList_T,
            KnownType.System_Collections_Generic_IEnumerable_T,
            KnownType.System_Collections_ObjectModel_Collection_T);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(c =>
                {
                    var typeDeclaration = (BaseTypeDeclarationSyntax)c.Node;
                    var implementedTypes = typeDeclaration.BaseList?.Types;
                    if (implementedTypes is null || c.IsRedundantPositionalRecordContext())
                    {
                        return;
                    }

                    var containingType = (INamedTypeSymbol)c.ContainingSymbol;
                    var typeSymbols = containingType.Interfaces.Concat([containingType.BaseType]).WhereNotNull().ToImmutableArray();
                    if (typeSymbols.Any(x => x.OriginalDefinition.IsAny(GenericTypes)))
                    {
                        return;
                    }
                    foreach (var typeSymbol in typeSymbols)
                    {
                        if (SuggestGenericCollectionType(typeSymbol) is { } suggestedGenericType)
                        {
                            c.ReportIssue(Rule, typeDeclaration.Identifier, suggestedGenericType);
                        }
                    }
                },
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKindEx.RecordDeclaration,
                SyntaxKindEx.RecordStructDeclaration);

        private static string SuggestGenericCollectionType(ITypeSymbol typeSymbol) =>
            NonGenericToGenericMapping.FirstOrDefault(pair => pair.Key.Matches(typeSymbol)).Value;
    }
}
