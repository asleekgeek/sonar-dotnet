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
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.slf4j.event.Level;
import org.sonar.api.testfixtures.log.LogTester;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.Assert.assertThrows;
import static org.junit.Assert.assertEquals;

public class VisualStudioTestResultParserTest {

  @Rule
  public LogTester logTester = new LogTester();

  @Before
  public void before() {
    logTester.setLevel(Level.DEBUG);
  }

  @Test
  public void valid() {
    Map<String, UnitTestResults> results = new HashMap<>();
    Map<String, String> fileMap = new HashMap<>() {
      {
        put("Playground.Test.TestProject1.UnitTest1.TestMethod1", "C:\\dev\\Playground\\Playground.Test\\Sample.cs");
        put("Playground.Test.TestProject1.UnitTest1.TestShouldFail", "C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs");
        put("Playground.Test.TestProject1.UnitTest1.TestShouldSkip", "C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs");
        put("Playground.Test.TestProject1.UnitTest1.TestShouldError", "C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs");
        put("Playground.Test.TestProject1.UnitTest2.TestMethod5", "C:\\dev\\Playground\\Playground.Test\\Sample.cs");
      }
    };

    var sut = new VisualStudioTestResultParser();

    sut.parse(new File("src/test/resources/visualstudio_test_results/valid.trx"), results, fileMap);

    assertThat(results).hasSize(2);
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\Sample.cs").tests()).isEqualTo(13);
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\Sample.cs").failures()).isZero();
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\Sample.cs").skipped()).isEqualTo(7);
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\Sample.cs").errors()).isEqualTo(1);
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\Sample.cs").executionTime()).isEqualTo(47);

    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs").tests()).isEqualTo(3);
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs").failures()).isEqualTo(2);
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs").skipped()).isEqualTo(1);
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs").errors()).isZero();
    assertThat(results.get("C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs").executionTime()).isEqualTo(22);

    List<String> infoLogs = logTester.logs(Level.INFO);
    assertThat(infoLogs).hasSize(1);
    assertThat(infoLogs.get(0)).startsWith("Parsing the Visual Studio Test Results file ");

    List<String> debugLogs = logTester.logs(Level.DEBUG);
    assertThat(debugLogs)
      .hasSize(32)
      .contains(
        "Parsed Visual Studio Unit Test - testId: d7744238-9adf-b364-3d70-ae38261a8cd8 outcome: Failed, duration: 20",
        "Parsed Visual Studio Unit Test - testId: c7dc64cd-0233-3937-7ce3-ae46f9eabe5c outcome: NotExecuted, duration: 0",
        "Added Test Method: Playground.Test.TestProject1.UnitTest1.TestMethod1 to File: C:\\dev\\Playground\\Playground.Test\\Sample.cs",
        "Added Test Method: Playground.Test.TestProject1.UnitTest1.TestShouldError to File: C:\\dev\\Playground\\Playground.Test\\UnitTest1.cs"
      );
  }

  @Test
  public void multiple_runs_same_test() {
    Map<String, UnitTestResults> results = new HashMap<>();
    Map<String, String> fileMap = new HashMap<>() {
      {
        put("TestReport.TestReport.Test1.TestMethod1", "Sample.cs");
      }
    };

    var sut = new VisualStudioTestResultParser();

    sut.parse(new File("src/test/resources/visualstudio_test_results/multiple_runs_same_test.trx"), results, fileMap);

    assertThat(results).hasSize(1);
    assertThat(results.get("Sample.cs").tests()).isEqualTo(2);
    assertThat(results.get("Sample.cs").failures()).isZero();
    assertThat(results.get("Sample.cs").skipped()).isZero();
    assertThat(results.get("Sample.cs").errors()).isZero();
    assertThat(results.get("Sample.cs").executionTime()).isEqualTo(26);

    assertThat(logTester.logs(Level.WARN)).isEmpty();

    var infoLogs = logTester.logs(Level.INFO);
    assertThat(infoLogs).hasSize(1);
    assertThat(infoLogs.get(0)).startsWith("Parsing the Visual Studio Test Results file ");

    var debugLogs = logTester.logs(Level.DEBUG);
    assertThat(debugLogs)
      .hasSize(3)
      .contains(
        "Parsed Visual Studio Unit Test - testId: ab6094d2-07ab-58c5-03e0-a4fb244efd26 outcome: Passed, duration: 24",
        "Parsed Visual Studio Unit Test - testId: ab6094d2-07ab-58c5-03e0-a4fb244efd26 outcome: Passed, duration: 2",
        "Added Test Method: TestReport.TestReport.Test1.TestMethod1 to File: Sample.cs"
      );
  }

  @Test
  public void test_name_not_mapped() {
    var sut = new VisualStudioTestResultParser();
    var file = new File("src/test/resources/visualstudio_test_results/test_name_not_mapped.trx");

    sut.parse(file, new HashMap<>(), Map.of("Some.Other.TestMethod", "C:\\dev\\Playground\\NUnit.Test\\Sample.cs"));

    List<String> debugLogs = logTester.logs(Level.DEBUG);
    assertThat(debugLogs).hasSize(2);
    assertThat(debugLogs.get(1)).isEqualTo("Test method Playground.Test.TestProject1.UnitTest1.TestShouldFail cannot be mapped to the test source file. The test will not be included.");
  }

  @Test
  public void invalid_date() {
    Map<String, UnitTestResults> results = new HashMap<>();
    var sut = new VisualStudioTestResultParser();
    var file = new File("src/test/resources/visualstudio_test_results/invalid_dates.trx");

    var exception = assertThrows(ParseErrorException.class, () -> sut.parse(file, results, new HashMap<>()));
    assertThat(exception.getMessage()).startsWith("Expected a valid date and time instead of \"2016-xx-14T17:04:31.100+01:00\" for the attribute \"startTime\". Unparseable date: \"2016-xx-14T17:04:31.100+01:00\" in ");
  }

  @Test
  public void invalid_character_fail() {
    Map<String, UnitTestResults> results = new HashMap<>();
    var sut = new VisualStudioTestResultParser();
    var file = new File("src/test/resources/visualstudio_test_results/invalid_character.trx");

    var exception = assertThrows(IllegalStateException.class, () -> sut.parse(file, results, new HashMap<>()));

    assertThat(exception.getMessage()).startsWith("Error while parsing the XML file:");
  }

  @Test
  public void test_result_no_test_method() {
    Map<String, UnitTestResults> results = new HashMap<>();
    var sut = new VisualStudioTestResultParser();
    var file = new File("src/test/resources/visualstudio_test_results/test_result_no_test_method.trx");

    var exception =  assertThrows(ParseErrorException.class, () -> sut.parse(file, results, new HashMap<>()));
    assertEquals("No TestMethod attribute found on UnitTest tag", exception.getMessage());
  }

  @Test
  public void invalid_test_outcome() {
    Map<String, UnitTestResults> results = new HashMap<>();
    Map<String, String> fileMap = Map.of("Playground.Test.TestProject1.UnitTest1.InvalidOutcome", "C:\\dev\\Playground\\Playground.Test\\Sample.cs");
    var sut = new VisualStudioTestResultParser();
    var file = new File("src/test/resources/visualstudio_test_results/invalid_test_outcome.trx");

    var exception = assertThrows(IllegalArgumentException.class, () -> sut.parse(file, results, fileMap));
    assertEquals("Outcome of unit test must match VSTest Format", exception.getMessage());
  }

  @Test
  public void nunit_project_with_vs_logger() {
    Map<String, UnitTestResults> results = new HashMap<>();
    Map<String, String> fileMap = new HashMap<>() {
      {
        put("Calculator.NUnit3.Calculator.NUnit3.GenericTests.GenericTest", "Sample.cs");
        put("Calculator.NUnit3.Calculator.NUnit3.Derived.TestMethodInBaseClass", "Sample.cs");
        put("Calculator.NUnit3.Calculator.NUnit3.Tests.TestMethod1", "Sample.cs");
        put("Calculator.NUnit3.Calculator.NUnit3.Derived.VirtualMethodInBaseClass", "Sample.cs");
      }
    };

    var sut = new VisualStudioTestResultParser();

    sut.parse(new File("src/test/resources/visualstudio_test_results/nunitproject_with_vs_logger.trx"), results, fileMap);

    assertThat(results).hasSize(1);
    assertThat(results.get("Sample.cs").tests()).isEqualTo(6);
    assertThat(results.get("Sample.cs").failures()).isZero();
    assertThat(results.get("Sample.cs").skipped()).isZero();
    assertThat(results.get("Sample.cs").errors()).isZero();
    assertThat(results.get("Sample.cs").executionTime()).isEqualTo(10);

    assertThat(logTester.logs(Level.WARN)).isEmpty();

    var infoLogs = logTester.logs(Level.INFO);
    assertThat(infoLogs).hasSize(1);
    assertThat(infoLogs.get(0)).startsWith("Parsing the Visual Studio Test Results file ");

    var debugLogs = logTester.logs(Level.DEBUG);
    assertThat(debugLogs)
      .hasSize(12)
      .contains(
        "Parsed Visual Studio Unit Test - testId: dfbab7a8-4d30-e4af-b7e9-5f143fa0d63c outcome: Passed, duration: 0",
        "Parsed Visual Studio Unit Test - testId: e856dddd-b0b9-1283-7d12-995daa1c2159 outcome: Passed, duration: 0",
        "Parsed Visual Studio Unit Test - testId: 04f8f26c-73f6-0a50-702c-3424ece7fca7 outcome: Passed, duration: 1",
        "Parsed Visual Studio Unit Test - testId: d19495ce-8556-0afa-0598-f28569986b94 outcome: Passed, duration: 2",
        "Parsed Visual Studio Unit Test - testId: b1d90856-8e97-2f05-b5a1-46236018b4f5 outcome: Passed, duration: 0",
        "Parsed Visual Studio Unit Test - testId: 7c7642ed-18fb-4d66-c0f5-3e220f7aa426 outcome: Passed, duration: 7",
        "Added Test Method: Calculator.NUnit3.Calculator.NUnit3.GenericTests.GenericTest to File: Sample.cs",
        "Added Test Method: Calculator.NUnit3.Calculator.NUnit3.GenericTests.GenericTest to File: Sample.cs",
        "Added Test Method: Calculator.NUnit3.Calculator.NUnit3.Derived.TestMethodInBaseClass to File: Sample.cs",
        "Added Test Method: Calculator.NUnit3.Calculator.NUnit3.GenericTests.GenericTest to File: Sample.cs",
        "Added Test Method: Calculator.NUnit3.Calculator.NUnit3.Tests.TestMethod1 to File: Sample.cs",
        "Added Test Method: Calculator.NUnit3.Calculator.NUnit3.Derived.VirtualMethodInBaseClass to File: Sample.cs"
      );
  }
}
