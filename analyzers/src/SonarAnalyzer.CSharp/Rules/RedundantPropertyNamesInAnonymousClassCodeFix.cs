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
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public sealed class RedundantPropertyNamesInAnonymousClassCodeFix : SonarCodeFix
    {
        internal const string Title = "Remove redundant explicit property names";
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RedundantPropertyNamesInAnonymousClass.DiagnosticId);

        protected override Task RegisterCodeFixesAsync(SyntaxNode root, SonarCodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var nameEquals = root.FindNode(diagnosticSpan) as NameEqualsSyntax;
            if (!(nameEquals?.Parent?.Parent is AnonymousObjectCreationExpressionSyntax anonymousObjectCreation))
            {
                return Task.CompletedTask;
            }

            context.RegisterCodeFix(
                Title,
                c =>
                {
                    var newInitializersWithSeparators = anonymousObjectCreation.Initializers.GetWithSeparators()
                        .Select(item => GetNewSyntaxListItem(item));
                    var newAnonymousObjectCreation = anonymousObjectCreation
                        .WithInitializers(SyntaxFactory.SeparatedList<AnonymousObjectMemberDeclaratorSyntax>(newInitializersWithSeparators))
                        .WithTriviaFrom(anonymousObjectCreation);

                    var newRoot = root.ReplaceNode(
                        anonymousObjectCreation,
                        newAnonymousObjectCreation);
                    return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                },
                context.Diagnostics);

            return Task.CompletedTask;
        }

        private static SyntaxNodeOrToken GetNewSyntaxListItem(SyntaxNodeOrToken item)
        {
            if (!item.IsNode)
            {
                return item;
            }

            var member = (AnonymousObjectMemberDeclaratorSyntax)item.AsNode();
            if (member.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == member.NameEquals.Name.Identifier.ValueText)
            {
                return SyntaxFactory.AnonymousObjectMemberDeclarator(member.Expression).WithTriviaFrom(member);
            }

            return item;
        }
    }
}
