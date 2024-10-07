﻿using System;
using System.Diagnostics;

class RawStringLiterals
{
    int SomeProperty => 1;
    int SomeField = 2;

    [DebuggerDisplay("""{SomeProperty}""")] int ExistingMemberTripleQuotes => 1;
    [DebuggerDisplay(""""{SomeField}"""")] int ExistingMemberQuadrupleQuotes => 1;
    [DebuggerDisplay("""
        Some text{SomeField}
        """)] int ExistingMultiLine => 1;
    [DebuggerDisplay($$"""""
        Some text{SomeField}
        """"")] int ExistingMultiLineInterpolated => 1;

    [DebuggerDisplay("""{Nonexistent}""")] int NonexistentTripleQuotes => 1;      // Noncompliant
    //               ^^^^^^^^^^^^^^^^^^^
    [DebuggerDisplay(""""{Nonexistent}"""")] int NonexistentQuadrupleQuotes => 1; // Noncompliant
    //               ^^^^^^^^^^^^^^^^^^^^^
    [DebuggerDisplay("""
        Some text{Nonexistent}
        """)] int NonexistentMultiLine1 => 1;                                     // Noncompliant@-2^22#46
    [DebuggerDisplay("""
        Some text{Some
        Property}
        """)] int NonexistentMultiLine2 => 1;                                     // FN: the new line char make the expression within braces not a valid identifier
    [DebuggerDisplay($$"""""
        Some text{Nonexistent}
        """"")] int NonexistentMultiLineInterpolated => 1;                        // FN: interpolated raw string literals strings not supported
}

public class AccessModifiers
{
    public class BaseClass
    {
        private protected int PrivateProtectedProperty => 1;

        [DebuggerDisplay("{PrivateProtectedProperty}")] // Compliant
        public int SomeProperty => 1;

        [DebuggerDisplay("{Nonexistent}")]              // Noncompliant
        public int OtherProperty => 1;
    }

    public class SubClass : BaseClass
    {
        [DebuggerDisplay("{PrivateProtectedProperty}")] // Compliant
        public int OtherProperty => 1;
    }
}

[DebuggerDisplay("{RecordProperty}")]
public record SomeRecord(int RecordProperty)
{
    [DebuggerDisplay("{RecordProperty}")] public record struct RecordStruct1(int RecordStructProperty);       // Noncompliant
    [DebuggerDisplay("{RecordStructProperty}")] public record struct RecordStruct2(int RecordStructProperty); // Compliant, RecordStructProperty is a property

    [DebuggerDisplay("{RecordProperty}")] public record NestedRecord1(int NestedRecordProperty);       // Noncompliant
    [DebuggerDisplay("{NestedRecordProperty}")] public record NestedRecord2(int NestedRecordProperty); // Compliant, NestedRecordProperty is a property
}

[DebuggerDisplay("{RecordProperty1} bla bla {RecordProperty2}")]
public record struct SomeRecordStruct(int RecordProperty1, string RecordProperty2)
{
    [DebuggerDisplay("{RecordProperty}")]            // Noncompliant
    public class NestedClass1
    {
        [DebuggerDisplay("{NestedClassProperty}")]
        public int NestedClassProperty => 1;
    }

    [DebuggerDisplay("{NestedClassProperty}")]
    public class NestedClass2
    {
        [DebuggerDisplay("{NestedClassProperty}")]
        public int NestedClassProperty => 1;
    }
}

public class ConstantInterpolatedStrings
{
    [DebuggerDisplay($"{{{nameof(SomeProperty)}}}")]
    [DebuggerDisplay($"{{{nameof(NotAProperty)}}}")] // FN: constant interpolated strings not supported
    public int SomeProperty => 1;

    public class NotAProperty { }
}

public interface DefaultInterfaceImplementations
{
    [DebuggerDisplay("{OtherProperty}")]
    [DebuggerDisplay("{OtherPropertyImplemented}")]
    [DebuggerDisplay("{Nonexistent}")]               // Noncompliant
    int WithNonexistentProperty => 1;

    string OtherProperty { get; }
    string OtherPropertyImplemented => "Something";
}

public partial class PartialProperty
{
    public partial string UserName { get; set; }
}

[DebuggerDisplay("{Name}")] // Noncompliant
public partial class PartialProperty
{
    private string _userName;
    public partial string UserName { get => _userName; set { } }
}

[DebuggerDisplay("{UserName}")] // Compliant
public partial class OtherPartialProperty
{
    public partial string UserName { get; set; }
}

public partial class OtherPartialProperty
{
    private string _userName;
    public partial string UserName { get => _userName; set { } }
}

public class EscapeChar
{
    //https://sonarsource.atlassian.net/browse/NET-359
    [DebuggerDisplay("{Non\existent}")] // Compliant FN: escape char in braces not a valid identifier
    public int SomeProperty => 1;

    [DebuggerDisplay("Test:\e {AnotherProperty}")] // Compliant
    public int AnotherProperty => 1;

    [DebuggerDisplay("Hello\e {Nonexistent}")] // Noncompliant
    public int SomeOtherProperty => 1;

    [DebuggerDisplay("{Nonexistent}")] // Noncompliant
    public int OtherProperty => 1;
}