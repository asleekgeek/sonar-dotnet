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
    public sealed class DurableEntityInterfaceRestrictions : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S6424";
        private const string MessageFormat = "Use valid entity interface. {0} {1}.";
        private const string SignalEntityName = "SignalEntity";
        private const string SignalEntityAsyncName = "SignalEntityAsync";
        private const string CreateEntityProxyName = "CreateEntityProxy";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(c =>
                {
                    var name = (GenericNameSyntax)c.Node;
                    if (name.Identifier.ValueText is SignalEntityName or SignalEntityAsyncName or CreateEntityProxyName
                        && name.TypeArgumentList.Arguments.Count == 1
                        && c.Model.GetSymbolInfo(name).Symbol is IMethodSymbol method
                        && IsRestrictedMethod(method)
                        && method.TypeArguments.Single() is INamedTypeSymbol { TypeKind: not TypeKind.Error } entityInterface
                        && InterfaceErrorMessage(entityInterface) is { } message)
                    {
                        c.ReportIssue(Rule, name, entityInterface.Name, message);
                    }
                },
                SyntaxKind.GenericName);

        private static bool IsRestrictedMethod(IMethodSymbol method) =>
            method.Is(KnownType.Microsoft_Azure_WebJobs_Extensions_DurableTask_IDurableEntityContext, SignalEntityName)
            || method.Is(KnownType.Microsoft_Azure_WebJobs_Extensions_DurableTask_IDurableEntityClient, SignalEntityAsyncName)
            || method.Is(KnownType.Microsoft_Azure_WebJobs_Extensions_DurableTask_IDurableOrchestrationContext, CreateEntityProxyName);

        private static string InterfaceErrorMessage(INamedTypeSymbol entityInterface)
        {
            if (entityInterface.TypeKind != TypeKind.Interface)
            {
                return "is not an interface";
            }
            else if (entityInterface.IsGenericType)
            {
                return "is generic";
            }
            else
            {
                var members = new[] { entityInterface }.Concat(entityInterface.AllInterfaces).SelectMany(x => x.GetMembers()).ToArray();
                return members.Any()
                    ? members.Select(MemberErrorMessage).WhereNotNull().FirstOrDefault()
                    : "is empty";
            }
        }

        private static string MemberErrorMessage(ISymbol member)
        {
            if (member is not IMethodSymbol method)
            {
                return $@"contains {member.Kind.ToString().ToLower()} ""{member.Name}"". Only methods are allowed";
            }
            else if (method.IsGenericMethod)
            {
                return $@"contains generic method ""{method.Name}""";
            }
            else if (method.Parameters.Length > 1)
            {
                return $@"contains method ""{method.Name}"" with {method.Parameters.Length} parameters. Zero or one are allowed";
            }
            else if (!(method.ReturnsVoid
                || method.ReturnType.Is(KnownType.System_Threading_Tasks_Task)
                || method.ReturnType.Is(KnownType.System_Threading_Tasks_Task_T)))
            {
                return $@"contains method ""{method.Name}"" with invalid return type. Only ""void"", ""Task"" and ""Task<T>"" are allowed";
            }
            else
            {
                return null;
            }
        }
    }
}
