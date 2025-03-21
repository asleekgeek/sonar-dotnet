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
package org.sonarsource.dotnet.shared.sarif;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.function.UnaryOperator;
import org.sonarsource.dotnet.shared.plugins.RoslynReport;

public class SarifParserFactory {
  private SarifParserFactory() {
    // private
  }

  public static SarifParser create(RoslynReport report, UnaryOperator<String> toRealPath) {
    try (InputStreamReader reader = new InputStreamReader(Files.newInputStream(report.getReportPath()), StandardCharsets.UTF_8)) {

      JsonParser parser = new JsonParser();
      JsonObject root = parser.parse(reader).getAsJsonObject();
      if (root.has("version")) {
        String version = root.get("version").getAsString();

        return switch (version) {
          case "0.4", "0.1" -> new SarifParser01And04(report.getProject(), root, toRealPath);
          default -> new SarifParser10(report.getProject(), root, toRealPath);
        };
      }
    } catch (IOException e) {
      throw new IllegalStateException("Unable to read the Roslyn SARIF report file: " + report.getReportPath().toAbsolutePath(), e);
    }

    throw new IllegalStateException(String.format("Unable to parse the Roslyn SARIF report file: %s. Unrecognized format", report.getReportPath().toAbsolutePath()));
  }
}
