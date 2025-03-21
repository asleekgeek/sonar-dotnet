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
package org.sonarsource.dotnet.shared.plugins;

import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Collections;
import org.junit.Test;

import static org.assertj.core.api.Assertions.assertThat;

public class ReportPathCollectorTest {
  private ReportPathCollector underTest = new ReportPathCollector();

  @Test
  public void should_save_roslyn_report_paths() {
    RoslynReport r1 = new RoslynReport(null, Paths.get("p1"));
    RoslynReport r2 = new RoslynReport(null, Paths.get("p2"));
    underTest.addRoslynReport(Collections.singletonList(r1));
    underTest.addRoslynReport(Collections.singletonList(r2));
    assertThat(underTest.roslynReports()).containsOnly(r1, r2);
    assertThat(underTest.protobufDirs()).isEmpty();
  }

  @Test
  public void should_save_proto_report_paths() {
    Path p1 = Paths.get("p1");
    Path p2 = Paths.get("p2");
    underTest.addProtobufDirs(Collections.singletonList(p1));
    underTest.addProtobufDirs(Collections.singletonList(p2));
    assertThat(underTest.protobufDirs()).containsOnly(p1, p2);
    assertThat(underTest.roslynReports()).isEmpty();
  }
}
