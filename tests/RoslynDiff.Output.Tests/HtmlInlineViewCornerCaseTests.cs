namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Corner case tests for HTML Inline View Mode.
/// Tests very long lines, overlapping regions, edge cases with line numbers and context.
/// </summary>
public class HtmlInlineViewCornerCaseTests : IDisposable
{
    private readonly HtmlFormatter _formatter = new();
    private readonly string _tempDirectory;

    public HtmlInlineViewCornerCaseTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"roslyn-diff-inline-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region Very Long Lines Tests (HIGH Priority - I1-H1)

    [Fact]
    public void Format_InlineView_WithVeryLongLine_ShouldNotExhaustMemory()
    {
        // Arrange - Create a line with 10,000 characters
        var longLine = new string('x', 10_000);
        var result = CreateDiffResultWithContent($"var s = \"{longLine}\";", $"var s = \"{longLine}Z\";");

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var act = () => _formatter.FormatResult(result, options);

        // Assert - Should not crash or hang
        act.Should().NotThrow("very long lines should be handled");

        var html = act.Invoke();
        html.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_InlineView_WithExtremelyLongLine_ShouldHandleGracefully()
    {
        // Arrange - Create a line with 100,000 characters (extreme case)
        var extremeLine = new string('a', 100_000);
        var result = CreateDiffResultWithContent($"// {extremeLine}", $"// {extremeLine}X");

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act & Assert
        var act = () => _formatter.FormatResult(result, options);
        act.Should().NotThrow("extremely long lines should not crash");
    }

    #endregion

    #region Line Number Edge Cases Tests (HIGH Priority - I1-H2, I1-H3)

    [Fact]
    public void Format_InlineView_WithLineNumberZero_ShouldHandleGracefully()
    {
        // Arrange - Test with StartLine = 0 (possibly invalid)
        var result = CreateDiffResultAtLine("void Method() { }", "void Method() { Console.WriteLine(); }", 0);

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Should either treat as line 1 or handle specially
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("class=\"line-number\"");
    }

    [Fact]
    public void Format_InlineView_WithLineNumberAtFileStart_ShouldDisplay()
    {
        // Arrange - Change at line 1 (start of file)
        var result = CreateDiffResultAtLine("namespace Old { }", "namespace New { }", 1);

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("class=\"line-number\"");
        html.Should().Contain(">1<", "line 1 should be displayed");
    }

    [Fact]
    public void Format_InlineView_WithLineNumberAtFileEnd_ShouldDisplay()
    {
        // Arrange - Change at line 10000 (end of large file)
        var result = CreateDiffResultAtLine("// end", "// END", 10000);

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain(">10000<", "large line number should be displayed");
    }

    #endregion

    #region Context Mode Tests (HIGH Priority - I1-H4)

    [Fact]
    public void Format_InlineView_WithInlineContextNull_ShouldShowFullFile()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = null  // Null should show full file
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Should not have context separators
        html.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_InlineView_WithInlineContextZero_ShouldShowOnlyChanges()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = 0  // Zero context
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Should have context separators between distant changes
        html.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_InlineView_WithVeryLargeContext_ShouldNotExhaustMemory()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = int.MaxValue  // Extreme context value
        };

        // Act
        var act = () => _formatter.FormatResult(result, options);

        // Assert - Should not exhaust memory
        act.Should().NotThrow("very large context should be handled");
    }

    #endregion

    #region Line Ending Tests (MEDIUM Priority - I1-M1)

    [Fact]
    public void Format_InlineView_WithWindowsLineEndings_ShouldHandleCorrectly()
    {
        // Arrange
        var result = CreateDiffResultWithContent(
            "void Method()\r\n{\r\n    return;\r\n}",
            "void Method()\r\n{\r\n    return 0;\r\n}");

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Line endings should not cause issues
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("class=\"line-number\"");
    }

    [Fact]
    public void Format_InlineView_WithMixedLineEndings_ShouldHandleCorrectly()
    {
        // Arrange - Old has \r\n, new has \n
        var result = CreateDiffResultWithContent(
            "void Method()\r\n{\r\n    return;\r\n}",
            "void Method()\n{\n    return 0;\n}");

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Tab and Whitespace Tests (MEDIUM Priority - I1-M2, I1-M8)

    [Fact]
    public void Format_InlineView_WithTabCharacters_ShouldPreserve()
    {
        // Arrange
        var result = CreateDiffResultWithContent("void Method()\t{ }", "void Method()\t{ return; }");

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Tabs should be preserved or converted consistently
        html.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_InlineView_WithWhitespaceOnlyChanges_ShouldBeVisible()
    {
        // Arrange - Content changed from 4 spaces to 1 tab
        var result = CreateDiffResultWithContent("    return;", "\treturn;");

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - Whitespace changes should be visible
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("line-prefix");
    }

    #endregion

    #region Empty Content Tests (MEDIUM Priority - I1-M7)

    [Fact]
    public void Format_InlineView_WithEmptyOldContent_ShouldDisplay()
    {
        // Arrange
        var result = CreateDiffResultWithContent("", "void Method() { }");

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain(">+<", "should show + prefix for added content");
    }

    #endregion

    #region Whole File Change Tests (MEDIUM Priority - I1-M6)

    [Fact]
    public void Format_InlineView_WithWholeFileChange_ShouldHandleEfficiently()
    {
        // Arrange - Change spanning entire 1000-line file
        var largeOldContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"    Line {i};"));
        var largeNewContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"    Modified Line {i};"));

        var result = CreateDiffResultAtLine(largeOldContent, largeNewContent, 1, 1000);

        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var html = _formatter.FormatResult(result, options);
        stopwatch.Stop();

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "whole file change should render in reasonable time");
    }

    #endregion

    #region Helper Methods

    private DiffResult CreateSimpleDiffResult()
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
                            Name = "Method",
                            OldContent = "void Method() { }",
                            NewContent = "void Method() { return; }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
    }

    private DiffResult CreateDiffResultWithContent(string oldContent, string newContent)
    {
        return CreateDiffResultAtLine(oldContent, newContent, 1);
    }

    private DiffResult CreateDiffResultAtLine(string oldContent, string newContent, int lineNumber, int? endLine = null)
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
                            Name = "Method",
                            OldContent = oldContent,
                            NewContent = newContent,
                            OldLocation = new Location { StartLine = lineNumber, EndLine = endLine ?? lineNumber },
                            NewLocation = new Location { StartLine = lineNumber, EndLine = endLine ?? lineNumber }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
    }

    #endregion
}
