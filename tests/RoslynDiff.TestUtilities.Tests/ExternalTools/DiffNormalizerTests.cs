using FluentAssertions;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.ExternalTools;
using RoslynDiff.TestUtilities.Models;
using Xunit;

namespace RoslynDiff.TestUtilities.Tests.ExternalTools;

/// <summary>
/// Tests for the DiffNormalizer class.
/// </summary>
public class DiffNormalizerTests
{
    [Fact]
    public void NormalizeWhitespace_ShouldConvertCRLFToLF()
    {
        // Arrange
        var input = "Line 1\r\nLine 2\r\nLine 3\r\n";

        // Act
        var result = DiffNormalizer.NormalizeWhitespace(input);

        // Assert
        result.Should().Be("Line 1\nLine 2\nLine 3");
    }

    [Fact]
    public void NormalizeWhitespace_ShouldRemoveTrailingWhitespace()
    {
        // Arrange
        var input = "Line 1   \nLine 2\t\nLine 3 ";

        // Act
        var result = DiffNormalizer.NormalizeWhitespace(input);

        // Assert
        result.Should().Be("Line 1\nLine 2\nLine 3");
    }

    [Fact]
    public void NormalizeWhitespace_ShouldRemoveTrailingEmptyLines()
    {
        // Arrange
        var input = "Line 1\nLine 2\n\n\n";

        // Act
        var result = DiffNormalizer.NormalizeWhitespace(input);

        // Assert
        result.Should().Be("Line 1\nLine 2");
    }

    [Fact]
    public void NormalizeWhitespace_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        // Act
        var act = () => DiffNormalizer.NormalizeWhitespace(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NormalizeHunkHeaders_ShouldAddMissingLineCounts()
    {
        // Arrange
        var input = "@@ -5 +5 @@ context\n@@ -10,3 +10,5 @@";

        // Act
        var result = DiffNormalizer.NormalizeHunkHeaders(input);

        // Assert
        result.Should().Contain("@@ -5,1 +5,1 @@");
        result.Should().Contain("@@ -10,3 +10,5 @@");
    }

    [Fact]
    public void NormalizeHunkHeaders_ShouldHandleMultipleHunks()
    {
        // Arrange
        var input = "@@ -1 +1 @@\n-old\n+new\n@@ -5,2 +5,3 @@\ncontext";

        // Act
        var result = DiffNormalizer.NormalizeHunkHeaders(input);

        // Assert
        result.Should().Contain("@@ -1,1 +1,1 @@");
        result.Should().Contain("@@ -5,2 +5,3 @@");
    }

    [Fact]
    public void NormalizeHunkHeaders_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        // Act
        var act = () => DiffNormalizer.NormalizeHunkHeaders(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StripFileTimestamps_ShouldRemoveTimestampsFromHeaders()
    {
        // Arrange
        var input = @"--- file1.txt	2024-01-17 10:30:45.123 +0000
+++ file2.txt	2024-01-17 10:31:00.456 +0000
@@ -1 +1 @@";

        // Act
        var result = DiffNormalizer.StripFileTimestamps(input);

        // Assert
        result.Should().NotContain("2024-01-17");
        result.Should().NotContain("10:30:45");
        result.Should().Contain("--- file1.txt");
        result.Should().Contain("+++ file2.txt");
    }

    [Fact]
    public void StripFileTimestamps_ShouldHandleVariousTimestampFormats()
    {
        // Arrange
        var input = @"--- file1.txt	2024-01-17 10:30:45
+++ file2.txt	2024-12-31 23:59:59.999999 -0800";

        // Act
        var result = DiffNormalizer.StripFileTimestamps(input);

        // Assert
        result.Should().NotContain("2024");
        result.Should().NotContain("10:30");
        result.Should().NotContain("23:59");
    }

    [Fact]
    public void StripFileTimestamps_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        // Act
        var act = () => DiffNormalizer.StripFileTimestamps(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NormalizeAll_ShouldApplyAllNormalizations()
    {
        // Arrange
        var input = @"--- file1.txt	2024-01-17 10:30:45
+++ file2.txt	2024-01-17 10:31:00
@@ -5 +5 @@
-old line
+new line
";

        // Act
        var result = DiffNormalizer.NormalizeAll(input);

        // Assert
        result.Should().NotContain("2024-01-17", "should strip timestamps");
        result.Should().Contain("@@ -5,1 +5,1 @@", "should normalize hunk headers");
        result.Should().NotEndWith("\n", "should remove trailing whitespace");
        result.Split('\n').All(line => !line.EndsWith(" ")).Should().BeTrue("should remove trailing spaces");
    }

    [Fact]
    public void CompareUnifiedDiffs_ShouldReturnPass_WhenDiffsAreIdentical()
    {
        // Arrange
        var diff1 = "@@ -1,3 +1,3 @@\n Line 1\n-Line 2\n+Line 2 Modified\n Line 3";
        var diff2 = "@@ -1,3 +1,3 @@\n Line 1\n-Line 2\n+Line 2 Modified\n Line 3";

        // Act
        var result = DiffNormalizer.CompareUnifiedDiffs(diff1, diff2);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void CompareUnifiedDiffs_ShouldReturnPass_WhenDiffsAreEquivalentAfterNormalization()
    {
        // Arrange
        var diff1 = "@@ -5 +5 @@\n-old\n+new";
        var diff2 = "@@ -5,1 +5,1 @@\n-old\n+new";

        // Act
        var result = DiffNormalizer.CompareUnifiedDiffs(diff1, diff2);

        // Assert
        result.Passed.Should().BeTrue("differences in hunk header format should be normalized");
    }

    [Fact]
    public void CompareUnifiedDiffs_ShouldReturnFail_WhenDiffsAreDifferent()
    {
        // Arrange
        var diff1 = "@@ -1 +1 @@\n-old line 1\n+new line 1";
        var diff2 = "@@ -1 +1 @@\n-old line 2\n+new line 2";

        // Act
        var result = DiffNormalizer.CompareUnifiedDiffs(diff1, diff2);

        // Assert
        result.Passed.Should().BeFalse();
        result.Issues.Should().NotBeEmpty();
    }

    [Fact]
    public void CompareUnifiedDiffs_ShouldIgnoreFileNames_WhenFlagIsSet()
    {
        // Arrange
        var diff1 = "--- file1.txt\n+++ file1.txt\n@@ -1 +1 @@\n-old\n+new";
        var diff2 = "--- file2.txt\n+++ file2.txt\n@@ -1 +1 @@\n-old\n+new";

        // Act
        var result = DiffNormalizer.CompareUnifiedDiffs(diff1, diff2, ignoreFileNames: true);

        // Assert
        result.Passed.Should().BeTrue("file names should be ignored");
    }

    [Fact]
    public void CompareUnifiedDiffs_ShouldThrowArgumentNullException_WhenFirstDiffIsNull()
    {
        // Act
        var act = () => DiffNormalizer.CompareUnifiedDiffs(null!, "diff2");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CompareUnifiedDiffs_ShouldThrowArgumentNullException_WhenSecondDiffIsNull()
    {
        // Act
        var act = () => DiffNormalizer.CompareUnifiedDiffs("diff1", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CompareParsedResults_ShouldReturnPass_WhenResultsAreEquivalent()
    {
        // Arrange
        var result1 = new ParsedDiffResult
        {
            Format = "unified-diff",
            Changes = new[]
            {
                new ParsedChange
                {
                    ChangeType = "hunk",
                    LineRange = new LineRange(1, 3, "new"),
                    OldLineRange = new LineRange(1, 3, "old")
                }
            }
        };

        var result2 = new ParsedDiffResult
        {
            Format = "unified-diff",
            Changes = new[]
            {
                new ParsedChange
                {
                    ChangeType = "hunk",
                    LineRange = new LineRange(1, 3, "new"),
                    OldLineRange = new LineRange(1, 3, "old")
                }
            }
        };

        // Act
        var compareResult = DiffNormalizer.CompareParsedResults(result1, result2);

        // Assert
        compareResult.Passed.Should().BeTrue();
    }

    [Fact]
    public void CompareParsedResults_ShouldReturnFail_WhenChangeCountsDiffer()
    {
        // Arrange
        var result1 = new ParsedDiffResult
        {
            Format = "unified-diff",
            Changes = new[]
            {
                new ParsedChange { ChangeType = "hunk" }
            }
        };

        var result2 = new ParsedDiffResult
        {
            Format = "unified-diff",
            Changes = new[]
            {
                new ParsedChange { ChangeType = "hunk" },
                new ParsedChange { ChangeType = "hunk" }
            }
        };

        // Act
        var result = DiffNormalizer.CompareParsedResults(result1, result2);

        // Assert
        result.Passed.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Contains("Change count mismatch"));
    }

    [Fact]
    public void ExtractContentLines_ShouldExtractOnlyDiffLines()
    {
        // Arrange
        var input = @"--- file1.txt
+++ file2.txt
@@ -1,3 +1,3 @@
 Context line
-Removed line
+Added line
 Another context";

        // Act
        var result = DiffNormalizer.ExtractContentLines(input);

        // Assert
        result.Should().Contain("Context line");
        result.Should().Contain("-Removed line");
        result.Should().Contain("+Added line");
        result.Should().NotContain("--- file1.txt");
        result.Should().NotContain("+++ file2.txt");
        result.Should().NotContain("@@");
    }

    [Fact]
    public void ExtractContentLines_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        // Act
        var act = () => DiffNormalizer.ExtractContentLines(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
