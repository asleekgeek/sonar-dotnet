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
import java.nio.file.Paths;
import java.util.Collections;
import org.junit.Test;
import org.sonar.api.batch.sensor.SensorContext;
import org.sonar.api.batch.sensor.SensorDescriptor;
import org.sonarsource.dotnet.shared.plugins.ModuleConfiguration;
import org.sonarsource.dotnet.shared.plugins.PluginMetadata;
import org.sonarsource.dotnet.shared.plugins.ReportPathCollector;
import org.sonarsource.dotnet.shared.plugins.RoslynReport;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoInteractions;
import static org.mockito.Mockito.verifyNoMoreInteractions;
import static org.mockito.Mockito.when;

public class PropertiesSensorTest {
  private ModuleConfiguration config = mock(ModuleConfiguration.class);
  private ReportPathCollector reportPathCollector = mock(ReportPathCollector.class);

  PropertiesSensor underTest = new PropertiesSensor(config, reportPathCollector, pluginMetadata());

  private PluginMetadata pluginMetadata() {
    PluginMetadata metadata = mock(PluginMetadata.class);
    when(metadata.languageKey()).thenReturn("languageKey");
    when(metadata.languageName()).thenReturn("Lang Name");
    return metadata;
  }

  @Test
  public void should_collect_properties_from_multiple_modules() {
    Path roslyn1 = Paths.get("roslyn1");
    Path roslyn2 = Paths.get("roslyn2");
    Path proto1 = Paths.get("proto1");
    Path proto2 = Paths.get("proto2");

    when(config.roslynReportPaths()).thenReturn(Collections.singletonList(roslyn1));
    when(config.protobufReportPaths()).thenReturn(Collections.singletonList(proto1));
    underTest.execute(mock(SensorContext.class));
    verify(reportPathCollector).addProtobufDirs(Collections.singletonList(proto1));
    verify(reportPathCollector).addRoslynReport(Collections.singletonList(new RoslynReport(null, roslyn1)));

    when(config.roslynReportPaths()).thenReturn(Collections.singletonList(roslyn2));
    when(config.protobufReportPaths()).thenReturn(Collections.singletonList(proto2));
    underTest.execute(mock(SensorContext.class));
    verify(reportPathCollector).addProtobufDirs(Collections.singletonList(proto2));
    verify(reportPathCollector).addRoslynReport(Collections.singletonList(new RoslynReport(null, roslyn2)));

    verifyNoMoreInteractions(reportPathCollector);
  }

  @Test
  public void should_describe() {
    SensorDescriptor desc = mock(SensorDescriptor.class);
    when(desc.name(anyString())).thenReturn(desc);

    underTest.describe(desc);

    verify(desc).name("Lang Name Properties");
    verify(desc, never()).onlyOnLanguage(any());
    verify(desc, never()).onlyOnLanguages(any());
    verify(desc, never()).onlyOnFileType(any());
    verify(desc, never()).onlyWhenConfiguration(any());
    verify(desc, never()).createIssuesForRuleRepository(any());
    verify(desc, never()).createIssuesForRuleRepositories(any());
  }

  @Test
  public void should_continue_if_report_path_not_present() {
    when(config.roslynReportPaths()).thenReturn(Collections.emptyList());
    when(config.protobufReportPaths()).thenReturn(Collections.emptyList());
    underTest.execute(mock(SensorContext.class));
    verifyNoInteractions(reportPathCollector);
  }
}
