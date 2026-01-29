namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for HTML Fragment Mode functionality.
/// Tests fragment generation, external CSS handling, and data attributes.
/// </summary>
public class HtmlFragmentTests : IDisposable
{
    private readonly HtmlFormatter _formatter = new();
    private readonly string _tempDirectory;

    public HtmlFragmentTests()
    {
        // Create temporary directory for CSS file output
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        // Clean up temporary directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region Fragment Structure Tests

    [Fact]
    public void Format_FragmentMode_ExcludesDocumentWrapper()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().NotContain("<!DOCTYPE html>");
        html.Should().NotContain("<html");
        html.Should().NotContain("</html>");
        html.Should().NotContain("<head>");
        html.Should().NotContain("</head>");
        html.Should().NotContain("<body>");
        html.Should().NotContain("</body>");
    }

    [Fact]
    public void Format_FragmentMode_ContainsRootFragmentDiv()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("<div class=\"roslyn-diff-fragment\"");
        html.Should().Contain("</div>");
    }

    [Fact]
    public void Format_FragmentMode_DoesNotContainEmbeddedStyles()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().NotContain("<style>");
        html.Should().NotContain("</style>");
    }

    [Fact]
    public void Format_FragmentMode_DoesNotContainEmbeddedScript()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().NotContain("<script>");
        html.Should().NotContain("</script>");
    }

    [Fact]
    public void Format_DocumentMode_ContainsFullHtmlStructure()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Document
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Verify document mode is unchanged (backward compatibility)
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html");
        html.Should().Contain("<head>");
        html.Should().Contain("<body>");
        html.Should().Contain("<style>");
        html.Should().Contain("<script>");
    }

    #endregion

    #region External CSS Tests

    [Fact]
    public void Format_FragmentMode_ReferencesExternalCss()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("<link rel=\"stylesheet\" href=\"roslyn-diff.css\">");
    }

    [Fact]
    public void Format_FragmentMode_GeneratesExternalCssFile()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        _formatter.FormatResult(result, options);

        // Assert
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        File.Exists(cssPath).Should().BeTrue("CSS file should be generated");

        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().NotBeNullOrWhiteSpace();
        cssContent.Should().Contain(".roslyn-diff-fragment", "CSS should be scoped to fragment container");
    }

    [Fact]
    public void Format_FragmentMode_CssFileContainsRequiredStyles()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        _formatter.FormatResult(result, options);

        // Assert
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        var cssContent = File.ReadAllText(cssPath);

        cssContent.Should().Contain(":root", "CSS should define color variables");
        cssContent.Should().Contain("--color-added-bg");
        cssContent.Should().Contain("--color-removed-bg");
        cssContent.Should().Contain("--color-modified-bg");
        cssContent.Should().Contain(".diff-container");
        cssContent.Should().Contain(".line-added");
        cssContent.Should().Contain(".line-removed");
        cssContent.Should().Contain(".line-modified");
    }

    [Fact]
    public void Format_FragmentMode_WithCustomCssFilename_UsesCustomName()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var customCssName = "custom-styles.css";
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            ExtractCssPath = customCssName
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain($"<link rel=\"stylesheet\" href=\"{customCssName}\">");

        var cssPath = Path.Combine(_tempDirectory, customCssName);
        File.Exists(cssPath).Should().BeTrue("Custom CSS file should be generated");
    }

    [Fact]
    public void Format_FragmentMode_DefaultCssFilename_IsRoslynDiffCss()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
            // ExtractCssPath not specified, should use default
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("<link rel=\"stylesheet\" href=\"roslyn-diff.css\">");

        var defaultCssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        File.Exists(defaultCssPath).Should().BeTrue();
    }

    [Fact]
    public void Format_FragmentMode_WithoutOutputPath_SkipsCssFileGeneration()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment
            // HtmlOutputPath not specified
        };

        // Act - Should not throw
        var act = () => _formatter.FormatResult(result, options);

        // Assert
        act.Should().NotThrow("Fragment generation should handle missing output path gracefully");
    }

    #endregion

    #region Data Attributes Tests

    [Fact]
    public void Format_FragmentMode_ContainsDataAttributesForFiles()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "/path/to/OldFile.cs",
            NewPath = "/path/to/NewFile.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats()
        };
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("data-old-file=\"OldFile.cs\"");
        html.Should().Contain("data-new-file=\"NewFile.cs\"");
    }

    [Fact]
    public void Format_FragmentMode_ContainsDataAttributesForStats()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats
            {
                Additions = 5,
                Deletions = 3,
                Modifications = 2,
                Moves = 1,
                Renames = 4
            }
        };
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("data-changes-total=\"15\"");
        html.Should().Contain("data-changes-added=\"5\"");
        html.Should().Contain("data-changes-removed=\"3\"");
        html.Should().Contain("data-changes-modified=\"2\"");
    }

    [Fact]
    public void Format_FragmentMode_ContainsDataAttributesForImpact()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats
            {
                BreakingPublicApiCount = 2,
                BreakingInternalApiCount = 1,
                NonBreakingCount = 5,
                FormattingOnlyCount = 3
            }
        };
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("data-impact-breaking-public=\"2\"");
        html.Should().Contain("data-impact-breaking-internal=\"1\"");
        html.Should().Contain("data-impact-non-breaking=\"5\"");
        html.Should().Contain("data-impact-formatting=\"3\"");
    }

    [Fact]
    public void Format_FragmentMode_ContainsDataAttributeForMode()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats()
        };
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("data-mode=\"roslyn\"");
    }

    [Fact]
    public void Format_FragmentMode_LineMode_ContainsCorrectDataAttribute()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Line,
            FileChanges = [],
            Stats = new DiffStats()
        };
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("data-mode=\"line\"");
    }

    #endregion

    #region Content Tests

    [Fact]
    public void Format_FragmentMode_ContainsSummarySection()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            IncludeStats = true
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Fragment should include summary section like document mode
        html.Should().Contain("changes");
    }

    [Fact]
    public void Format_FragmentMode_ContainsDiffContent()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            IncludeContent = true
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("TestMethod");
        html.Should().Contain("diff-container");
    }

    [Fact]
    public void Format_FragmentMode_WithChanges_ShowsAllChangeTypes()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "AddedMethod",
                            NewContent = "public void AddedMethod() { }",
                            NewLocation = new Location { StartLine = 1, EndLine = 1 }
                        },
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "RemovedMethod",
                            OldContent = "public void RemovedMethod() { }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "ModifiedMethod",
                            OldContent = "public void ModifiedMethod() { }",
                            NewContent = "public void ModifiedMethod() { return; }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1, Deletions = 1, Modifications = 1 }
        };
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            IncludeContent = true
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("AddedMethod");
        html.Should().Contain("RemovedMethod");
        html.Should().Contain("ModifiedMethod");
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void Format_DefaultOptions_UsesDocumentMode()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions();

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Default should be document mode
        options.HtmlMode.Should().Be(HtmlMode.Document);
        html.Should().StartWith("<!DOCTYPE html>");
    }

    [Fact]
    public void Format_DocumentMode_UnchangedFromPreviousBehavior()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Document
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Verify all expected elements from original implementation
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html");
        html.Should().Contain("<head>");
        html.Should().Contain("<title>");
        html.Should().Contain("<style>");
        html.Should().Contain("</style>");
        html.Should().Contain("</head>");
        html.Should().Contain("<body>");
        html.Should().Contain("<script>");
        html.Should().Contain("</script>");
        html.Should().Contain("</body>");
        html.Should().Contain("</html>");
    }

    #endregion

    #region Helper Methods

    private static DiffResult CreateSimpleDiffResult()
    {
        return new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = "public void TestMethod() { }",
                            NewContent = "public void TestMethod() { return; }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
    }

    #endregion
}
