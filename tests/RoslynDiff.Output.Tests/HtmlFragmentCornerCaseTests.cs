namespace RoslynDiff.Output.Tests;

using System.Collections.Concurrent;
using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Corner case tests for HTML Fragment Mode.
/// Tests CSS file permission errors, concurrent writes, special characters, and edge cases.
/// </summary>
public class HtmlFragmentCornerCaseTests : IDisposable
{
    private readonly HtmlFormatter _formatter = new();
    private readonly string _tempDirectory;

    public HtmlFragmentCornerCaseTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"roslyn-diff-fragment-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                // Remove read-only attributes before cleanup
                RemoveReadOnlyAttributes(_tempDirectory);
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private void RemoveReadOnlyAttributes(string path)
    {
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attr = File.GetAttributes(file);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);
                }
            }
            catch { }
        }
    }

    #region CSS File Write Permission Tests (HIGH Priority - F1-H1)

    [Fact]
    public void Format_FragmentMode_WithReadOnlyDirectory_ShouldHandleGracefully()
    {
        // Arrange
        var readOnlyDir = Path.Combine(_tempDirectory, "readonly");
        Directory.CreateDirectory(readOnlyDir);

        // Make directory read-only
        var dirInfo = new DirectoryInfo(readOnlyDir);
        dirInfo.Attributes |= FileAttributes.ReadOnly;

        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(readOnlyDir, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        try
        {
            // Act
            var act = () => _formatter.FormatResult(result, options);

            // Assert - Should either throw clear exception or skip CSS with warning
            // Should NOT crash with obscure IOException
            try
            {
                var html = act.Should().NotThrow("should handle permission errors gracefully").Which;
            }
            catch (UnauthorizedAccessException ex)
            {
                // This is acceptable - clear error message
                ex.Message.Should().NotBeNullOrWhiteSpace();
            }
            catch (IOException ex)
            {
                // This is acceptable if the message is clear
                ex.Message.Should().Contain("CSS", "error should mention CSS file");
            }
        }
        finally
        {
            // Cleanup
            dirInfo.Attributes &= ~FileAttributes.ReadOnly;
        }
    }

    [Fact]
    public void Format_FragmentMode_WithLockedCssFile_ShouldHandleGracefully()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");

        // Pre-create and lock the CSS file
        File.WriteAllText(cssPath, "/* locked */");
        using var lockedFile = File.Open(cssPath, FileMode.Open, FileAccess.Read, FileShare.None);

        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var act = () => _formatter.FormatResult(result, options);

        // Assert - Should handle locked file gracefully
        try
        {
            var html = act.Should().NotThrow("should handle locked CSS file").Which;
        }
        catch (IOException ex)
        {
            // Acceptable if error is clear
            ex.Message.Should().NotBeNullOrWhiteSpace();
        }
    }

    #endregion

    #region CSS Path Special Characters Tests (HIGH Priority - F1-H2)

    [Fact]
    public void Format_FragmentMode_WithSpacesInCssFilename_ShouldWork()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            ExtractCssPath = "my styles (2).css"
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("href=\"my styles (2).css\"");
        var cssPath = Path.Combine(_tempDirectory, "my styles (2).css");
        File.Exists(cssPath).Should().BeTrue();
    }

    [Fact]
    public void Format_FragmentMode_WithUnicodeCssFilename_ShouldWork()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            ExtractCssPath = "样式.css"  // Chinese characters
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("样式.css");
        var cssPath = Path.Combine(_tempDirectory, "样式.css");
        File.Exists(cssPath).Should().BeTrue();
    }

    [Fact]
    public void Format_FragmentMode_WithSpecialCharsInCssPath_ShouldHandleGracefully()
    {
        // Arrange - Test various special characters
        var testNames = new[]
        {
            "path%20encoded.css",    // URL encoded
            "file&name.css",         // Ampersand
            "file'name.css",         // Single quote
        };

        foreach (var cssName in testNames)
        {
            var result = CreateSimpleDiffResult();
            var htmlOutputPath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}.html");
            var options = new OutputOptions
            {
                HtmlMode = HtmlMode.Fragment,
                HtmlOutputPath = htmlOutputPath,
                ExtractCssPath = cssName
            };

            // Act
            var act = () => _formatter.FormatResult(result, options);

            // Assert - Should handle gracefully (either work or fail with clear error)
            act.Should().NotThrow($"should handle CSS name: {cssName}");
        }
    }

    #endregion

    #region Concurrent CSS File Writes Tests (HIGH Priority - F1-H4)

    [Fact]
    public void Format_FragmentMode_ConcurrentWritesToSameCss_ShouldNotCorrupt()
    {
        // Arrange - Create 50 fragments in parallel writing to same CSS
        var results = Enumerable.Range(0, 50)
            .Select(i => CreateSimpleDiffResult($"Test{i}.cs"))
            .ToList();

        var exceptions = new ConcurrentBag<Exception>();
        var htmlFiles = new ConcurrentBag<string>();

        // Act - Generate all fragments in parallel
        Parallel.ForEach(results, (result, _, index) =>
        {
            try
            {
                var htmlOutputPath = Path.Combine(_tempDirectory, $"test{index}.html");
                var options = new OutputOptions
                {
                    HtmlMode = HtmlMode.Fragment,
                    HtmlOutputPath = htmlOutputPath
                };

                var html = _formatter.FormatResult(result, options);
                htmlFiles.Add(htmlOutputPath);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty("no exceptions should occur during concurrent writes");

        // Verify CSS file is valid and not corrupted
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        File.Exists(cssPath).Should().BeTrue();

        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().NotBeNullOrWhiteSpace();
        cssContent.Should().Contain(".roslyn-diff-fragment");

        // CSS should not be truncated or contain garbage
        cssContent.Length.Should().BeGreaterThan(100, "CSS should have reasonable size");
    }

    [Fact]
    public void Format_FragmentMode_RapidSequentialWrites_ShouldSucceed()
    {
        // Arrange - Rapidly write 100 fragments sequentially
        var results = Enumerable.Range(0, 100)
            .Select(i => CreateSimpleDiffResult($"Test{i}.cs"))
            .ToList();

        // Act
        for (int i = 0; i < results.Count; i++)
        {
            var htmlOutputPath = Path.Combine(_tempDirectory, $"rapid{i}.html");
            var options = new OutputOptions
            {
                HtmlMode = HtmlMode.Fragment,
                HtmlOutputPath = htmlOutputPath
            };

            var act = () => _formatter.FormatResult(results[i], options);

            // Assert
            act.Should().NotThrow($"write {i} should succeed");
        }

        // Verify CSS is still valid after all writes
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().Contain(".roslyn-diff-fragment");
    }

    #endregion

    #region HTML Output Path Without Directory Tests (HIGH Priority - F1-H5)

    [Fact]
    public void Format_FragmentMode_WithFilenameOnly_ShouldUseCwd()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = "fragment.html"  // No directory component
        };

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            // Change to temp directory
            Directory.SetCurrentDirectory(_tempDirectory);

            // Act
            var html = _formatter.FormatResult(result, options);

            // Assert
            html.Should().Contain("href=\"roslyn-diff.css\"");

            // CSS should be written to current working directory
            var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
            File.Exists(cssPath).Should().BeTrue("CSS should be in current directory");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    #endregion

    #region Data Attributes Special Characters Tests (MEDIUM Priority - F1-M3, F1-M4)

    [Fact]
    public void Format_FragmentMode_WithHtmlSpecialCharsInFilename_ShouldEscape()
    {
        // Arrange - Test HTML special characters in filenames
        var result = new DiffResult
        {
            OldPath = "File<T>.cs",
            NewPath = "File<T>.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "File<T>.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "Class<T>",
                            OldContent = "class Test { }",
                            NewContent = "class Test { void Method() { } }",
                            OldLocation = new Location { StartLine = 1, EndLine = 1 },
                            NewLocation = new Location { StartLine = 1, EndLine = 1 }
                        }
                    ]
                }
            ]
        };

        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - HTML special characters should be escaped
        html.Should().Contain("&lt;", "< should be escaped to &lt;");
        html.Should().Contain("&gt;", "> should be escaped to &gt;");
        html.Should().NotContain("File<T>", "raw < and > should not appear in HTML");
    }

    [Fact]
    public void Format_FragmentMode_WithAmpersandInFilename_ShouldEscape()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "File&Test.cs",
            NewPath = "File&Test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = []
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
        html.Should().Contain("&amp;", "& should be escaped to &amp;");
    }

    [Fact]
    public void Format_FragmentMode_WithQuotesInFilename_ShouldEscape()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "File\"Test\".cs",
            NewPath = "File\"Test\".cs",
            Mode = DiffMode.Roslyn,
            FileChanges = []
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
        html.Should().Contain("&quot;", "\" should be escaped in attribute values");
    }

    [Fact]
    public void Format_FragmentMode_CssLinkHrefWithSpecialChars_ShouldEscape()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            ExtractCssPath = "my&styles.css"
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - href attribute should escape ampersand
        html.Should().Contain("href=\"my&amp;styles.css\"", "href should escape &");
    }

    #endregion

    #region Null/Empty Path Tests (MEDIUM Priority - F1-M6)

    [Fact]
    public void Format_FragmentMode_WithNullOldAndNewPath_ShouldHandleGracefully()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = null,
            NewPath = null,
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "unknown.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Test",
                            OldContent = "old",
                            NewContent = "new",
                            OldLocation = new Location { StartLine = 1, EndLine = 1 },
                            NewLocation = new Location { StartLine = 1, EndLine = 1 }
                        }
                    ]
                }
            ]
        };

        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Should not crash, data attributes should be empty or omitted
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("<div class=\"roslyn-diff-fragment\"");
    }

    [Fact]
    public void Format_FragmentMode_WithEmptyChanges_ShouldRenderEmptyFragment()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = []  // Empty changes
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
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("<div class=\"roslyn-diff-fragment\"");
    }

    #endregion

    #region Very Long Path Tests (MEDIUM Priority - F1-M1)

    [Fact]
    public void Format_FragmentMode_WithVeryLongCssPath_ShouldHandleOrReject()
    {
        // Arrange - Create a very long path (300+ characters)
        var longPath = string.Join("/", Enumerable.Range(0, 30).Select(i => "subdir"));
        var cssName = $"{longPath}/styles.css";

        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlOutputPath,
            ExtractCssPath = cssName
        };

        // Act
        var act = () => _formatter.FormatResult(result, options);

        // Assert - Should either:
        // 1. Handle gracefully with directory creation
        // 2. Throw PathTooLongException with clear message
        // 3. Throw ArgumentException about invalid path
        try
        {
            var html = act.Should().NotThrow("should handle or reject with clear error").Which;
        }
        catch (PathTooLongException ex)
        {
            // Acceptable
            ex.Message.Should().NotBeNullOrWhiteSpace();
        }
        catch (DirectoryNotFoundException)
        {
            // Acceptable - path is too long to create
        }
    }

    #endregion

    #region Helper Methods

    private DiffResult CreateSimpleDiffResult(string? fileName = null)
    {
        return new DiffResult
        {
            OldPath = fileName ?? "test.cs",
            NewPath = fileName ?? "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = fileName ?? "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = "public void TestMethod() { }",
                            NewContent = "public void TestMethod() { Console.WriteLine(\"test\"); }",
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
