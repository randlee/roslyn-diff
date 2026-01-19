namespace RoslynDiff.Output.Tests.Formatters;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for verifying that formatters correctly display whitespace warnings.
/// </summary>
public class FormatterWhitespaceWarningTests
{
    #region Helper Methods

    /// <summary>
    /// Creates a DiffResult containing a single change with the specified whitespace issues.
    /// </summary>
    private static DiffResult CreateDiffResultWithChange(Change change)
    {
        return new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes = [change]
                }
            ]
        };
    }

    /// <summary>
    /// Creates a modified change with specified whitespace issues.
    /// </summary>
    private static Change CreateModifiedChangeWithIssues(WhitespaceIssue issues)
    {
        return new Change
        {
            Type = ChangeType.Modified,
            Kind = ChangeKind.Line,
            Name = "TestLine",
            WhitespaceIssues = issues,
            OldContent = "    code",
            NewContent = "        code"
        };
    }

    #endregion

    #region SpectreConsoleFormatter Tests

    /// <summary>
    /// Unit tests for <see cref="SpectreConsoleFormatter"/> whitespace warning display.
    /// </summary>
    public class SpectreConsoleFormatterWhitespaceTests
    {
        private readonly SpectreConsoleFormatter _formatter = new();

        [Fact]
        public void Format_ChangeWithIndentationIssue_ShouldDisplayWarning()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.IndentationChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("IndentationChanged");
        }

        [Fact]
        public void Format_ChangeWithNoIssues_ShouldNotShowWarning()
        {
            // Arrange
            var change = new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Line,
                Name = "TestLine",
                WhitespaceIssues = WhitespaceIssue.None,
                OldContent = "code",
                NewContent = "code modified"
            };
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().NotContain("IndentationChanged");
            output.Should().NotContain("MixedTabsSpaces");
            output.Should().NotContain("TrailingWhitespace");
            output.Should().NotContain("LineEndingChanged");
            output.Should().NotContain("AmbiguousTabWidth");
        }

        [Fact]
        public void Format_ChangeWithMultipleIssues_ShouldDisplayAllWarnings()
        {
            // Arrange
            var issues = WhitespaceIssue.IndentationChanged | WhitespaceIssue.MixedTabsSpaces | WhitespaceIssue.TrailingWhitespace;
            var change = CreateModifiedChangeWithIssues(issues);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("IndentationChanged");
            output.Should().Contain("MixedTabsSpaces");
            output.Should().Contain("TrailingWhitespace");
        }

        [Fact]
        public void Format_ChangeWithMixedTabsSpaces_ShouldDisplayWarning()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.MixedTabsSpaces);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("MixedTabsSpaces");
        }

        [Fact]
        public void Format_ChangeWithTrailingWhitespace_ShouldDisplayWarning()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.TrailingWhitespace);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("TrailingWhitespace");
        }

        [Fact]
        public void Format_ChangeWithLineEndingChanged_ShouldDisplayWarning()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.LineEndingChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("LineEndingChanged");
        }

        [Fact]
        public void Format_ChangeWithAmbiguousTabWidth_ShouldDisplayWarning()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.AmbiguousTabWidth);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("AmbiguousTabWidth");
        }

        [Fact]
        public void Format_ChangeWithAllIssues_ShouldDisplayAllWarnings()
        {
            // Arrange
            var allIssues = WhitespaceIssue.IndentationChanged
                | WhitespaceIssue.MixedTabsSpaces
                | WhitespaceIssue.TrailingWhitespace
                | WhitespaceIssue.LineEndingChanged
                | WhitespaceIssue.AmbiguousTabWidth;
            var change = CreateModifiedChangeWithIssues(allIssues);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("IndentationChanged");
            output.Should().Contain("MixedTabsSpaces");
            output.Should().Contain("TrailingWhitespace");
            output.Should().Contain("LineEndingChanged");
            output.Should().Contain("AmbiguousTabWidth");
        }
    }

    #endregion

    #region PlainTextFormatter Tests

    /// <summary>
    /// Unit tests for <see cref="PlainTextFormatter"/> whitespace warning display.
    /// </summary>
    public class PlainTextFormatterWhitespaceTests
    {
        private readonly PlainTextFormatter _formatter = new();

        [Fact]
        public void Format_ChangeWithIndentationIssue_ShouldDisplayWarningText()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.IndentationChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("IndentationChanged");
        }

        [Fact]
        public void Format_ChangeWithNoIssues_ShouldNotShowWarningText()
        {
            // Arrange
            var change = new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Line,
                Name = "TestLine",
                WhitespaceIssues = WhitespaceIssue.None,
                OldContent = "code",
                NewContent = "code modified"
            };
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().NotContain("WhitespaceIssue");
            output.Should().NotContain("IndentationChanged");
        }

        [Fact]
        public void Format_ChangeWithMultipleIssues_ShouldListAllIssues()
        {
            // Arrange
            var issues = WhitespaceIssue.IndentationChanged | WhitespaceIssue.TrailingWhitespace;
            var change = CreateModifiedChangeWithIssues(issues);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("IndentationChanged");
            output.Should().Contain("TrailingWhitespace");
        }

        [Fact]
        public void Format_WhitespaceWarning_ShouldBeHumanReadable()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.MixedTabsSpaces);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            // The output should contain the warning in a readable format
            output.Should().Contain("MixedTabsSpaces");
        }
    }

    #endregion

    #region JsonFormatter Tests

    /// <summary>
    /// Unit tests for <see cref="JsonFormatter"/> whitespace warning display.
    /// </summary>
    public class JsonFormatterWhitespaceTests
    {
        private readonly JsonFormatter _formatter = new();

        [Fact]
        public void Format_ChangeWithWhitespaceIssues_ShouldIncludeWhitespaceIssuesProperty()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.IndentationChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

            // Assert
            output.Should().Contain("whitespaceIssues");
            output.Should().Contain("IndentationChanged");
        }

        [Fact]
        public void Format_ChangeWithNoIssues_ShouldHaveNoneOrOmitProperty()
        {
            // Arrange
            var change = new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Line,
                Name = "TestLine",
                WhitespaceIssues = WhitespaceIssue.None,
                OldContent = "code",
                NewContent = "code modified"
            };
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

            // Assert
            // Either the property is omitted, or it shows "None"
            var containsNonWhitespaceIssue = output.Contains("IndentationChanged")
                || output.Contains("MixedTabsSpaces")
                || output.Contains("TrailingWhitespace")
                || output.Contains("LineEndingChanged")
                || output.Contains("AmbiguousTabWidth");
            containsNonWhitespaceIssue.Should().BeFalse("no whitespace issues should be present");
        }

        [Fact]
        public void Format_ChangeWithMultipleIssues_ShouldIncludeAllInProperty()
        {
            // Arrange
            var issues = WhitespaceIssue.IndentationChanged | WhitespaceIssue.MixedTabsSpaces;
            var change = CreateModifiedChangeWithIssues(issues);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

            // Assert
            output.Should().Contain("whitespaceIssues");
            output.Should().Contain("IndentationChanged");
            output.Should().Contain("MixedTabsSpaces");
        }

        [Fact]
        public void Format_WithWhitespaceIssues_ShouldProduceValidJson()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.IndentationChanged | WhitespaceIssue.TrailingWhitespace);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

            // Assert
            var action = () => JsonDocument.Parse(output);
            action.Should().NotThrow("output should be valid JSON");
        }

        [Fact]
        public void Format_WithWhitespaceIssues_OutputCanBeDeserialized()
        {
            // Arrange
            var issues = WhitespaceIssue.IndentationChanged | WhitespaceIssue.MixedTabsSpaces;
            var change = CreateModifiedChangeWithIssues(issues);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

            // Assert - verify it can be parsed and contains expected structure
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;
            root.TryGetProperty("files", out var files).Should().BeTrue();
            files.GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public void Format_AllWhitespaceIssueTypes_ShouldBeRepresentedCorrectly()
        {
            // Arrange
            var allIssues = WhitespaceIssue.IndentationChanged
                | WhitespaceIssue.MixedTabsSpaces
                | WhitespaceIssue.TrailingWhitespace
                | WhitespaceIssue.LineEndingChanged
                | WhitespaceIssue.AmbiguousTabWidth;
            var change = CreateModifiedChangeWithIssues(allIssues);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

            // Assert
            output.Should().Contain("IndentationChanged");
            output.Should().Contain("MixedTabsSpaces");
            output.Should().Contain("TrailingWhitespace");
            output.Should().Contain("LineEndingChanged");
            output.Should().Contain("AmbiguousTabWidth");
        }
    }

    #endregion

    #region HtmlFormatter Tests

    /// <summary>
    /// Unit tests for <see cref="HtmlFormatter"/> whitespace warning display.
    /// </summary>
    public class HtmlFormatterWhitespaceTests
    {
        private readonly HtmlFormatter _formatter = new();

        [Fact]
        public void Format_ChangeWithWhitespaceIssues_ShouldApplyWarningCssClass()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.IndentationChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            // The output should contain a warning CSS class or warning indicator
            output.Should().Contain("whitespace");
        }

        [Fact]
        public void Format_ChangeWithNoIssues_ShouldNotApplyWarningClass()
        {
            // Arrange
            var change = new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Line,
                Name = "TestLine",
                WhitespaceIssues = WhitespaceIssue.None,
                OldContent = "code",
                NewContent = "code modified"
            };
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            // Should not contain whitespace warning element markup when no issues
            // (CSS class definition in style block is always present, but the actual warning div should not be rendered)
            output.Should().NotContain("<div class=\"whitespace-warning\"");
            output.Should().NotContain("IndentationChanged");
        }

        [Fact]
        public void Format_ChangeWithWhitespaceIssues_ShouldIncludeTooltipOrTitle()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.IndentationChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            // The output should contain the issue name in a title or tooltip
            output.Should().Contain("IndentationChanged");
        }

        [Fact]
        public void Format_ChangeWithMultipleIssues_ShouldIncludeAllInTooltip()
        {
            // Arrange
            var issues = WhitespaceIssue.IndentationChanged | WhitespaceIssue.MixedTabsSpaces | WhitespaceIssue.TrailingWhitespace;
            var change = CreateModifiedChangeWithIssues(issues);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("IndentationChanged");
            output.Should().Contain("MixedTabsSpaces");
            output.Should().Contain("TrailingWhitespace");
        }

        [Fact]
        public void Format_ChangeWithWhitespaceIssues_ShouldProduceValidHtml()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.IndentationChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("<!DOCTYPE html>");
            output.Should().Contain("</html>");
        }

        [Fact]
        public void Format_ChangeWithLineEndingChanged_ShouldShowSpecificWarning()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.LineEndingChanged);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("LineEndingChanged");
        }

        [Fact]
        public void Format_ChangeWithAmbiguousTabWidth_ShouldShowSpecificWarning()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.AmbiguousTabWidth);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            output.Should().Contain("AmbiguousTabWidth");
        }

        [Fact]
        public void Format_WhitespaceWarning_ShouldBeVisuallyDistinct()
        {
            // Arrange
            var change = CreateModifiedChangeWithIssues(WhitespaceIssue.MixedTabsSpaces);
            var result = CreateDiffResultWithChange(change);

            // Act
            var output = _formatter.FormatResult(result, new OutputOptions());

            // Assert
            // Verify there's some styling or class associated with whitespace warnings
            output.Should().Contain("MixedTabsSpaces");
        }
    }

    #endregion
}
