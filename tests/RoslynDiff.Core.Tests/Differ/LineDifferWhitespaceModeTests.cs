namespace RoslynDiff.Core.Tests.Differ;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for LineDiffer's WhitespaceMode functionality.
/// Tests the various whitespace handling modes and their interaction with the legacy IgnoreWhitespace option.
/// </summary>
public class LineDifferWhitespaceModeTests
{
    private readonly LineDiffer _differ = new();

    #region ResolveWhitespaceMode Tests (via Compare behavior)

    [Fact]
    public void Compare_DefaultOptions_UsesExactWhitespaceMode()
    {
        // Arrange - Default DiffOptions should use WhitespaceMode.Exact
        var oldContent = "  line with spaces  ";
        var newContent = "line with spaces";
        var options = new DiffOptions { ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Exact mode should detect the whitespace difference
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_IgnoreWhitespaceTrue_MapsToIgnoreLeadingTrailingBehavior()
    {
        // Arrange - IgnoreWhitespace=true should behave like IgnoreLeadingTrailing
        var oldContent = "  line with spaces  ";
        var newContent = "line with spaces";
        var options = new DiffOptions { IgnoreWhitespace = true, ContextLines = 0 };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Leading/trailing whitespace should be ignored
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_ExplicitWhitespaceMode_TakesPrecedenceOverIgnoreWhitespace()
    {
        // Arrange - Explicit WhitespaceMode should override IgnoreWhitespace
        // Set IgnoreWhitespace=true but WhitespaceMode=IgnoreAll
        var oldContent = "line with   spaces";
        var newContent = "line with spaces";
        var options = new DiffOptions
        {
            IgnoreWhitespace = true,
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - IgnoreAll collapses whitespace, so no change should be detected
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_WhitespaceModeExact_IgnoreWhitespaceIsFalse_UsesExact()
    {
        // Arrange - Both settings agree on exact comparison
        var oldContent = "  line  ";
        var newContent = "line";
        var options = new DiffOptions
        {
            IgnoreWhitespace = false,
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Exact comparison detects the difference
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region WhitespaceMode.Exact Tests

    [Fact]
    public void Compare_Exact_DetectsLeadingWhitespaceDifferences()
    {
        // Arrange
        var oldContent = "no leading space";
        var newContent = "  no leading space";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_Exact_DetectsTrailingWhitespaceDifferences()
    {
        // Arrange
        var oldContent = "no trailing space";
        var newContent = "no trailing space  ";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_Exact_DetectsTabVsSpaceDifferences()
    {
        // Arrange
        var oldContent = "    indented with spaces";
        var newContent = "\tindented with spaces";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_Exact_DetectsMiddleWhitespaceDifferences()
    {
        // Arrange
        var oldContent = "word  word";
        var newContent = "word word";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_Exact_IdenticalContent_NoChanges()
    {
        // Arrange
        var content = "  line with whitespace  ";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(content, content, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region WhitespaceMode.IgnoreLeadingTrailing Tests

    [Fact]
    public void Compare_IgnoreLeadingTrailing_IgnoresLeadingWhitespace()
    {
        // Arrange
        var oldContent = "no leading space";
        var newContent = "  no leading space";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreLeadingTrailing,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreLeadingTrailing_IgnoresTrailingWhitespace()
    {
        // Arrange
        var oldContent = "no trailing space";
        var newContent = "no trailing space   ";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreLeadingTrailing,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreLeadingTrailing_IgnoresBothLeadingAndTrailing()
    {
        // Arrange
        var oldContent = "text";
        var newContent = "   text   ";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreLeadingTrailing,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreLeadingTrailing_StillDetectsMiddleWhitespaceChanges()
    {
        // Arrange - Note: DiffPlex's IgnoreWhitespace trims leading/trailing but still detects middle changes
        var oldContent = "word  word";
        var newContent = "word word";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreLeadingTrailing,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Middle whitespace changes should still be detected
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_IgnoreLeadingTrailing_StillDetectsContentChanges()
    {
        // Arrange
        var oldContent = "  old content  ";
        var newContent = "  new content  ";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreLeadingTrailing,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_IgnoreLeadingTrailing_MultipleLines()
    {
        // Arrange
        var oldContent = "line1\n  line2\nline3  ";
        var newContent = "  line1\nline2  \n  line3";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreLeadingTrailing,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - All lines have same content when trimmed
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region WhitespaceMode.IgnoreAll Tests

    [Fact]
    public void Compare_IgnoreAll_IgnoresLeadingWhitespace()
    {
        // Arrange
        var oldContent = "text";
        var newContent = "   text";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreAll_IgnoresTrailingWhitespace()
    {
        // Arrange
        var oldContent = "text";
        var newContent = "text   ";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreAll_IgnoresMiddleWhitespaceChanges()
    {
        // Arrange
        var oldContent = "word  word";
        var newContent = "word word";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreAll_CollapsesMultipleSpacesToSingle()
    {
        // Arrange
        var oldContent = "a    b     c";
        var newContent = "a b c";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreAll_CollapsesTabsAndSpaces()
    {
        // Arrange
        var oldContent = "word\t\tword";
        var newContent = "word word";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreAll_StillDetectsActualContentChanges()
    {
        // Arrange
        var oldContent = "old   content   here";
        var newContent = "new   content   here";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_IgnoreAll_IgnoresAllWhitespaceVariations()
    {
        // Arrange - Extreme whitespace variation
        var oldContent = "   a   b   c   ";
        var newContent = "a b c";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_IgnoreAll_MultipleLines()
    {
        // Arrange
        var oldContent = "  line1  \n  line2   line2b  \n  line3  ";
        var newContent = "line1\nline2 line2b\nline3";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region WhitespaceMode.LanguageAware Tests

    [Theory]
    [InlineData("test.py")]
    [InlineData("script.pyw")]
    [InlineData("config.yaml")]
    [InlineData("config.yml")]
    [InlineData("Makefile")]
    public void Compare_LanguageAware_PythonFile_UsesExactComparison(string filePath)
    {
        // Arrange - Python and whitespace-significant languages should use exact comparison
        var oldContent = "def foo():\n    pass";
        var newContent = "def foo():\n  pass";  // Changed indentation
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.LanguageAware,
            NewPath = filePath,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Indentation change should be detected (semantically significant)
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("test.cs")]
    [InlineData("test.java")]
    [InlineData("test.js")]
    [InlineData("test.ts")]
    [InlineData("test.go")]
    public void Compare_LanguageAware_CSharpFile_UsesIgnoreLeadingTrailing(string filePath)
    {
        // Arrange - C# and brace languages should ignore leading/trailing whitespace
        var oldContent = "  public void Method() { }  ";
        var newContent = "public void Method() { }";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.LanguageAware,
            NewPath = filePath,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Leading/trailing whitespace should be ignored
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Theory]
    [InlineData("file.unknown")]
    [InlineData("README")]
    [InlineData("LICENSE")]
    [InlineData(".gitignore")]
    public void Compare_LanguageAware_UnknownExtension_UsesExactComparisonForSafety(string filePath)
    {
        // Arrange - Unknown file types should use exact comparison for safety
        var oldContent = "  content  ";
        var newContent = "content";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.LanguageAware,
            NewPath = filePath,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Unknown extensions use exact comparison, so difference should be detected
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_LanguageAware_NullPath_UsesExactComparisonForSafety()
    {
        // Arrange - No file path provided should default to exact comparison
        var oldContent = "  content  ";
        var newContent = "content";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.LanguageAware,
            NewPath = null,
            OldPath = null,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Should use exact comparison
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_LanguageAware_CSharpFile_StillDetectsMiddleWhitespaceChanges()
    {
        // Arrange - Even for insignificant languages, middle whitespace may be significant
        var oldContent = "var x =  5;";
        var newContent = "var x = 5;";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.LanguageAware,
            NewPath = "test.cs",
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Middle whitespace changes should still be detected
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_LanguageAware_UsesOldPathIfNewPathNotSet()
    {
        // Arrange
        var oldContent = "  content  ";
        var newContent = "content";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.LanguageAware,
            OldPath = "test.cs",
            NewPath = null,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Should use C# classification (ignore leading/trailing)
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Compare_EmptyLines_IgnoreAll_HandlesProperly()
    {
        // Arrange
        var oldContent = "line1\n\nline2";
        var newContent = "line1\n\nline2";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_OnlyWhitespaceLine_IgnoreAll()
    {
        // Arrange
        var oldContent = "line1\n   \nline2";
        var newContent = "line1\n\nline2";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Whitespace-only lines become empty when collapsed
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_MixedLineEndings_Exact()
    {
        // Arrange - Different line ending styles
        var oldContent = "line1\r\nline2";
        var newContent = "line1\nline2";
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Note: Line ending handling depends on DiffPlex implementation
        // This test documents current behavior
        result.Should().NotBeNull();
    }

    [Fact]
    public void Compare_UnicodeWhitespace_Exact()
    {
        // Arrange - Unicode whitespace characters (non-breaking space)
        var oldContent = "word\u00A0word";  // Non-breaking space
        var newContent = "word word";       // Regular space
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.Exact,
            ContextLines = 0
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert - Unicode whitespace differs from regular space in exact mode
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_LargeFile_IgnoreAll_Performance()
    {
        // Arrange - Test that IgnoreAll mode handles larger files efficiently
        var lines = Enumerable.Range(1, 500).Select(i => $"   Line number {i}   ").ToList();
        var oldContent = string.Join("\n", lines);
        var newContent = string.Join("\n", lines.Select(l => l.Trim()));
        var options = new DiffOptions
        {
            WhitespaceMode = WhitespaceMode.IgnoreAll,
            ContextLines = 3
        };

        // Act
        var result = _differ.Compare(oldContent, newContent, options);

        // Assert
        result.Should().NotBeNull();
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion
}
