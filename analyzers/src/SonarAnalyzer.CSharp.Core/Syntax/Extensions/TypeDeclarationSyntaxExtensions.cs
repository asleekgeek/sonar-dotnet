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

namespace SonarAnalyzer.CSharp.Core.Syntax.Extensions;

public static class TypeDeclarationSyntaxExtensions
{
    /// <summary>
    /// Returns a union of all the methods and local functions from a given type declaration.
    /// </summary>
    public static IEnumerable<IMethodDeclaration> GetMethodDeclarations(this TypeDeclarationSyntax typeDeclaration) =>
        typeDeclaration.Members
                       .OfType<MethodDeclarationSyntax>()
                       .SelectMany(method => GetLocalFunctions(method).Union(new List<IMethodDeclaration> { MethodDeclarationFactory.Create(method) }));

    private static IEnumerable<IMethodDeclaration> GetLocalFunctions(MethodDeclarationSyntax methodDeclaration) =>
        methodDeclaration.DescendantNodes()
                         .Where(member => member.IsKind(SyntaxKindEx.LocalFunctionStatement))
                         .Select(member => MethodDeclarationFactory.Create(member));

    public static IMethodSymbol PrimaryConstructor(this TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
    {
        if (ParameterList(typeDeclaration) is { } parameterList)
        {
            return parameterList is { Parameters: { Count: > 0 } parameters } && parameters[0] is { Identifier.RawKind: not (int)SyntaxKind.ArgListKeyword } parameter0
                ? semanticModel.GetDeclaredSymbol(parameter0)?.ContainingSymbol as IMethodSymbol
                : semanticModel.GetDeclaredSymbol(typeDeclaration).GetMembers(".ctor").OfType<IMethodSymbol>().FirstOrDefault(m => m is
                {
                    MethodKind: MethodKind.Constructor,
                    Parameters.Length: 0,
                });
        }

        return null;
    }

    public static ParameterListSyntax ParameterList(this TypeDeclarationSyntax typeDeclaration) =>
        typeDeclaration.Kind() switch
        {
            SyntaxKind.ClassDeclaration => ((ClassDeclarationSyntaxWrapper)typeDeclaration).ParameterList,
            SyntaxKind.StructDeclaration => ((StructDeclarationSyntaxWrapper)typeDeclaration).ParameterList,
            SyntaxKindEx.RecordDeclaration or SyntaxKindEx.RecordStructDeclaration => ((RecordDeclarationSyntaxWrapper)typeDeclaration).ParameterList,
            _ => default,
        };
}
