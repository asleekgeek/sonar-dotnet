﻿using System;

namespace CSharp10
{
    class Program
    {
        public const string Part1 = " ";      // Noncompliant {{Replace the control character at position 1 by its escape sequence '\0'.}}
        public const string Part2 = "Some valid string";
        public const string InterpolatedString = $"{Part1}{Part2}"; // The noncompliant control character is not reported here.
    }
}

namespace CSharp11
{
    class Program
    {
        public const string RawCompliant = """test"""; // Compliant
        public const string RawCompliantWithSpecialCharacter = """test"""; // Compliant, raw string

        public const string RawCompliantWithInterpolation = $"""test{RawCompliantWithSpecialCharacter}"""; // Compliant, raw string
        public const string RawCompliantWithInterpolationAndSpecialCharacter = $"""test{RawCompliant}"""; // Compliant, raw string

        void Utf8StringLiterals()
        {
            ReadOnlySpan<byte> Utf8Compliant = "test"u8; // Compliant
            ReadOnlySpan<byte> Utf8Noncompliant = "test"u8; // Noncompliant
            ReadOnlySpan<byte> Utf8VerbatimCompliant = @"test"u8; // Compliant, verbatim utf-8 string

            ReadOnlySpan<byte> Utf8CompliantRaw = """test"""u8; // Compliant, raw string
            ReadOnlySpan<byte> Utf8CompliantRawWithSpecialCharacter = """test"""u8; // Compliant, raw string
        }

        void NewlinesInStringInterpolation()
        {
            var compliant = "test"; // Compliant
            var nonCompliant = "test"; // Noncompliant

            var baseCase = $"test{
                compliant
                }"; // Compliant

            var interpolatedTextHasControlCharacter = $"test{
                nonCompliant
                }"; // Compliant, interpolated text ignored

            var normalTextHasControlCharacter = $"test{
                nonCompliant
                }"; // Noncompliant@-2

            var verbatimWithControlCharacter = @$"test{
                nonCompliant
                }"; // Compliant, verbatim

            var rawWithControlCharacter = $"""test{
                nonCompliant
                }"""; // Compliant, raw
        }
    }
}

namespace CSharp13
{
    class Program
    {
        string compliant = "\u001B"; // Compliant
        string newEscapeChar = "\e"; // Compliant
        string nonCompliant = ""; // Noncompliant {{Replace the control character at position 1 by its escape sequence '\u001B'.}}
    }
}
