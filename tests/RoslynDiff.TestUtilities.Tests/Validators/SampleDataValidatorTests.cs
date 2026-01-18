using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;

namespace RoslynDiff.TestUtilities.Tests.Validators;

/// <summary>
/// Integration tests for the SampleDataValidator class.
/// These tests demonstrate basic usage and verify the validator orchestrates all validations correctly.
/// </summary>
public class SampleDataValidatorTests
{
    private readonly string _sampleFilesPath;

    public SampleDataValidatorTests()
    {
        // Locate the samples directory
        var baseDir = AppContext.BaseDirectory;
        _sampleFilesPath = Path.Combine(baseDir, "..", "..", "..", "..", "..", "samples");

        // Normalize the path
        _sampleFilesPath = Path.GetFullPath(_sampleFilesPath);
    }

    [Fact]
    public void ValidateAll_WithValidFiles_ReturnsResults()
    {
        // Arrange
        var oldFile = FindSampleFile("before", "*.cs");
        var newFile = FindSampleFile("after", "*.cs");

        if (oldFile == null || newFile == null)
        {
            // Skip test if sample files are not available
            return;
        }

        var options = new SampleDataValidatorOptions
        {
            PreserveTempFiles = false,
            CliTimeoutMs = 60000 // Increase timeout for CI environments
        };

        // Act
        var results = SampleDataValidator.ValidateAll(oldFile, newFile, options).ToList();

        // Assert
        Assert.NotEmpty(results);

        // Display all results for debugging
        foreach (var result in results)
        {
            Console.WriteLine(result.ToString());
        }

        // At least some validations should run
        Assert.True(results.Count >= 1, "Expected at least one validation result");
    }

    [Fact]
    public void ValidateLineNumberIntegrity_WithValidFiles_ReturnsResults()
    {
        // Arrange
        var oldFile = FindSampleFile("before", "*.cs");
        var newFile = FindSampleFile("after", "*.cs");

        if (oldFile == null || newFile == null)
        {
            return;
        }

        var options = new SampleDataValidatorOptions
        {
            PreserveTempFiles = false
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile, newFile, options).ToList();

        // Assert
        Assert.NotEmpty(results);

        foreach (var result in results)
        {
            Console.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void ValidateJsonConsistency_WithValidFiles_ReturnsResults()
    {
        // Arrange
        var oldFile = FindSampleFile("before", "*.cs");
        var newFile = FindSampleFile("after", "*.cs");

        if (oldFile == null || newFile == null)
        {
            return;
        }

        var options = new SampleDataValidatorOptions
        {
            PreserveTempFiles = false
        };

        // Act
        var results = SampleDataValidator.ValidateJsonConsistency(oldFile, newFile, options).ToList();

        // Assert
        Assert.NotEmpty(results);

        foreach (var result in results)
        {
            Console.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void ValidateHtmlConsistency_WithValidFiles_ReturnsResults()
    {
        // Arrange
        var oldFile = FindSampleFile("before", "*.cs");
        var newFile = FindSampleFile("after", "*.cs");

        if (oldFile == null || newFile == null)
        {
            return;
        }

        var options = new SampleDataValidatorOptions
        {
            PreserveTempFiles = false
        };

        // Act
        var results = SampleDataValidator.ValidateHtmlConsistency(oldFile, newFile, options).ToList();

        // Assert
        Assert.NotEmpty(results);

        foreach (var result in results)
        {
            Console.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void ValidateCrossFormatConsistency_WithValidFiles_ReturnsResults()
    {
        // Arrange
        var oldFile = FindSampleFile("before", "*.cs");
        var newFile = FindSampleFile("after", "*.cs");

        if (oldFile == null || newFile == null)
        {
            return;
        }

        var options = new SampleDataValidatorOptions
        {
            PreserveTempFiles = false
        };

        // Act
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile, newFile, options).ToList();

        // Assert
        Assert.NotEmpty(results);

        foreach (var result in results)
        {
            Console.WriteLine(result.ToString());
        }
    }

    [Fact]
    public void AggregateResults_WithMixedResults_ReturnsCorrectSummary()
    {
        // Arrange
        var results = new List<TestResult>
        {
            TestResult.Pass("Test 1", "Success"),
            TestResult.Pass("Test 2", "Success"),
            TestResult.Fail("Test 3", "Failed", new[] { "Issue 1" }),
            TestResult.Pass("Test 4", "Success")
        };

        // Act
        var aggregated = SampleDataValidator.AggregateResults(results);

        // Assert
        Assert.False(aggregated.Passed);
        Assert.Contains("1 of 4", aggregated.Message);
        Assert.Single(aggregated.Issues);
    }

    [Fact]
    public void AggregateResults_WithAllPassingResults_ReturnsPass()
    {
        // Arrange
        var results = new List<TestResult>
        {
            TestResult.Pass("Test 1", "Success"),
            TestResult.Pass("Test 2", "Success"),
            TestResult.Pass("Test 3", "Success")
        };

        // Act
        var aggregated = SampleDataValidator.AggregateResults(results);

        // Assert
        Assert.True(aggregated.Passed);
        Assert.Contains("All 3", aggregated.Message);
        Assert.Empty(aggregated.Issues);
    }

    [Fact]
    public void ValidateAll_WithNullOldFile_ThrowsArgumentNullException()
    {
        // Arrange
        string oldFile = null!;
        var newFile = "new.cs";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SampleDataValidator.ValidateAll(oldFile, newFile).ToList());
    }

    [Fact]
    public void ValidateAll_WithNullNewFile_ThrowsArgumentNullException()
    {
        // Arrange
        var oldFile = "old.cs";
        string newFile = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SampleDataValidator.ValidateAll(oldFile, newFile).ToList());
    }

    [Fact]
    public void ValidateAll_WithNonExistentOldFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var oldFile = "nonexistent-old.cs";
        var newFile = "nonexistent-new.cs";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            SampleDataValidator.ValidateAll(oldFile, newFile).ToList());
    }

    [Fact]
    public void SampleDataValidatorOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SampleDataValidatorOptions();

        // Assert
        Assert.True(options.IgnoreTimestamps);
        Assert.Equal(DiffMode.Auto, options.DiffMode);
        Assert.False(options.IncludeExternalTools);
        Assert.Null(options.TempOutputDirectory);
        Assert.False(options.PreserveTempFiles);
        Assert.Equal(30000, options.CliTimeoutMs);
        Assert.Null(options.RoslynDiffCliPath);
    }

    /// <summary>
    /// Finds a sample file in the samples directory.
    /// </summary>
    /// <param name="subdirectory">The subdirectory (before/after).</param>
    /// <param name="pattern">File pattern to search for.</param>
    /// <returns>Path to the first matching file, or null if not found.</returns>
    private string? FindSampleFile(string subdirectory, string pattern)
    {
        var searchPath = Path.Combine(_sampleFilesPath, subdirectory);

        if (!Directory.Exists(searchPath))
        {
            return null;
        }

        var files = Directory.GetFiles(searchPath, pattern, SearchOption.TopDirectoryOnly);
        return files.FirstOrDefault();
    }
}
