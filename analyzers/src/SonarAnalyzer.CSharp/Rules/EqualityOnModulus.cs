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
    public sealed class EqualityOnModulus : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2197";
        private const string MessageFormat = "The result of this modulus operation may not be {0}.";

        private const string CountName = nameof(Enumerable.Count);
        private const string LongCountName = nameof(Enumerable.LongCount);
        private const string LengthName = "Length"; // Represents Array.Length and String.Length
        private const string LongLengthName = nameof(Array.LongLength);
        private const string ListCapacityName = nameof(List<object>.Capacity);

        private static readonly string[] CollectionSizePropertyOrMethodNames = { CountName, LongCountName, LengthName, LongLengthName, ListCapacityName };

        private static readonly CSharpExpressionNumericConverter ExpressionNumericConverter = new();

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterNodeAction(VisitEquality, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);

        private static void VisitEquality(SonarSyntaxNodeReportingContext c)
        {
            var equalsExpression = (BinaryExpressionSyntax)c.Node;

            if ((CheckExpression(equalsExpression.Left, equalsExpression.Right, c.Model)
                ?? CheckExpression(equalsExpression.Right, equalsExpression.Left, c.Model)) is { } constantValue)
            {
                c.ReportIssue(Rule, equalsExpression, constantValue < 0 ? "negative" : "positive");
            }
        }

        private static int? CheckExpression(SyntaxNode node, ExpressionSyntax expression, SemanticModel model) =>
            ExpressionNumericConverter.ConstantIntValue(node) is { } constantValue
            && constantValue != 0
            && expression.RemoveParentheses() is BinaryExpressionSyntax binary
            && binary.IsKind(SyntaxKind.ModuloExpression)
            && !ExpressionIsAlwaysPositive(binary, model)
                ? constantValue
                : null;

        private static bool ExpressionIsAlwaysPositive(BinaryExpressionSyntax binaryExpression, SemanticModel semantic)
        {
            var type = semantic.GetTypeInfo(binaryExpression).Type;
            if (type.IsAny(KnownType.UnsignedIntegers) || type.Is(KnownType.System_UIntPtr))
            {
                return true;
            }

            var leftExpression = binaryExpression.Left;
            var leftExpressionStringForm = leftExpression.ToString();
            return CollectionSizePropertyOrMethodNames.Any(x => leftExpressionStringForm.Contains(x))
                   && semantic.GetSymbolInfo(leftExpression).Symbol is { } symbol
                   && IsCollectionSize(symbol);
        }

        private static bool IsCollectionSize(ISymbol symbol) =>
            IsEnumerableCountMethod(symbol)
            || (symbol is IPropertySymbol propertySymbol
                && (IsLengthProperty(propertySymbol)
                    || IsCollectionCountProperty(propertySymbol)
                    || IsListCapacityProperty(propertySymbol)));

        private static bool IsEnumerableCountMethod(ISymbol symbol) =>
            (CountName.Equals(symbol.Name) || LongCountName.Equals(symbol.Name))
            && symbol is IMethodSymbol methodSymbol
            && methodSymbol.IsExtensionMethod
            && methodSymbol.ReceiverType is not null
            && methodSymbol.IsExtensionOn(KnownType.System_Collections_Generic_IEnumerable_T);

        private static bool IsLengthProperty(IPropertySymbol propertySymbol) =>
            (LengthName.Equals(propertySymbol.Name) || LongLengthName.Equals(propertySymbol.Name))
            && propertySymbol.ContainingType.IsAny(KnownType.System_Array, KnownType.System_String);

        private static bool IsCollectionCountProperty(IPropertySymbol propertySymbol) =>
            CountName.Equals(propertySymbol.Name)
            && (propertySymbol.ContainingType.Implements(KnownType.System_Collections_Generic_ICollection_T)
                || propertySymbol.ContainingType.Implements(KnownType.System_Collections_Generic_IReadOnlyCollection_T));

        private static bool IsListCapacityProperty(IPropertySymbol propertySymbol) =>
            ListCapacityName.Equals(propertySymbol.Name)
            && propertySymbol.ContainingType.Implements(KnownType.System_Collections_Generic_IList_T);
    }
}
