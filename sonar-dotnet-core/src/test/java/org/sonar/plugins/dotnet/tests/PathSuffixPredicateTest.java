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

import java.net.URI;
import java.net.URISyntaxException;
import java.util.Arrays;
import java.util.Collection;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Parameterized;
import org.sonar.api.batch.fs.InputFile;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

@RunWith(Parameterized.class)
public class PathSuffixPredicateTest {

  private String pathSuffix;
  private String absolutePath;
  private boolean expectedResult;

  public PathSuffixPredicateTest(String pathSuffix, String absolutePath, boolean expectedResult) {
    this.pathSuffix = pathSuffix;
    this.absolutePath = absolutePath;
    this.expectedResult = expectedResult;
  }

  @Parameterized.Parameters
  public static Collection input() {
    return Arrays.asList(new Object[][] {
        // Match
        {"", "", true},
        {"", "file.cs", true},
        {"some/path/file.cs", "/_/some/path/file.cs", true},
        {"some/path/file.cs", "/_/X/Y/some/path/file.cs", true},
        {"some/path/file.cs", "/home/test/long/path/some/path/file.cs", true},
        {"some/path/file.cs", "some/path/file.cs", true},
        // No match
        {"some/PATH/file.cs", "some/path/file.cs", false},
        {"some/path/file.cs", "some/PATH/file.cs", false},
        {"some/path/file.cs", "/_/some/PATH/file.cs", false},
        {"some/path/file.cs", "/path/file.cs", false},
        {"file.cs", "", false},
        {"foo", "bar", false},
        {"foo", "", false},
        {"\\some\\path\\file.cs", "/some/path/file.cs", false}
      }
    );
  }

  @Test
  public void test_apply() throws URISyntaxException {
    PathSuffixPredicate predicate = new PathSuffixPredicate(pathSuffix);
    assertThat(predicate.apply(mockInput(absolutePath))).isEqualTo(expectedResult);
  }

  private InputFile mockInput(String path) throws URISyntaxException {
    URI uri = new URI(path);
    InputFile inputFile = mock(InputFile.class);
    when(inputFile.uri()).thenReturn(uri);
    return inputFile;
  }
}
