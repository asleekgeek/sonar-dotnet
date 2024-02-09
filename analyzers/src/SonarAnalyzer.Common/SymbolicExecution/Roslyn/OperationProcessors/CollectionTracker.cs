﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2024 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using SonarAnalyzer.SymbolicExecution.Constraints;

namespace SonarAnalyzer.SymbolicExecution.Roslyn.OperationProcessors;

internal static class CollectionTracker
{
    public static readonly ImmutableArray<KnownType> CollectionTypes = ImmutableArray.Create(
        KnownType.System_Collections_Generic_List_T,
        KnownType.System_Collections_Generic_IList_T,
        KnownType.System_Collections_Immutable_IImmutableList_T,
        KnownType.System_Collections_Generic_ICollection_T,
        KnownType.System_Collections_Generic_HashSet_T,
        KnownType.System_Collections_Generic_ISet_T,
        KnownType.System_Collections_Immutable_IImmutableSet_T,
        KnownType.System_Collections_Generic_Queue_T,
        KnownType.System_Collections_Immutable_IImmutableQueue_T,
        KnownType.System_Collections_Generic_Stack_T,
        KnownType.System_Collections_Immutable_IImmutableStack_T,
        KnownType.System_Collections_ObjectModel_ObservableCollection_T,
        KnownType.System_Array,
        KnownType.System_Collections_Immutable_IImmutableArray_T,
        KnownType.System_Collections_Generic_Dictionary_TKey_TValue,
        KnownType.System_Collections_Generic_IDictionary_TKey_TValue,
        KnownType.System_Collections_Immutable_IImmutableDictionary_TKey_TValue);

    public static CollectionConstraint ObjectCreationConstraint(ProgramState state, IObjectCreationOperationWrapper operation)
    {
        if (operation.Type.IsAny(CollectionTypes))
        {
            return operation.Arguments.SingleOrDefault(IsEnumerable) is { } argument
                ? state.Constraint<CollectionConstraint>(argument)
                : CollectionConstraint.Empty;
        }
        else
        {
            return null;
        }

        static bool IsEnumerable(IOperation operation) =>
            operation.ToArgument().Parameter.Type.DerivesOrImplements(KnownType.System_Collections_IEnumerable);
    }

    public static CollectionConstraint ArrayCreationConstraint(IArrayCreationOperationWrapper operation) =>
        operation.DimensionSizes.Any(x => x.ConstantValue.Value is 0)
            ? CollectionConstraint.Empty
            : CollectionConstraint.NotEmpty;
}