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

using TypeMap = System.Collections.Generic.Dictionary<Microsoft.CodeAnalysis.INamedTypeSymbol, System.Collections.Generic.HashSet<Microsoft.CodeAnalysis.INamedTypeSymbol>>;

namespace SonarAnalyzer.Core.Rules;

public abstract class InvalidCastToInterfaceBase<TSyntaxKind> : SonarDiagnosticAnalyzer
    where TSyntaxKind : struct
{
    protected const string DiagnosticId = "S1944";
    protected const string MessageFormat = "{0}";
    private const string MessageInterface = "Review this cast; in this project there's no type that implements both '{0}' and '{1}'.";
    private const string MessageClass = "Review this cast; in this project there's no type that extends '{0}' and implements '{1}'.";

    // Once we remove the old SE engine, this should inherit SonarDiagnosticAnalyzer<TSyntaxKind> to delegate and simplify this scaffolding.
    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    protected abstract ILanguageFacade<TSyntaxKind> Language { get; }
    protected abstract DiagnosticDescriptor Rule { get; }

    protected override void Initialize(SonarAnalysisContext context) =>
        context.RegisterCompilationStartAction(
            compilationStartContext =>
            {
                var interfaceImplementers = BuildTypeMap(compilationStartContext.Compilation.GlobalNamespace.GetAllNamedTypes());
                compilationStartContext.RegisterNodeAction(Language.GeneratedCodeRecognizer, c =>
                    {
                        var type = Language.Syntax.CastType(c.Node);
                        var interfaceType = c.Model.GetTypeInfo(type).Type as INamedTypeSymbol;
                        var expressionType = c.Model.GetTypeInfo(Language.Syntax.CastExpression(c.Node)).Type as INamedTypeSymbol;
                        if (IsImpossibleCast(interfaceImplementers, interfaceType, expressionType))
                        {
                            var location = type.GetLocation();
                            var interfaceTypeName = interfaceType.ToMinimalDisplayString(c.Model, location.SourceSpan.Start);
                            var expressionTypeName = expressionType.ToMinimalDisplayString(c.Model, location.SourceSpan.Start);
                            var message = expressionType.IsInterface() ? MessageInterface : MessageClass;
                            c.ReportIssue(Rule, location, string.Format(message, expressionTypeName, interfaceTypeName));
                        }
                    },
                    Language.SyntaxKind.CastExpressions);
            });

    private static TypeMap BuildTypeMap(IEnumerable<INamedTypeSymbol> allTypes)
    {
        var ret = new TypeMap();
        foreach (var type in allTypes)
        {
            if (type.IsInterface())
            {
                Add(type, type);
            }
            foreach (var @interface in type.AllInterfaces)
            {
                Add(@interface, type);
            }
        }
        return ret;

        void Add(INamedTypeSymbol key, INamedTypeSymbol value)
        {
            if (!ret.TryGetValue(key, out var values))
            {
                values = new();
                ret.Add(key, values);
            }
            values.Add(value);
        }
    }

    private static bool IsImpossibleCast(TypeMap interfaceImplementers, INamedTypeSymbol interfaceType, INamedTypeSymbol expressionType)
    {
        return interfaceType.IsInterface()
            && ConcreteImplementationExists(interfaceType)
            && ExpressionTypeIsRelevant()
            && !expressionType.DerivesOrImplements(interfaceType)
            && interfaceImplementers.TryGetValue(interfaceType, out var implementers)
            && !implementers.Any(x => x.DerivesOrImplements(expressionType));

        bool ExpressionTypeIsRelevant() =>
            expressionType is not null
            && !expressionType.IsSealed
            && !expressionType.Is(KnownType.System_Object)
            && (!expressionType.IsInterface() || ConcreteImplementationExists(expressionType));

        bool ConcreteImplementationExists(INamedTypeSymbol type) =>
            interfaceImplementers.TryGetValue(type, out var implementers) && implementers.Any(x => x.IsClassOrStruct());
    }
}
