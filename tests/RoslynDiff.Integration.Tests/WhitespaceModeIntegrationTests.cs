namespace RoslynDiff.Integration.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Integration tests for whitespace mode handling in line-based diffing.
/// Tests the WhitespaceMode enum and its effects on diff comparison results.
/// </summary>
public class WhitespaceModeIntegrationTests
{
    private readonly DifferFactory _factory = new();
    private readonly OutputFormatterFactory _formatterFactory = new();

    #region Helper Methods

    /// <summary>
    /// Gets the path to a whitespace test data file.
    /// </summary>
    private static string GetTestDataPath(string relativePath)
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "TestData", "Whitespace", relativePath);
    }

    /// <summary>
    /// Creates diff options with the specified whitespace mode.
    /// </summary>
    private static DiffOptions CreateOptions(string? oldPath, string? newPath, WhitespaceMode mode) => new()
    {
        OldPath = oldPath,
        NewPath = newPath,
        WhitespaceMode = mode
    };

    #endregion

    #region WhitespaceMode.Exact Tests (Standard Diff Compatibility)

    [Fact]
    public void ExactMode_IdenticalContent_NoChanges()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare(content, content, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void ExactMode_WhitespaceDifference_DetectsChange()
    {
        // Arrange - only whitespace difference (2 spaces vs 4 spaces)
        var oldContent = "  indented";
        var newContent = "    indented";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Exact mode should detect the whitespace difference
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExactMode_TrailingWhitespaceDifference_DetectsChange()
    {
        // Arrange
        var oldContent = "line without trailing";
        var newContent = "line without trailing   ";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Exact mode should detect trailing whitespace difference
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExactMode_TabVsSpaces_DetectsChange()
    {
        // Arrange
        var oldContent = "\tindented with tab";
        var newContent = "    indented with tab";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Exact mode should detect tab vs spaces difference
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region WhitespaceMode.IgnoreLeadingTrailing Tests

    [Fact]
    public void IgnoreLeadingTrailingMode_LeadingWhitespaceDifference_NoChange()
    {
        // Arrange
        var oldContent = "  indented";
        var newContent = "    indented";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreLeadingTrailing);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should ignore leading whitespace differences
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void IgnoreLeadingTrailingMode_TrailingWhitespaceDifference_NoChange()
    {
        // Arrange
        var oldContent = "content";
        var newContent = "content   ";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreLeadingTrailing);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should ignore trailing whitespace differences
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void IgnoreLeadingTrailingMode_ContentChange_DetectsChange()
    {
        // Arrange
        var oldContent = "  hello  ";
        var newContent = "  world  ";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreLeadingTrailing);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should still detect actual content changes
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IgnoreLeadingTrailingMode_InternalWhitespace_DetectsChange()
    {
        // Arrange - internal whitespace should still be detected
        var oldContent = "hello world";
        var newContent = "hello    world";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreLeadingTrailing);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Internal whitespace differences should be detected
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region WhitespaceMode.IgnoreAll Tests

    [Fact]
    public void IgnoreAllMode_AllWhitespaceDifferences_NoChange()
    {
        // Arrange - multiple whitespace differences
        var oldContent = "  hello   world  ";
        var newContent = "hello world";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreAll);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should ignore all whitespace differences
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void IgnoreAllMode_TabsVsSpaces_NoChange()
    {
        // Arrange
        var oldContent = "\t\thello\t\tworld";
        var newContent = "    hello    world";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreAll);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should treat tabs and spaces equivalently
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void IgnoreAllMode_ContentChange_DetectsChange()
    {
        // Arrange
        var oldContent = "  hello   world  ";
        var newContent = "hello universe";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreAll);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should still detect actual content changes
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region WhitespaceMode.LanguageAware Tests - Python Files

    [Fact]
    public async Task LanguageAwareMode_PythonIndentChange_DetectsChanges()
    {
        // Arrange
        var oldPath = GetTestDataPath("python_indent_change.old.py");
        var newPath = GetTestDataPath("python_indent_change.new.py");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = new LineDiffer();
        var options = CreateOptions(oldPath, newPath, WhitespaceMode.LanguageAware);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should detect the indentation changes as Python is whitespace-significant
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LanguageAwareMode_PythonFile_FlagsIndentationIssue()
    {
        // Arrange
        var oldContent = "def foo():\n    print('hello')";
        var newContent = "def foo():\n        print('hello')";  // Extra indent
        var differ = new LineDiffer();
        var options = CreateOptions("test.py", "test.py", WhitespaceMode.LanguageAware);
        options = options with { NewPath = "test.py" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - should flag indentation change for Python
        var changes = result.FileChanges.SelectMany(fc => fc.Changes)
            .Where(c => c.Type != ChangeType.Unchanged)
            .ToList();

        changes.Should().NotBeEmpty("Python indentation changes should be detected");

        // At least one change should have whitespace issues flagged
        var changesWithWhitespaceIssues = changes
            .Where(c => c.WhitespaceIssues != WhitespaceIssue.None)
            .ToList();

        changesWithWhitespaceIssues.Should().NotBeEmpty(
            "Python indentation changes should flag WhitespaceIssue.IndentationChanged");

        changesWithWhitespaceIssues.Any(c => c.WhitespaceIssues.HasFlag(WhitespaceIssue.IndentationChanged))
            .Should().BeTrue("Should have IndentationChanged flag set");
    }

    [Fact]
    public void LanguageAwareMode_YamlFile_FlagsIndentationIssue()
    {
        // Arrange
        var oldContent = "root:\n  child: value";
        var newContent = "root:\n    child: value";  // Extra indent
        var differ = new LineDiffer();
        var options = CreateOptions("config.yaml", "config.yaml", WhitespaceMode.LanguageAware);
        options = options with { NewPath = "config.yaml" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - should flag indentation change for YAML
        var changes = result.FileChanges.SelectMany(fc => fc.Changes)
            .Where(c => c.Type != ChangeType.Unchanged)
            .ToList();

        changes.Should().NotBeEmpty("YAML indentation changes should be detected");

        var changesWithIndentationIssues = changes
            .Where(c => c.WhitespaceIssues.HasFlag(WhitespaceIssue.IndentationChanged))
            .ToList();

        changesWithIndentationIssues.Should().NotBeEmpty(
            "YAML indentation changes should flag WhitespaceIssue.IndentationChanged");
    }

    #endregion

    #region WhitespaceMode.LanguageAware Tests - C# Files (Brace Languages)

    [Fact]
    public async Task LanguageAwareMode_CSharpFormatChange_AllowsNormalization()
    {
        // Arrange
        var oldPath = GetTestDataPath("csharp_format_change.old.cs");
        var newPath = GetTestDataPath("csharp_format_change.new.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = new LineDiffer();
        var options = CreateOptions(oldPath, newPath, WhitespaceMode.LanguageAware);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - C# is whitespace-insignificant, so formatting changes are detected but not flagged as issues
        result.Stats.TotalChanges.Should().BeGreaterThan(0, "Content changes should be detected");
    }

    [Fact]
    public void LanguageAwareMode_CSharpFile_NoIndentationWarnings()
    {
        // Arrange - C# with indentation change (cosmetic, not breaking)
        var oldContent = "namespace Test\n{\n    class Foo { }\n}";
        var newContent = "namespace Test\n{\n        class Foo { }\n}";  // Different indent
        var differ = new LineDiffer();
        var options = CreateOptions("test.cs", "test.cs", WhitespaceMode.LanguageAware);
        options = options with { NewPath = "test.cs" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - C# indentation changes should NOT flag IndentationChanged warnings
        // because C# is whitespace-insignificant
        var changesWithIndentationIssues = result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.WhitespaceIssues.HasFlag(WhitespaceIssue.IndentationChanged))
            .ToList();

        changesWithIndentationIssues.Should().BeEmpty(
            "C# is whitespace-insignificant, so indentation changes should not flag warnings");
    }

    [Fact]
    public void LanguageAwareMode_JavaFile_NoIndentationWarnings()
    {
        // Arrange - Java with indentation change (cosmetic, not breaking)
        var oldContent = "public class Foo {\n    void bar() { }\n}";
        var newContent = "public class Foo {\n        void bar() { }\n}";  // Different indent
        var differ = new LineDiffer();
        var options = CreateOptions("Foo.java", "Foo.java", WhitespaceMode.LanguageAware);
        options = options with { NewPath = "Foo.java" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Java indentation changes should NOT flag IndentationChanged warnings
        var changesWithIndentationIssues = result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.WhitespaceIssues.HasFlag(WhitespaceIssue.IndentationChanged))
            .ToList();

        changesWithIndentationIssues.Should().BeEmpty(
            "Java is whitespace-insignificant, so indentation changes should not flag warnings");
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void IgnoreWhitespace_True_EquivalentToIgnoreLeadingTrailing()
    {
        // Arrange
        var oldContent = "  hello  ";
        var newContent = "hello";
        var differ = new LineDiffer();

        var optionsWithIgnoreWhitespace = new DiffOptions
        {
            OldPath = "old.txt",
            NewPath = "new.txt",
            IgnoreWhitespace = true
        };

        var optionsWithMode = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreLeadingTrailing);

        // Act
        var resultWithFlag = differ.Compare(oldContent, newContent, optionsWithIgnoreWhitespace);
        var resultWithMode = differ.Compare(oldContent, newContent, optionsWithMode);

        // Assert - Both should produce equivalent results
        resultWithFlag.Stats.TotalChanges.Should().Be(resultWithMode.Stats.TotalChanges);
    }

    [Fact]
    public void DefaultWhitespaceMode_IsExact()
    {
        // Arrange
        var options = new DiffOptions
        {
            OldPath = "old.txt",
            NewPath = "new.txt"
        };

        // Assert
        options.WhitespaceMode.Should().Be(WhitespaceMode.Exact,
            "Default whitespace mode should be Exact for standard diff compatibility");
    }

    #endregion

    #region Mixed Content Tests

    [Fact]
    public void ExactMode_MixedTabsSpacesContent_PreservesAll()
    {
        // Arrange
        var content = "Line with spaces at start\n\tLine with tab at start\n    Line with 4 spaces";
        var differ = new LineDiffer();
        var options = CreateOptions("mixed.txt", "mixed.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare(content, content, options);

        // Assert - Identical content should produce no changes
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void LanguageAwareMode_MakefileTabRequired_DetectsTabIssue()
    {
        // Arrange - Makefile requires tabs for recipe lines
        var oldContent = "target:\n\tcommand";  // Correct: tab indented
        var newContent = "target:\n    command";  // Wrong: space indented
        var differ = new LineDiffer();
        var options = CreateOptions("Makefile", "Makefile", WhitespaceMode.LanguageAware);
        options = options with { NewPath = "Makefile" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should detect and flag the change (Makefile is whitespace-significant)
        result.Stats.TotalChanges.Should().BeGreaterThan(0);

        // Makefile should have indentation issues flagged
        var changesWithWhitespaceIssues = result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.WhitespaceIssues != WhitespaceIssue.None)
            .ToList();

        changesWithWhitespaceIssues.Should().NotBeEmpty(
            "Makefile tab-to-space change should flag whitespace issues");
    }

    #endregion

    #region Multi-line Content Tests

    [Fact]
    public void ExactMode_MultipleLineChanges_DetectsAll()
    {
        // Arrange
        var oldContent = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        var newContent = "Line 1\n  Line 2\nLine 3\nLine 4  \nLine 5";  // Whitespace changes on 2 and 4
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should detect changes on both lines
        var actualChanges = result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.Type != ChangeType.Unchanged)
            .ToList();

        actualChanges.Should().HaveCountGreaterThanOrEqualTo(2,
            "Should detect whitespace changes on at least 2 lines");
    }

    [Fact]
    public void IgnoreAllMode_MultipleWhitespaceChanges_IgnoresAll()
    {
        // Arrange
        var oldContent = "Line 1\nLine 2\nLine 3";
        var newContent = "  Line 1  \n\t\tLine 2\t\t\n    Line 3    ";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreAll);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should ignore all whitespace-only changes
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region Output Format Tests

    [Fact]
    public void WhitespaceIssues_IncludedInJsonOutput()
    {
        // Arrange
        var oldContent = "def foo():\n    print('hello')";
        var newContent = "def foo():\n        print('hello')";
        var differ = new LineDiffer();
        var options = CreateOptions("test.py", "test.py", WhitespaceMode.LanguageAware);
        options = options with { NewPath = "test.py" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");
        var json = formatter.FormatResult(result, new OutputOptions { IncludeContent = true, PrettyPrint = true });

        // Assert
        json.Should().NotBeNullOrWhiteSpace();

        // Parse and verify the JSON contains whitespace issue information
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue("JSON output should have 'files' property");

        // Verify that whitespaceIssues is included in the changes
        var filesArray = files.EnumerateArray().ToList();
        filesArray.Should().NotBeEmpty("Should have at least one file in output");

        var changesArray = filesArray[0].GetProperty("changes").EnumerateArray().ToList();
        var changesWithWhitespaceIssues = changesArray
            .Where(c => c.TryGetProperty("whitespaceIssues", out _))
            .ToList();

        changesWithWhitespaceIssues.Should().NotBeEmpty("Python file changes should include whitespaceIssues in JSON output");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ExactMode_EmptyContent_NoChanges()
    {
        // Arrange
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare("", "", options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void ExactMode_OnlyWhitespaceContent_HandledCorrectly()
    {
        // Arrange
        var oldContent = "   \n\t\t\n    ";
        var newContent = "\t\n  \n\t\t";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.Exact);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should detect differences in whitespace-only content
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IgnoreAllMode_OnlyWhitespaceContent_NoChanges()
    {
        // Arrange
        var oldContent = "   \n\t\t\n    ";
        var newContent = "\t\n  \n\t\t";
        var differ = new LineDiffer();
        var options = CreateOptions("old.txt", "new.txt", WhitespaceMode.IgnoreAll);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should ignore differences in whitespace-only content
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void LanguageAwareMode_UnknownExtension_UsesExactComparison()
    {
        // Arrange - Unknown file extension should use exact comparison for safety
        var oldContent = "  content";
        var newContent = "content";
        var differ = new LineDiffer();
        var options = CreateOptions("file.unknown", "file.unknown", WhitespaceMode.LanguageAware);
        options = options with { NewPath = "file.unknown" };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should detect the whitespace difference (exact comparison for unknown)
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LanguageAwareMode_NullPath_UsesExactComparison()
    {
        // Arrange
        var oldContent = "  content";
        var newContent = "content";
        var differ = new LineDiffer();
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.LanguageAware
        };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Should detect the whitespace difference (exact comparison when no path)
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion
}
