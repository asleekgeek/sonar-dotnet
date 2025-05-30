/*
 * SonarSource :: .NET :: Core
 * Copyright (C) 2014-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
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
package org.sonarsource.dotnet.shared.plugins.sensors;

import java.nio.file.Path;
import java.util.List;
import java.util.function.UnaryOperator;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.batch.fs.FileSystem;
import org.sonar.api.batch.fs.InputFile.Type;
import org.sonar.api.batch.sensor.SensorContext;
import org.sonar.api.batch.sensor.SensorDescriptor;
import org.sonar.api.notifications.AnalysisWarnings;
import org.sonar.api.scanner.sensor.ProjectSensor;
import org.sonarsource.dotnet.shared.plugins.PluginMetadata;
import org.sonarsource.dotnet.shared.plugins.ProjectTypeCollector;
import org.sonarsource.dotnet.shared.plugins.ProtobufDataImporter;
import org.sonarsource.dotnet.shared.plugins.RealPathProvider;
import org.sonarsource.dotnet.shared.plugins.ReportPathCollector;
import org.sonarsource.dotnet.shared.plugins.RoslynDataImporter;
import org.sonarsource.dotnet.shared.plugins.RoslynReport;
import org.sonarsource.dotnet.shared.plugins.SensorContextUtils;

import static org.sonarsource.dotnet.shared.CallableUtils.lazy;

/**
 * This class is the main sensor for C# and VB.NET which is going to process all Roslyn reports and protobuf contents and then push them to SonarQube.
 * <p>
 * This sensor is a SQ/SC project sensor. It will execute once at the end.
 * Please note that a "SQ/SC project" is usually the equivalent of an "MSBuild solution".
 */
public class DotNetSensor implements ProjectSensor {

  private static final Logger LOG = LoggerFactory.getLogger(DotNetSensor.class);
  private static final String GET_HELP_MESSAGE = "You can get help on the community forum: https://community.sonarsource.com";
  private static final String READ_MORE_MESSAGE = "Read more about how the SonarScanner for .NET detects test projects: https://github.com/SonarSource/sonar-scanner-msbuild/wiki/Analysis-of-product-projects-vs.-test-projects";

  private final ProtobufDataImporter protobufDataImporter;
  private final RoslynDataImporter roslynDataImporter;
  private final PluginMetadata pluginMetadata;
  private final ReportPathCollector reportPathCollector;
  private final ProjectTypeCollector projectTypeCollector;
  private final AnalysisWarnings analysisWarnings;

  public DotNetSensor(PluginMetadata pluginMetadata, ReportPathCollector reportPathCollector, ProjectTypeCollector projectTypeCollector,
    ProtobufDataImporter protobufDataImporter, RoslynDataImporter roslynDataImporter, AnalysisWarnings analysisWarnings) {
    this.pluginMetadata = pluginMetadata;
    this.reportPathCollector = reportPathCollector;
    this.projectTypeCollector = projectTypeCollector;
    this.protobufDataImporter = protobufDataImporter;
    this.roslynDataImporter = roslynDataImporter;
    this.analysisWarnings = analysisWarnings;
  }

  @Override
  public void describe(SensorDescriptor descriptor) {
    descriptor.name(pluginMetadata.languageName())
      .onlyOnLanguage(pluginMetadata.languageKey());
  }

  @Override
  public void execute(SensorContext context) {
    FileSystem fs = context.fileSystem();
    boolean hasFilesOfLanguage = SensorContextUtils.hasFilesOfLanguage(fs, pluginMetadata.languageKey());
    boolean hasProjects = projectTypeCollector.hasProjects();
    if (hasFilesOfLanguage && hasProjects) {
      importResults(fs, context);
    } else {
      log(hasFilesOfLanguage, hasProjects);
    }
    projectTypeCollector.getSummary(pluginMetadata.languageName()).ifPresent(LOG::info);
  }

  private void importResults(FileSystem fs, SensorContext context) {
    boolean hasMainFiles = SensorContextUtils.hasFilesOfType(fs, Type.MAIN, pluginMetadata.languageKey());
    boolean hasTestFiles = SensorContextUtils.hasFilesOfType(fs, Type.TEST, pluginMetadata.languageKey());
    UnaryOperator<String> toRealPath = new RealPathProvider();

    if (hasTestFiles && !hasMainFiles) {
      warnThatProjectContainsOnlyTestCode(fs, analysisWarnings, pluginMetadata.languageName());
    }

    List<Path> protobufPaths = reportPathCollector.protobufDirs();
    if (protobufPaths.isEmpty()) {
      LOG.warn("No protobuf reports found. The {} files will not have highlighting and metrics. {}",
        lazy(pluginMetadata::languageName),
        GET_HELP_MESSAGE);
    } else {
      protobufDataImporter.importResults(context, protobufPaths, toRealPath);
    }

    List<RoslynReport> roslynReports = reportPathCollector.roslynReports();
    if (roslynReports.isEmpty()) {
      LOG.warn("No Roslyn issue reports were found. The {} files have not been analyzed. {}", lazy(pluginMetadata::languageName), GET_HELP_MESSAGE);
    } else {
      roslynDataImporter.importRoslynReports(roslynReports, context, toRealPath);
    }
  }

  /**
   * If the project does not contain MAIN or TEST files OR does not have any found .NET projects (implicitly it has not been scanned with the Scanner for .NET)
   * we should log a warning to the user, because no files will be analyzed.
   *
   * @param hasFilesOfLanguage True if ANY files of this sensor language have been indexed.
   * @param hasProjects  True if at least one .NET project has been found in {@link FileTypeSensor#execute(SensorContext)}.
   */
  private void log(boolean hasFilesOfLanguage, boolean hasProjects) {
    if (hasProjects) {
      // the scanner for .NET has been used, which means that `hasFilesOfLanguage` is false.
      assert !hasFilesOfLanguage;
    } else if (hasFilesOfLanguage) {
      // the scanner for .NET has _not_ been used.
      LOG.warn("Your project contains {} files which cannot be analyzed with the scanner you are using." +
          " To analyze C# or VB.NET, you must use the SonarScanner for .NET 5.x or higher, see https://redirect.sonarsource.com/doc/install-configure-scanner-msbuild.html",
        lazy(pluginMetadata::languageName));
    }
    if (!hasFilesOfLanguage) {
      logDebugNoFiles();
    }
  }

  private static void warnThatProjectContainsOnlyTestCode(FileSystem fs, AnalysisWarnings analysisWarnings, String languageName) {
    LOG.warn("SonarScanner for .NET detected only TEST files and no MAIN files for {} in the current solution. " +
        "Only TEST-code related results will be imported to your SonarQube project. " +
        "Many of our rules (e.g. vulnerabilities) are raised only on MAIN-code. {}",
      languageName, READ_MORE_MESSAGE);

    // Before outputting a warning in the User Interface, we want to make sure it's worth the user attention.
    // There can be cases where a project written in language X has tests written in languages X, Y and Z.
    // In this case, the fact that there is only test code for languages Y and Z should not trigger a UI warning.
    if (!SensorContextUtils.hasAnyMainFiles(fs)) {
      analysisWarnings.addUnique(
        String.format("Your project contains only TEST-code for language %s and no MAIN-code for any language, so only TEST-code related results are imported. " +
            "Many of our rules (e.g. vulnerabilities) are raised only on MAIN-code. %s",
          languageName, READ_MORE_MESSAGE));
    }
  }

  private static void logDebugNoFiles() {
    // No MAIN and no TEST files -> skip
    LOG.debug("No files to analyze. Skip Sensor.");
  }
}
