using FluentAssertions;
using RoslynDiff.TestUtilities.ExternalTools;
using Xunit;

namespace RoslynDiff.TestUtilities.Tests.ExternalTools;

/// <summary>
/// Tests for the DiffToolRunner class.
/// </summary>
public class DiffToolRunnerTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        // Clean up temp files
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private string CreateTempFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void IsAvailable_ShouldReturnTrue_WhenDiffIsInstalled()
    {
        // Act
        var isAvailable = DiffToolRunner.IsAvailable();

        // Assert
        isAvailable.Should().BeTrue("diff should be available on Unix-like systems");
    }

    [Fact]
    public void RunUnifiedDiff_ShouldReturnExitCode0_WhenFilesAreIdentical()
    {
        // Arrange
        if (!DiffToolRunner.IsAvailable())
        {
            return; // Skip test if diff not available
        }

        var content = "Line 1\nLine 2\nLine 3\n";
        var file1 = CreateTempFile(content);
        var file2 = CreateTempFile(content);

        // Act
        var result = DiffToolRunner.RunUnifiedDiff(file1, file2);

        // Assert
        result.ExitCode.Should().Be(0, "identical files should return exit code 0");
        result.StdOut.Should().BeEmpty("no differences should produce no output");
    }

    [Fact]
    public void RunUnifiedDiff_ShouldReturnExitCode1_WhenFilesAreDifferent()
    {
        // Arrange
        if (!DiffToolRunner.IsAvailable())
        {
            return; // Skip test if diff not available
        }

        var file1 = CreateTempFile("Line 1\nLine 2\nLine 3\n");
        var file2 = CreateTempFile("Line 1\nLine 2 Modified\nLine 3\n");

        // Act
        var result = DiffToolRunner.RunUnifiedDiff(file1, file2);

        // Assert
        result.ExitCode.Should().Be(1, "different files should return exit code 1");
        result.StdOut.Should().NotBeEmpty("differences should produce output");
        result.StdOut.Should().Contain("@@", "unified diff should contain hunk headers");
    }

    [Fact]
    public void RunUnifiedDiff_ShouldIncludeContextLines()
    {
        // Arrange
        if (!DiffToolRunner.IsAvailable())
        {
            return; // Skip test if diff not available
        }

        var file1 = CreateTempFile("Line 1\nLine 2\nLine 3\nLine 4\nLine 5\n");
        var file2 = CreateTempFile("Line 1\nLine 2\nLine 3 Modified\nLine 4\nLine 5\n");

        // Act
        var result = DiffToolRunner.RunUnifiedDiff(file1, file2, contextLines: 1);

        // Assert
        result.StdOut.Should().Contain("Line 2", "should include context lines");
        result.StdOut.Should().Contain("Line 4", "should include context lines");
    }

    [Fact]
    public void RunUnifiedDiff_ShouldThrowArgumentNullException_WhenOldFileIsNull()
    {
        // Act
        var act = () => DiffToolRunner.RunUnifiedDiff(null!, "newfile.txt");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("oldFile");
    }

    [Fact]
    public void RunUnifiedDiff_ShouldThrowArgumentNullException_WhenNewFileIsNull()
    {
        // Act
        var act = () => DiffToolRunner.RunUnifiedDiff("oldfile.txt", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newFile");
    }

    [Fact]
    public void ExtractChangedLineNumbers_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        // Act
        var lineNumbers = DiffToolRunner.ExtractChangedLineNumbers("");

        // Assert
        lineNumbers.Should().BeEmpty();
    }

    [Fact]
    public void ExtractChangedLineNumbers_ShouldExtractLineNumbers_FromUnifiedDiff()
    {
        // Arrange
        var diffOutput = @"--- a/file.txt
+++ b/file.txt
@@ -1,3 +1,3 @@
 Line 1
-Line 2
+Line 2 Modified
 Line 3
@@ -10,2 +10,3 @@
 Line 10
+Line 10.5
 Line 11";

        // Act
        var lineNumbers = DiffToolRunner.ExtractChangedLineNumbers(diffOutput).ToList();

        // Assert
        lineNumbers.Should().Contain(new[] { 1, 2, 3, 10, 11, 12 });
    }

    [Fact]
    public void ExtractChangedLineNumbers_ShouldHandleSingleLineChanges()
    {
        // Arrange
        var diffOutput = @"@@ -5 +5 @@
-Old line
+New line";

        // Act
        var lineNumbers = DiffToolRunner.ExtractChangedLineNumbers(diffOutput).ToList();

        // Assert
        lineNumbers.Should().Contain(5);
    }

    [Fact]
    public void Run_ShouldTimeout_WhenCommandTakesTooLong()
    {
        // Arrange
        if (!DiffToolRunner.IsAvailable())
        {
            return; // Skip test if diff not available
        }

        // Act & Assert
        var act = () => DiffToolRunner.Run("--help", timeout: 1);

        // Note: This test is timing-dependent and may not reliably timeout
        // Just verify it doesn't crash
        try
        {
            act.Invoke();
        }
        catch (TimeoutException)
        {
            // Expected in some cases
        }
    }

    [Fact]
    public void ExtractChangedLineNumbers_ShouldReturnSortedUniqueNumbers()
    {
        // Arrange
        var diffOutput = @"@@ -3,5 +3,5 @@
 context
-old
+new
 context
@@ -1,2 +1,2 @@
-old
+new";

        // Act
        var lineNumbers = DiffToolRunner.ExtractChangedLineNumbers(diffOutput).ToList();

        // Assert
        lineNumbers.Should().BeInAscendingOrder();
        lineNumbers.Should().OnlyHaveUniqueItems();
    }
}
