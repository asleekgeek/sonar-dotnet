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

using System.Text.RegularExpressions;
using SonarAnalyzer.CFG.Common;

namespace SonarAnalyzer.Core.AnalysisContext;

public static class IssueReporter
{
    private static readonly ImmutableHashSet<string> ExcludedFromDesignTimeRuleIds = ImmutableHashSet.Create(
        "S108",
        "S1481",
        "S927",
        "S4487",
        "S2696",
        "S2259",
        "S1144",
        "S2325",
        "S1117",
        "S1481",
        "S1871");

    private static readonly Regex VbNetErrorPattern = new Regex(@"\s+error(\s+\S+)?\s*:",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        Constants.DefaultRegexTimeout);

    // Minimum supported version for Razor IDE is Visual Studio 17.9/Roslyn 4.9.2
    // https://learn.microsoft.com/en-us/visualstudio/extensibility/roslyn-version-support?view=vs-2022
    private static Version minimumDesignTimeRoslynVersion = new("4.9.2");

    public static void ReportIssueCore(Compilation compilation,
                                       Func<DiagnosticDescriptor, bool> hasMatchingScope,
                                       Func<Diagnostic, ReportingContext> createReportingContext,
                                       DiagnosticDescriptor rule,
                                       Location primaryLocation,
                                       IEnumerable<SecondaryLocation> secondaryLocations = null,
                                       ImmutableDictionary<string, string> properties = null,
                                       params string[] messageArgs)
    {
        _ = rule ?? throw new ArgumentNullException(nameof(rule));
        secondaryLocations ??= [];
        properties ??= ImmutableDictionary<string, string>.Empty;
        secondaryLocations = secondaryLocations.Where(x => x.Location.IsValid(compilation)).ToArray();
        properties = properties.AddRange(secondaryLocations.Select((x, index) => new KeyValuePair<string, string>(index.ToString(), x.Message)));
        var diagnostic = Diagnostic.Create(rule, primaryLocation, secondaryLocations.Select(x => x.Location), properties, messageArgs);
        ReportIssueCore(hasMatchingScope, createReportingContext, diagnostic);
    }

    [Obsolete("Use another overload of ReportIssue, without calling Diagnostic.Create")]
    public static void ReportIssueCore(Func<DiagnosticDescriptor, bool> hasMatchingScope, Func<Diagnostic, ReportingContext> createReportingContext, Diagnostic diagnostic)
    {
        if (ShouldRaiseOnRazorFile(ref diagnostic)
            && hasMatchingScope(diagnostic.Descriptor)
            && SonarAnalysisContext.LegacyIsRegisteredActionEnabled(diagnostic.Descriptor, diagnostic.Location?.SourceTree))
        {
            var reportingContext = createReportingContext(diagnostic);
            if (!diagnostic.Location.IsValid(reportingContext.Compilation))
            {
                Debug.Fail("Primary location should be part of the compilation. An AD0001 is raised if this is not the case.");
                return;
            }
            // This is the current way SonarLint will handle how and what to report.
            if (SonarAnalysisContext.ReportDiagnostic is not null)
            {
                Debug.Assert(SonarAnalysisContext.ShouldDiagnosticBeReported is null, "Not expecting SonarLint to set both the old and the new delegates.");
                SonarAnalysisContext.ReportDiagnostic(reportingContext);
                return;
            }
            // Standalone NuGet, Scanner run and SonarLint < 4.0 used with latest NuGet
            if (!IsTriggeringVbcError(reportingContext.Diagnostic) && (SonarAnalysisContext.ShouldDiagnosticBeReported?.Invoke(reportingContext.Tree, reportingContext.Diagnostic) ?? true))
            {
                reportingContext.ReportDiagnostic(reportingContext.Diagnostic);
            }
        }
    }

    internal static void SetMinimumDesignTimeRoslynVersion(Version version) =>
     minimumDesignTimeRoslynVersion = version;

    internal static Version GetMinimumDesignTimeRoslynVersion() =>
        minimumDesignTimeRoslynVersion;

    internal static bool IsTextMatchingVbcErrorPattern(string text) =>
        text is not null && VbNetErrorPattern.SafeIsMatch(text);

    private static bool ShouldRaiseOnRazorFile(ref Diagnostic diagnostic)
    {
        // On build time, if the diagnostic has a mapped location, we do the mapping ourselves and raise there.
        if (GeneratedCodeRecognizer.IsBuildTimeRazorGeneratedFile(diagnostic.Location.SourceTree))
        {
            if (diagnostic.Location.GetMappedLineSpan().HasMappedPath)
            {
                // we want to map the locations of razor files exclusively during compile time, and not design time (IDE).
                // The reason is that for IDE, the correct way to raise issues is directly on the generated files.
                // source: https://github.com/dotnet/razor/issues/9308#issuecomment-1883869224
                diagnostic = MapDiagnostic(diagnostic);
                return true;
            }
            else
            {
                return false;
            }
        }
        // On design time, we only raise on generated .ide.g.cs files if the diagnostic has a mapped location.
        else if (GeneratedCodeRecognizer.IsDesignTimeRazorGeneratedFile(diagnostic.Location.SourceTree))
        {
            return diagnostic.Location.GetMappedLineSpan().HasMappedPath
                && !ExcludedFromDesignTimeRuleIds.Contains(diagnostic.Id)
                && !RoslynVersion.IsVersionLessThan(minimumDesignTimeRoslynVersion);
        }
        // If it is not a build/design-time generated file, it is a normal file.
        else
        {
            return true;
        }
    }

    private static Diagnostic MapDiagnostic(Diagnostic diagnostic)
    {
        var mappedLocation = diagnostic.Location.EnsureMappedLocation();

        var descriptor = new DiagnosticDescriptor(
            diagnostic.Descriptor.Id,
            diagnostic.Descriptor.Title,
            diagnostic.GetMessage(),
            diagnostic.Descriptor.Category,
            diagnostic.Descriptor.DefaultSeverity,
            diagnostic.Descriptor.IsEnabledByDefault,
            diagnostic.Descriptor.Description,
            diagnostic.Descriptor.HelpLinkUri,
            diagnostic.Descriptor.CustomTags.ToArray());

        return Diagnostic.Create(descriptor,
            mappedLocation,
            diagnostic.AdditionalLocations.Select(x => x.EnsureMappedLocation()).ToImmutableList(),
            diagnostic.Properties);
    }

    /// <summary>
    /// VB.Net Complier (VBC) post-process issues and will fail if the line contains the <see cref="VbNetErrorPattern"/>.
    /// </summary>
    /// <remarks>
    /// This helper method is intended to be used only while waiting for the bug to be fixed on Microsoft side.
    /// <see href="https://github.com/dotnet/roslyn/issues/5724"/>.
    /// </remarks>
    /// <param name="diagnostic">
    /// The diagnostic to test.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> when reporting the diagnostic will trigger a VBC post-process error and <c>false</c> otherwise.
    /// </returns>
    private static bool IsTriggeringVbcError(Diagnostic diagnostic)
    {
        if (diagnostic.Location is null || diagnostic.Location.SourceTree?.GetRoot().Language != LanguageNames.VisualBasic)
        {
            return false;
        }

        var text = diagnostic.Location.SourceTree.GetText();
        var lineNumber = diagnostic.Location.LineNumberToReport();

        return IsTextMatchingVbcErrorPattern(text.Lines[lineNumber - 1].ToString());
    }
}
