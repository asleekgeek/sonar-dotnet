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
package org.sonar.plugins.dotnet.tests;

import java.io.File;
import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;
import java.util.regex.Pattern;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.scanner.ScannerSide;

@ScannerSide
public class VisualStudioTestResultParser implements UnitTestResultParser {

  private static final Logger LOG = LoggerFactory.getLogger(VisualStudioTestResultParser.class);

  @Override
  public void parse(File file, Map<String, UnitTestResults> unitTestResults, Map<String, String> methodFileMap) {
    LOG.info("Parsing the Visual Studio Test Results file '{}'.", file.getAbsolutePath());

    var parser = new Parser(file, unitTestResults, methodFileMap, List.of("TestRun"));
    Map<String, Consumer<XmlParserHelper>> tagHandlers = Map.of(
      "UnitTestResult", parser::handleUnitTestResultTag,
      "UnitTest", parser::handleUnitTestTag
    );

    parser.parse(tagHandlers);
  }

  private static class Parser extends XmlTestReportParser {
    private final Map<String, UnitTestResults> testIdTestResultMap;
    // Date Format: // https://github.com/microsoft/vstest/blob/7d34b30433259fb914aaaf276fde663a47b6ef2f/src/Microsoft.TestPlatform.Extensions.TrxLogger/XML/XmlPersistence.cs#L557-L572
    private final DateFormat dateFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSXXX");
    private final Pattern millisecondsPattern = Pattern.compile("(\\.(\\d{0,3}))\\d*+");

    Parser(File file,  Map<String, UnitTestResults> unitTestResults, Map<String, String> methodFileMap, List<String> rootTags) {
      super(file, unitTestResults, methodFileMap, rootTags);
      this.testIdTestResultMap = new HashMap<>();
    }

    private void handleUnitTestResultTag(XmlParserHelper xmlParserHelper) {
      var testId = xmlParserHelper.getRequiredAttribute("testId");
      var outcome = xmlParserHelper.getRequiredAttribute("outcome");
      var start = getRequiredDateAttribute(xmlParserHelper, "startTime");
      var finish = getRequiredDateAttribute(xmlParserHelper, "endTime");
      var duration = finish.getTime() - start.getTime();
      var testResult = new VisualStudioTestResults(outcome, duration);
      if (testIdTestResultMap.containsKey(testId)) {
        testIdTestResultMap.get(testId).add(testResult);
      } else {
        testIdTestResultMap.put(testId, testResult);
      }
      LOG.debug("Parsed Visual Studio Unit Test - testId: {} outcome: {}, duration: {}", testId, outcome, duration);
    }

    private void handleUnitTestTag(XmlParserHelper xmlParserHelper) {
      var testId = xmlParserHelper.getRequiredAttribute("id");

      String tagName;
      while ((tagName = xmlParserHelper.nextStartTag()) != null) {
        if ("TestMethod".equals(tagName)) {
          break;
        }
      }
      if (tagName == null) {
        throw new ParseErrorException("No TestMethod attribute found on UnitTest tag");
      }

      var methodName = xmlParserHelper.getRequiredAttribute("name");
      var className = xmlParserHelper.getRequiredAttribute("className");
      var codeBase = xmlParserHelper.getRequiredAttribute("codeBase");
      var dllName = extractDllNameFromFilePath(codeBase);
      var fullyQualifiedName = getFullName(className + "." + methodName, dllName);
      var testIdTestResult = testIdTestResultMap.get(testId);
      addTestResultToFile(fullyQualifiedName, testIdTestResult);
    }

    private Date getRequiredDateAttribute(XmlParserHelper xmlParserHelper, String name) {
      var value = xmlParserHelper.getRequiredAttribute(name);
      try {
        value = keepOnlyMilliseconds(value);
        return dateFormat.parse(value);
      } catch (ParseException e) {
        throw xmlParserHelper.parseError("Expected a valid date and time instead of \"" + value + "\" for the attribute \"" + name + "\". " + e.getMessage());
      }
    }

    private String keepOnlyMilliseconds(String value) {
      var sb = new StringBuilder();
      var matcher = millisecondsPattern.matcher(value);
      var trailingZeros = new StringBuilder();
      while (matcher.find()) {
        String milliseconds = matcher.group(2);
        trailingZeros.setLength(0);
        trailingZeros.append("0".repeat(Math.max(0, 3 - milliseconds.length())));
        matcher.appendReplacement(sb, "$1" + trailingZeros);
      }
      matcher.appendTail(sb);

      return sb.toString();
    }
  }
}
