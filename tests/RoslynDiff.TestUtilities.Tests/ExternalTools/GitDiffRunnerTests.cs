using FluentAssertions;
using RoslynDiff.TestUtilities.ExternalTools;
using Xunit;

namespace RoslynDiff.TestUtilities.Tests.ExternalTools;

/// <summary>
/// Tests for the GitDiffRunner class.
/// </summary>
public class GitDiffRunnerTests : IDisposable
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
    public void IsAvailable_ShouldReturnTrue_WhenGitIsInstalled()
    {
        // Act
        var isAvailable = GitDiffRunner.IsAvailable();

        // Assert
        isAvailable.Should().BeTrue("git should be available on development machines");
    }

    [Fact]
    public void RunDiff_ShouldReturnExitCode0_WhenFilesAreIdentical()
    {
        // Arrange
        if (!GitDiffRunner.IsAvailable())
        {
            return; // Skip test if git not available
        }

        var content = "Line 1\nLine 2\nLine 3\n";
        var file1 = CreateTempFile(content);
        var file2 = CreateTempFile(content);

        // Act
        var result = GitDiffRunner.RunDiff(file1, file2);

        // Assert
        result.ExitCode.Should().Be(0, "identical files should return exit code 0");
        result.StdOut.Should().BeEmpty("no differences should produce no output");
    }

    [Fact]
    public void RunDiff_ShouldReturnExitCode1_WhenFilesAreDifferent()
    {
        // Arrange
        if (!GitDiffRunner.IsAvailable())
        {
            return; // Skip test if git not available
        }

        var file1 = CreateTempFile("Line 1\nLine 2\nLine 3\n");
        var file2 = CreateTempFile("Line 1\nLine 2 Modified\nLine 3\n");

        // Act
        var result = GitDiffRunner.RunDiff(file1, file2);

        // Assert
        result.ExitCode.Should().Be(1, "different files should return exit code 1");
        result.StdOut.Should().NotBeEmpty("differences should produce output");
        result.StdOut.Should().Contain("@@", "git diff should contain hunk headers");
    }

    [Fact]
    public void RunDiff_ShouldIncludeContextLines()
    {
        // Arrange
        if (!GitDiffRunner.IsAvailable())
        {
            return; // Skip test if git not available
        }

        var file1 = CreateTempFile("Line 1\nLine 2\nLine 3\nLine 4\nLine 5\n");
        var file2 = CreateTempFile("Line 1\nLine 2\nLine 3 Modified\nLine 4\nLine 5\n");

        // Act
        var result = GitDiffRunner.RunDiff(file1, file2, contextLines: 1);

        // Assert
        // The diff should contain the modified line
        result.StdOut.Should().Contain("Line 3", "should include the modified line");
        // Context lines should be present (at least one of them)
        var hasContext = result.StdOut.Contains("Line 2") || result.StdOut.Contains("Line 4");
        hasContext.Should().BeTrue("should include at least one context line");
    }

    [Fact]
    public void RunDiff_ShouldThrowArgumentNullException_WhenOldFileIsNull()
    {
        // Act
        var act = () => GitDiffRunner.RunDiff(null!, "newfile.txt");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("oldFile");
    }

    [Fact]
    public void RunDiff_ShouldThrowArgumentNullException_WhenNewFileIsNull()
    {
        // Act
        var act = () => GitDiffRunner.RunDiff("oldfile.txt", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newFile");
    }

    [Fact]
    public void ExtractChangedLineNumbers_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        // Act
        var lineNumbers = GitDiffRunner.ExtractChangedLineNumbers("");

        // Assert
        lineNumbers.Should().BeEmpty();
    }

    [Fact]
    public void ExtractChangedLineNumbers_ShouldExtractLineNumbers_FromGitDiff()
    {
        // Arrange
        var diffOutput = @"diff --git a/file.txt b/file.txt
index abc123..def456 100644
--- a/file.txt
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
        var lineNumbers = GitDiffRunner.ExtractChangedLineNumbers(diffOutput).ToList();

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
        var lineNumbers = GitDiffRunner.ExtractChangedLineNumbers(diffOutput).ToList();

        // Assert
        lineNumbers.Should().Contain(5);
    }

    [Fact]
    public void Run_ShouldExecuteGitCommand()
    {
        // Arrange
        if (!GitDiffRunner.IsAvailable())
        {
            return; // Skip test if git not available
        }

        // Act
        var result = GitDiffRunner.Run("--version");

        // Assert
        result.ExitCode.Should().Be(0);
        result.StdOut.Should().Contain("git version");
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
        var lineNumbers = GitDiffRunner.ExtractChangedLineNumbers(diffOutput).ToList();

        // Assert
        lineNumbers.Should().BeInAscendingOrder();
        lineNumbers.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void RunDiff_ShouldProduceUnifiedDiffFormat()
    {
        // Arrange
        if (!GitDiffRunner.IsAvailable())
        {
            return; // Skip test if git not available
        }

        var file1 = CreateTempFile("Original content");
        var file2 = CreateTempFile("Modified content");

        // Act
        var result = GitDiffRunner.RunDiff(file1, file2);

        // Assert
        result.StdOut.Should().Contain("---", "should have old file marker");
        result.StdOut.Should().Contain("+++", "should have new file marker");
        result.StdOut.Should().Contain("@@", "should have hunk header");
    }
}
