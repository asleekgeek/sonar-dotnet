/*
 * SonarSource :: .NET :: Core
 * Copyright (C) 2014-2024 SonarSource SA
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
package org.sonar.plugins.dotnet.tests;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.IOException;

import static org.sonarsource.dotnet.shared.CallableUtils.lazy;

public class XUnitTestResultsFileParserOld implements UnitTestResultsParserOld {

  private static final Logger LOG = LoggerFactory.getLogger(XUnitTestResultsFileParserOld.class);

  @Override
  public void accept(File file, UnitTestResults unitTestResults) {
    LOG.debug("The current user dir is '{}'.", lazy(() -> System.getProperty("user.dir")));
    LOG.info("Parsing the XUnit Test Results file '{}'.", file.getAbsolutePath());
    new Parser(file, unitTestResults).parse();
  }

  private static class Parser {

    private final File file;
    private final UnitTestResults unitTestResults;

    Parser(File file, UnitTestResults unitTestResults) {
      this.file = file;
      this.unitTestResults = unitTestResults;
    }

    public void parse() {
      try (XmlParserHelper xmlParserHelper = new XmlParserHelper(file)) {

        String tag = xmlParserHelper.nextStartTag();
        if (!"assemblies".equals(tag) && !"assembly".equals(tag)) {
          throw xmlParserHelper.parseError("Expected either an <assemblies> or an <assembly> root tag, but got <" + tag + "> instead.");
        }

        do {
          if ("assembly".equals(tag)) {
            handleAssemblyTag(xmlParserHelper);
          }
        } while ((tag = xmlParserHelper.nextStartTag()) != null);
      } catch (IOException e) {
        throw new IllegalStateException("Unable to close report", e);
      }
    }

    private void handleAssemblyTag(XmlParserHelper xmlParserHelper) {

      String totalString = xmlParserHelper.getAttribute("total");
      if (totalString == null) {
        LOG.warn("One of the assemblies contains no test result, please make sure this is expected.");
        return;
      }

      int total = xmlParserHelper.tagToIntValue("total", totalString);
      int failed = xmlParserHelper.getRequiredIntAttribute("failed");
      int skipped = xmlParserHelper.getRequiredIntAttribute("skipped");
      int errors = xmlParserHelper.getIntAttributeOrZero("errors");

      Double time = xmlParserHelper.getDoubleAttribute("time");
      Long executionTime = time != null ? (long) (time * 1000) : null;

      unitTestResults.add(total, skipped, failed, errors, executionTime);
      LOG.debug("Parsed XUnit test results - total: {}, failed: {}, skipped: {}, errors: {}, executionTime: {}.",
        total, failed, skipped, errors, executionTime);
    }

  }

}
