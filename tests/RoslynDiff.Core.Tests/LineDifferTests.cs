namespace RoslynDiff.Core.Tests;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="LineDiffer"/>.
/// </summary>
public class LineDifferTests
{
    private readonly LineDiffer _differ = new();

    #region CanHandle Tests

    [Fact]
    public void CanHandle_ExplicitLineMode_ReturnsTrue()
    {
        // Arrange
        var options = new DiffOptions { Mode = DiffMode.Line };

        // Act
        var result = _differ.CanHandle("any.cs", options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ExplicitRoslynMode_ReturnsFalse()
    {
        // Arrange
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = _differ.CanHandle("any.txt", options);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(".txt", true)]
    [InlineData(".md", true)]
    [InlineData(".json", true)]
    [InlineData(".xml", true)]
    [InlineData(".cs", false)]
    [InlineData(".vb", false)]
    public void CanHandle_AutoMode_ReturnsExpectedValue(string extension, bool expected)
    {
        // Arrange
        var options = new DiffOptions { Mode = null };

        // Act
        var result = _differ.CanHandle($"file{extension}", options);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Compare Tests - Empty Files

    [Fact]
    public void Compare_BothEmpty_ReturnsEmptyResult()
    {
        // Arrange
        var oldContent = "";
        var newContent = "";
        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Should().NotBeNull();
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_OldEmpty_NewHasContent_ReturnsAdditions()
    {
        // Arrange
        var oldContent = "";
        var newContent = "line 1\nline 2\nline 3";
        var options = new DiffOptions { ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Additions.Should().Be(3);
        result.Stats.Deletions.Should().Be(0);
        result.Stats.TotalChanges.Should().Be(3);
    }

    [Fact]
    public void Compare_NewEmpty_OldHasContent_ReturnsDeletions()
    {
        // Arrange
        var oldContent = "line 1\nline 2\nline 3";
        var newContent = "";
        var options = new DiffOptions { ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Deletions.Should().Be(3);
        result.Stats.Additions.Should().Be(0);
        result.Stats.TotalChanges.Should().Be(3);
    }

    #endregion

    #region Compare Tests - Identical Files

    [Fact]
    public void Compare_IdenticalContent_ReturnsNoChanges()
    {
        // Arrange
        var content = "line 1\nline 2\nline 3";
        var options = new DiffOptions { ContextLines = 0 };

        // Act
        var result = _differ.Compare(content, content, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
        result.Stats.Additions.Should().Be(0);
        result.Stats.Deletions.Should().Be(0);
        result.Stats.Modifications.Should().Be(0);
    }

    #endregion

    #region Compare Tests - Single Line Changes

    [Fact]
    public void Compare_SingleLineAdded_ReturnsOneAddition()
    {
        // Arrange
        var oldContent = "line 1\nline 3";
        var newContent = "line 1\nline 2\nline 3";
        var options = new DiffOptions { ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Compare_SingleLineRemoved_ReturnsOneDeletion()
    {
        // Arrange
        var oldContent = "line 1\nline 2\nline 3";
        var newContent = "line 1\nline 3";
        var options = new DiffOptions { ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Deletions.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region Compare Tests - Context Lines

    [Fact]
    public void Compare_WithContextLines_IncludesUnchangedContext()
    {
        // Arrange
        var oldContent = "context 1\ncontext 2\nchanged line\ncontext 3\ncontext 4";
        var newContent = "context 1\ncontext 2\nnew line\ncontext 3\ncontext 4";
        var options = new DiffOptions { ContextLines = 2 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
        var changes = result.FileChanges[0].Changes;
        changes.Should().Contain(c => c.Type == ChangeType.Unchanged);
    }

    [Fact]
    public void Compare_WithZeroContextLines_NoUnchangedLines()
    {
        // Arrange
        var oldContent = "context 1\ncontext 2\nchanged line\ncontext 3\ncontext 4";
        var newContent = "context 1\ncontext 2\nnew line\ncontext 3\ncontext 4";
        var options = new DiffOptions { ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.FileChanges.Should().HaveCount(1);
        var changes = result.FileChanges[0].Changes;
        changes.Should().NotContain(c => c.Type == ChangeType.Unchanged);
    }

    #endregion

    #region Compare Tests - Whitespace

    [Fact]
    public void Compare_WithIgnoreWhitespace_IgnoresLeadingTrailingWhitespaceChanges()
    {
        // Arrange - DiffPlex's IgnoreWhitespace trims lines for comparison
        var oldContent = "  line with spaces  ";
        var newContent = "line with spaces";
        var options = new DiffOptions { IgnoreWhitespace = true, ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_WithoutIgnoreWhitespace_DetectsWhitespaceChanges()
    {
        // Arrange
        var oldContent = "line with   spaces";
        var newContent = "line with spaces";
        var options = new DiffOptions { IgnoreWhitespace = false, ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Compare Tests - Path Handling

    [Fact]
    public void Compare_WithPaths_SetsPathsInResult()
    {
        // Arrange
        var options = new DiffOptions
        {
            OldPath = "old/file.txt",
            NewPath = "new/file.txt"
        };

        // Act
        var result = _differ.Compare("content", "content", options);

        // Assert
        result.OldPath.Should().Be("old/file.txt");
        result.NewPath.Should().Be("new/file.txt");
    }

    #endregion

    #region Compare Tests - Null Arguments

    [Fact]
    public void Compare_NullOldContent_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var act = () => _differ.Compare(null!, "content", options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("oldContent");
    }

    [Fact]
    public void Compare_NullNewContent_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var act = () => _differ.Compare("content", null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newContent");
    }

    [Fact]
    public void Compare_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _differ.Compare("old", "new", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion

    #region Compare Tests - Large Files

    [Fact]
    public void Compare_LargeFile_CompletesSuccessfully()
    {
        // Arrange
        var lines = Enumerable.Range(1, 1000).Select(i => $"Line number {i}");
        var oldContent = string.Join("\n", lines);
        var newLines = lines.ToList();
        newLines[500] = "Modified line 501";
        var newContent = string.Join("\n", newLines);
        var options = new DiffOptions { ContextLines = 3 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Should().NotBeNull();
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region DifferFactory Integration

    [Fact]
    public void DifferFactory_ForTextFile_ReturnsLineDiffer()
    {
        // Arrange
        var factory = new DifferFactory();
        var options = new DiffOptions();

        // Act
        var differ = factory.GetDiffer("file.txt", options);

        // Assert
        differ.Should().BeOfType<LineDiffer>();
    }

    [Fact]
    public void DifferFactory_ExplicitLineMode_ReturnsLineDifferForCsFile()
    {
        // Arrange
        var factory = new DifferFactory();
        var options = new DiffOptions { Mode = DiffMode.Line };

        // Act
        var differ = factory.GetDiffer("file.cs", options);

        // Assert
        differ.Should().BeOfType<LineDiffer>();
    }

    #endregion
}
