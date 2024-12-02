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

import javax.annotation.CheckForNull;
import java.io.File;
import java.io.IOException;

import static org.sonarsource.dotnet.shared.CallableUtils.lazy;

public class NUnitTestResultsFileParserOld implements UnitTestResultsParserOld {

  private static final Logger LOG = LoggerFactory.getLogger(NUnitTestResultsFileParserOld.class);

  @Override
  public void accept(File file, UnitTestResults unitTestResults) {
    LOG.debug("The current user dir is '{}'.", lazy(() -> System.getProperty("user.dir")));
    LOG.info("Parsing the NUnit Test Results file '{}'.", file.getAbsolutePath());
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
        String rootTag = xmlParserHelper.nextStartTag();
        if ("test-results".equals(rootTag)) {
          handleTestResultsTag(xmlParserHelper);
        } else if ("test-run".equals(rootTag)) {
          handleTestRunTag(xmlParserHelper);
        } else {
          throw xmlParserHelper.parseError("Unrecognized root element <" + rootTag + ">");
        }

      } catch (IOException e) {
        throw new IllegalStateException("Unable to close report", e);
      }
    }

    private void handleTestResultsTag(XmlParserHelper xmlParserHelper) {
      int total = xmlParserHelper.getRequiredIntAttribute("total");
      int errors = xmlParserHelper.getRequiredIntAttribute("errors");
      int failures = xmlParserHelper.getRequiredIntAttribute("failures");
      int inconclusive = xmlParserHelper.getRequiredIntAttribute("inconclusive");
      int ignored = xmlParserHelper.getRequiredIntAttribute("ignored");
      int skipped = xmlParserHelper.getRequiredIntAttribute("skipped");

      int totalSkipped = skipped + inconclusive + ignored;

      Double duration = readExecutionTimeFromDirectlyNestedTestSuiteTags(xmlParserHelper);
      Long executionTime = duration != null ? (long) duration.doubleValue() : null;

      unitTestResults.add(total, totalSkipped, failures, errors, executionTime);

      LOG.debug("Parsed NUnit results - total: {}, totalSkipped: {}, failures: {}, errors: {}, execution time: {}.",
        total, totalSkipped, failures, errors, executionTime);
    }

    private void handleTestRunTag(XmlParserHelper xmlParserHelper) {
      int total = xmlParserHelper.getRequiredIntAttribute("total");
      int failures = xmlParserHelper.getRequiredIntAttribute("failed");
      int inconclusive = xmlParserHelper.getRequiredIntAttribute("inconclusive");
      int skipped = xmlParserHelper.getRequiredIntAttribute("skipped");

      int totalSkipped = skipped + inconclusive;

      Double duration = xmlParserHelper.getDoubleAttribute("duration");
      Long executionTime = duration != null ? (long) (duration * 1000) : null;

      int errors = readErrorCountFromNestedTestCaseTags(xmlParserHelper);

      unitTestResults.add(total, totalSkipped, failures, errors, executionTime);

      LOG.debug("Parsed NUnit test run - total: {}, totalSkipped: {}, failures: {}, errors: {}, execution time: {}.",
        total, totalSkipped, failures, errors, executionTime);
    }

    @CheckForNull
    private static Double readExecutionTimeFromDirectlyNestedTestSuiteTags(XmlParserHelper xmlParserHelper) {
      Double executionTime = null;

      String tag;
      int level = 0;
      while ((tag = xmlParserHelper.nextStartOrEndTag()) != null) {
        if ("<test-suite>".equals(tag)) {
          level++;
          Double time = xmlParserHelper.getDoubleAttribute("time");

          if (level == 1 && time != null) {
            if (executionTime == null) {
              executionTime = 0d;
            }
            executionTime += time * 1000;
          }
        } else if ("</test-suite>".equals(tag)) {
          level--;
        }
      }

      return executionTime;
    }

    private static int readErrorCountFromNestedTestCaseTags(XmlParserHelper xmlParserHelper) {
      int errors = 0;

      String tag;
      int level = 0;
      while ((tag = xmlParserHelper.nextStartOrEndTag()) != null) {
        if ("<test-case>".equals(tag)) {
          level++;
          String label = xmlParserHelper.getAttribute("label");

          if (level == 1 && "Error".equals(label)) {
            errors++;
          }
        } else if ("</test-case>".equals(tag)) {
          level--;
        }
      }

      return errors;
    }
  }

}
