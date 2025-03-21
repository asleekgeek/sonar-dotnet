/*
 * SonarSource :: C# :: Core
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
package org.sonarsource.csharp.core;

import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class CSharpCorePluginMetadataTest {

  @Test
  void pluginProperties() {
    CSharpCorePluginMetadata sut = new CSharpCorePluginMetadata() {
      @Override
      public String pluginKey() {
        return null;
      }

      @Override
      public String analyzerProjectName() {
        return null;
      }

      @Override
      public String resourcesDirectory() {
        return null;
      }
    };
    assertThat(sut.languageKey()).isEqualTo("cs");
    assertThat(sut.languageName()).isEqualTo("C#");
    assertThat(sut.repositoryKey()).isEqualTo("csharpsquid");
    assertThat(sut.fileSuffixesKey()).isEqualTo("sonar.cs.file.suffixes");
    assertThat(sut.fileSuffixesDefaultValue()).isEqualTo(".cs,.razor");
  }
}
