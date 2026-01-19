using RoslynDiff.TestUtilities.Models;
using FluentAssertions;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Base class for sample validation tests providing shared infrastructure.
/// </summary>
public abstract class SampleValidationTestBase : IDisposable
{
    private readonly List<string> _tempFiles = new();
    protected readonly string SamplesDirectory;
    protected readonly string TestFixturesDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SampleValidationTestBase"/> class.
    /// </summary>
    protected SampleValidationTestBase()
    {
        // Find the samples directory relative to the test assembly
        var baseDirectory = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", ".."));

        SamplesDirectory = Path.Combine(projectRoot, "samples");
        TestFixturesDirectory = Path.Combine(baseDirectory, "TestFixtures");
    }

    /// <summary>
    /// Gets all sample file pairs from the samples/before and samples/after directories.
    /// </summary>
    /// <returns>A collection of tuples containing old file path, new file path, and display name.</returns>
    protected IEnumerable<(string OldFile, string NewFile, string Name)> GetSampleFilePairs()
    {
        var beforeDir = Path.Combine(SamplesDirectory, "before");
        var afterDir = Path.Combine(SamplesDirectory, "after");

        if (!Directory.Exists(beforeDir) || !Directory.Exists(afterDir))
        {
            yield break;
        }

        var beforeFiles = Directory.GetFiles(beforeDir, "*.*", SearchOption.AllDirectories);

        foreach (var beforeFile in beforeFiles)
        {
            var relativePath = Path.GetRelativePath(beforeDir, beforeFile);
            var afterFile = Path.Combine(afterDir, relativePath);

            if (File.Exists(afterFile))
            {
                yield return (beforeFile, afterFile, relativePath);
            }
        }
    }

    /// <summary>
    /// Gets all test fixture file pairs from the TestFixtures directory.
    /// </summary>
    /// <returns>A collection of tuples containing old file path, new file path, and display name.</returns>
    protected IEnumerable<(string OldFile, string NewFile, string Name)> GetTestFixturePairs()
    {
        if (!Directory.Exists(TestFixturesDirectory))
        {
            yield break;
        }

        var subdirectories = Directory.GetDirectories(TestFixturesDirectory);

        foreach (var subdir in subdirectories)
        {
            var files = Directory.GetFiles(subdir, "*.*");
            var oldFiles = files.Where(f => f.Contains("_Old", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var oldFile in oldFiles)
            {
                var newFile = oldFile.Replace("_Old", "_New", StringComparison.OrdinalIgnoreCase);

                if (File.Exists(newFile))
                {
                    var name = Path.GetFileName(oldFile);
                    yield return (oldFile, newFile, name);
                }
            }
        }
    }

    /// <summary>
    /// Registers a temporary file for cleanup.
    /// </summary>
    /// <param name="filePath">The path to the temporary file.</param>
    protected void RegisterTempFile(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            _tempFiles.Add(filePath);
        }
    }

    /// <summary>
    /// Asserts that a test result passed.
    /// </summary>
    /// <param name="result">The test result to assert.</param>
    protected void AssertPassed(TestResult result)
    {
        result.Should().NotBeNull();
        result.Passed.Should().BeTrue(
            $"{result.Context} should pass. {result.Message}\n" +
            $"Issues: {string.Join("\n", result.Issues)}"
        );
    }

    /// <summary>
    /// Asserts that a test result failed.
    /// </summary>
    /// <param name="result">The test result to assert.</param>
    protected void AssertFailed(TestResult result)
    {
        result.Should().NotBeNull();
        result.Passed.Should().BeFalse($"{result.Context} should fail");
    }

    /// <summary>
    /// Asserts that all test results passed.
    /// </summary>
    /// <param name="results">The collection of test results to assert.</param>
    protected void AssertAllPassed(IEnumerable<TestResult> results)
    {
        var resultsList = results.ToList();
        resultsList.Should().NotBeEmpty("Expected at least one test result");

        var failures = resultsList.Where(r => !r.Passed).ToList();

        if (failures.Any())
        {
            var failureMessages = failures.Select(f =>
                $"- {f.Context}: {f.Message}\n  {string.Join("\n  ", f.Issues)}"
            );

            throw new Xunit.Sdk.XunitException(
                $"{failures.Count} of {resultsList.Count} validations failed:\n" +
                string.Join("\n", failureMessages)
            );
        }
    }

    /// <summary>
    /// Gets a summary of test results.
    /// </summary>
    /// <param name="results">The collection of test results.</param>
    /// <returns>A summary string.</returns>
    protected string GetResultsSummary(IEnumerable<TestResult> results)
    {
        var resultsList = results.ToList();
        var passed = resultsList.Count(r => r.Passed);
        var failed = resultsList.Count(r => !r.Passed);

        return $"Results: {passed} passed, {failed} failed (Total: {resultsList.Count})";
    }

    /// <summary>
    /// Finds a sample file by name.
    /// </summary>
    /// <param name="fileName">The file name to search for.</param>
    /// <param name="inBeforeDirectory">True to search in before directory, false for after.</param>
    /// <returns>The full path to the file, or null if not found.</returns>
    protected string? FindSampleFile(string fileName, bool inBeforeDirectory = true)
    {
        var directory = inBeforeDirectory
            ? Path.Combine(SamplesDirectory, "before")
            : Path.Combine(SamplesDirectory, "after");

        if (!Directory.Exists(directory))
        {
            return null;
        }

        var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
        return files.FirstOrDefault();
    }

    /// <summary>
    /// Checks if running in CI environment.
    /// </summary>
    /// <returns>True if running in CI, false otherwise.</returns>
    protected static bool IsRunningInCI()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_ID"));
    }

    /// <summary>
    /// Cleans up temporary files.
    /// </summary>
    public void Dispose()
    {
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

        GC.SuppressFinalize(this);
    }
}
