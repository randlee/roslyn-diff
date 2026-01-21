namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for TFM (Target Framework Moniker) support in <see cref="HtmlFormatter"/>.
/// </summary>
public class HtmlFormatterTfmTests
{
    private readonly HtmlFormatter _formatter = new();

    #region TFM Badge Tests

    [Fact]
    public void Format_WithTfmSpecificChange_ShouldShowTfmBadge()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 },
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("tfm-badge", "HTML should include TFM badge CSS class");
        html.Should().Contain(".NET 10.0", "HTML should display formatted TFM");
    }

    [Fact]
    public void Format_WithMultipleTfmChange_ShouldShowAllTfms()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net9.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 },
                            ApplicableToTfms = new[] { "net8.0", "net10.0" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("tfm-badge");
        html.Should().Contain(".NET 8.0");
        html.Should().Contain(".NET 10.0");
    }

    [Fact]
    public void Format_WithChangeApplyingToAllTfms_ShouldNotShowBadge()
    {
        // Arrange - Empty ApplicableToTfms means applies to all
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "UniversalMethod",
                            NewContent = "public void UniversalMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 },
                            ApplicableToTfms = null // Applies to all TFMs
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert - Should not show TFM badge for universal changes
        var changeSection = ExtractChangeSection(html, "UniversalMethod");
        changeSection.Should().NotContain("tfm-badge", "Universal changes should not have TFM badges");
    }

    [Fact]
    public void Format_WithNoTfmAnalysis_ShouldNotShowTfmBadges()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = null, // No TFM analysis
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 },
                            ApplicableToTfms = new[] { "net8.0" } // Even if specified, shouldn't show
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert - Check for actual badge span, not just CSS class definition
        html.Should().NotContain("<span class=\"tfm-badge\">", "No TFM badge spans when no TFM analysis performed");
    }

    [Fact]
    public void Format_WithEmptyAnalyzedTfms_ShouldNotShowTfmBadges()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = Array.Empty<string>(), // Empty list
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            NewContent = "public void NewMethod() { }",
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert - Check for actual badge span, not just CSS class definition
        html.Should().NotContain("<span class=\"tfm-badge\">");
    }

    #endregion

    #region TFM Metadata Tests

    [Fact]
    public void Format_WithAnalyzedTfms_ShouldShowInMetadata()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            Stats = new DiffStats()
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("Frameworks:", "Should display frameworks label");
        html.Should().Contain(".NET 8.0", "Should display first framework");
        html.Should().Contain(".NET 10.0", "Should display second framework");
    }

    [Fact]
    public void Format_WithSingleTfm_ShouldShowInMetadata()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0" },
            Stats = new DiffStats()
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("Frameworks:");
        html.Should().Contain(".NET 8.0");
    }

    [Fact]
    public void Format_WithNoTfms_ShouldNotShowFrameworksInMetadata()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = null,
            Stats = new DiffStats()
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().NotContain("Frameworks:", "Should not show frameworks when none analyzed");
    }

    [Fact]
    public void Format_WithEmptyTfmsList_ShouldNotShowFrameworksInMetadata()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = Array.Empty<string>(),
            Stats = new DiffStats()
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().NotContain("Frameworks:");
    }

    #endregion

    #region Nested Changes TFM Tests

    [Fact]
    public void Format_WithNestedTfmSpecificChanges_ShouldShowBadgesOnChildren()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "TestClass",
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "Net10Method",
                                    NewContent = "public void Net10Method() { }",
                                    NewLocation = new Location { StartLine = 5, EndLine = 5 },
                                    ApplicableToTfms = new[] { "net10.0" }
                                },
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "UniversalMethod",
                                    NewContent = "public void UniversalMethod() { }",
                                    NewLocation = new Location { StartLine = 8, EndLine = 8 },
                                    ApplicableToTfms = null // Applies to all
                                }
                            ]
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1, Additions = 2 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("Net10Method");
        var net10Section = ExtractChangeSection(html, "Net10Method");
        net10Section.Should().Contain("tfm-badge");
        net10Section.Should().Contain(".NET 10.0");

        var universalSection = ExtractChangeSection(html, "UniversalMethod");
        universalSection.Should().NotContain("tfm-badge");
    }

    [Fact]
    public void Format_WithMultipleLevelsOfNesting_ShouldShowTfmBadgesAtAllLevels()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Namespace,
                            Name = "MyNamespace",
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Modified,
                                    Kind = ChangeKind.Class,
                                    Name = "MyClass",
                                    ApplicableToTfms = new[] { "net10.0" },
                                    Children =
                                    [
                                        new Change
                                        {
                                            Type = ChangeType.Added,
                                            Kind = ChangeKind.Method,
                                            Name = "DeepMethod",
                                            NewContent = "void DeepMethod() { }",
                                            ApplicableToTfms = new[] { "net10.0" }
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 2, Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        var classSection = ExtractChangeSection(html, "MyClass");
        classSection.Should().Contain("tfm-badge");
        classSection.Should().Contain(".NET 10.0");

        var methodSection = ExtractChangeSection(html, "DeepMethod");
        methodSection.Should().Contain("tfm-badge");
    }

    #endregion

    #region TFM Formatting Tests

    [Fact]
    public void Format_WithNet8Tfm_ShouldFormatAsDotNet80()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain(".NET 8.0");
        html.Should().NotContain("net8.0", "Raw TFM should be formatted");
    }

    [Fact]
    public void Format_WithNetStandardTfm_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "netstandard2.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            ApplicableToTfms = new[] { "netstandard2.0" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain(".NET Standard 2.0");
    }

    [Fact]
    public void Format_WithNetFrameworkTfm_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net48" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            ApplicableToTfms = new[] { "net48" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain(".NET Framework 4.8");
    }

    [Fact]
    public void Format_WithNetCoreAppTfm_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "netcoreapp3.1" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            ApplicableToTfms = new[] { "netcoreapp3.1" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain(".NET Core 3.1");
    }

    #endregion

    #region HTML Structure Tests

    [Fact]
    public void Format_WithTfmBadge_ShouldHaveCorrectCssClass()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("<span class=\"tfm-badge\">");
    }

    [Fact]
    public void Format_ShouldIncludeTfmBadgeStyles()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain(".tfm-badge");
        html.Should().Contain("background-color: #e7f5ff");
        html.Should().Contain("color: #1971c2");
    }

    [Fact]
    public void Format_WithTfmBadge_ShouldBeProperlyEscaped()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert - Should not contain raw angle brackets in badge content
        var badgeContent = ExtractTfmBadgeContent(html);
        badgeContent.Should().NotContain("<");
        badgeContent.Should().NotContain(">");
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void Format_WithoutTfmProperties_ShouldStillWork()
    {
        // Arrange - Old-style result without TFM properties
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "OldMethod",
                            NewContent = "public void OldMethod() { }"
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert - Should work without errors
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("OldMethod");
        html.Should().NotContain("<span class=\"tfm-badge\">");
        html.Should().NotContain("Frameworks:");
    }

    [Fact]
    public void Format_MixedTfmAndNonTfmChanges_ShouldHandleBoth()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "SpecificMethod",
                            ApplicableToTfms = new[] { "net8.0" }
                        },
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "UniversalMethod",
                            ApplicableToTfms = null
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 2 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("SpecificMethod");
        html.Should().Contain("UniversalMethod");

        var specificSection = ExtractChangeSection(html, "SpecificMethod");
        specificSection.Should().Contain("tfm-badge");

        var universalSection = ExtractChangeSection(html, "UniversalMethod");
        universalSection.Should().NotContain("tfm-badge");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Format_CompleteScenario_ShouldShowAllTfmFeatures()
    {
        // Arrange - Comprehensive scenario with multiple TFMs and changes
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            OldPath = "/path/to/old.cs",
            NewPath = "/path/to/new.cs",
            FileChanges =
            [
                new FileChange
                {
                    Path = "MyClass.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Net10OnlyMethod",
                            NewContent = "#if NET10_0\npublic void Net10OnlyMethod() { }\n#endif",
                            NewLocation = new Location { StartLine = 10, EndLine = 12 },
                            ApplicableToTfms = new[] { "net10.0" },
                            Impact = ChangeImpact.NonBreaking
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "SharedMethod",
                            OldContent = "public void SharedMethod() { }",
                            NewContent = "public void SharedMethod() { /* updated */ }",
                            OldLocation = new Location { StartLine = 20, EndLine = 20 },
                            NewLocation = new Location { StartLine = 20, EndLine = 20 },
                            ApplicableToTfms = null, // Applies to all
                            Impact = ChangeImpact.NonBreaking
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1, Modifications = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert - Verify all TFM features are present

        // 1. Metadata section shows analyzed TFMs
        html.Should().Contain("Frameworks:");
        html.Should().Contain(".NET 8.0");
        html.Should().Contain(".NET 10.0");

        // 2. TFM-specific change shows badge
        var net10Section = ExtractChangeSection(html, "Net10OnlyMethod");
        net10Section.Should().Contain("tfm-badge");
        net10Section.Should().Contain(".NET 10.0");

        // 3. Universal change doesn't show badge
        var sharedSection = ExtractChangeSection(html, "SharedMethod");
        sharedSection.Should().NotContain("tfm-badge");

        // 4. CSS styles are present
        html.Should().Contain(".tfm-badge");

        // 5. Valid HTML structure
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("</html>");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts the change section HTML for a specific change by name.
    /// </summary>
    private static string ExtractChangeSection(string html, string changeName)
    {
        var startIndex = html.IndexOf($"change-name\">{changeName}<", StringComparison.Ordinal);
        if (startIndex == -1)
        {
            return string.Empty;
        }

        // Find the start of the change section (go back to find the opening div)
        var sectionStart = html.LastIndexOf("<div class=\"change-section", startIndex, StringComparison.Ordinal);
        if (sectionStart == -1)
        {
            sectionStart = 0;
        }

        // Find the end of this change section (next change-section or end of changes-list)
        var sectionEnd = html.IndexOf("<div class=\"change-section", startIndex + 1, StringComparison.Ordinal);
        if (sectionEnd == -1)
        {
            sectionEnd = html.IndexOf("</div><!--changes-list-->", startIndex, StringComparison.Ordinal);
            if (sectionEnd == -1)
            {
                sectionEnd = html.Length;
            }
        }

        return html.Substring(sectionStart, sectionEnd - sectionStart);
    }

    /// <summary>
    /// Extracts the content of TFM badges from HTML.
    /// </summary>
    private static string ExtractTfmBadgeContent(string html)
    {
        var startTag = "<span class=\"tfm-badge\">";
        var endTag = "</span>";

        var startIndex = html.IndexOf(startTag, StringComparison.Ordinal);
        if (startIndex == -1)
        {
            return string.Empty;
        }

        startIndex += startTag.Length;
        var endIndex = html.IndexOf(endTag, startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
        {
            return string.Empty;
        }

        return html.Substring(startIndex, endIndex - startIndex);
    }

    #endregion
}
