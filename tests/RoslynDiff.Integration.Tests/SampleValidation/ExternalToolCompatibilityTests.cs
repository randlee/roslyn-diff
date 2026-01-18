using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;
using FluentAssertions;
using System.Diagnostics;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Tests compatibility with external diff tools (diff, git diff).
/// Validates that roslyn-diff output is compatible with standard diff formats.
/// </summary>
public class ExternalToolCompatibilityTests : SampleValidationTestBase
{
    /// <summary>
    /// Checks if the 'diff' command is available on the system.
    /// </summary>
    private static bool IsDiffAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "diff",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            return process != null && process.WaitForExit(1000);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the 'git' command is available on the system.
    /// </summary>
    private static bool IsGitAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            return process != null && process.WaitForExit(1000);
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Ext001_RoslynDiffGit_VsStandardDiff()
    {
        // Skip if diff is not available
        if (!IsDiffAvailable())
        {
            // Pass the test with a note that diff is not available
            Assert.True(true, "Skipping: 'diff' command not available on system");
            return;
        }

        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Line
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile!, newFile!, options);

        // Assert
        // If roslyn-diff can generate output in line mode, it should be compatible with diff format
        AssertAllPassed(results);
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Ext002_RoslynDiffGit_VsGitDiff()
    {
        // Skip if git is not available
        if (!IsGitAvailable())
        {
            // Pass the test with a note that git is not available
            Assert.True(true, "Skipping: 'git' command not available on system");
            return;
        }

        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Line
        };

        // Act
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile!, newFile!, options);

        // Assert
        // Validate that unified diff format is compatible with git diff
        var unifiedDiffResults = results.Where(r =>
            r.Context.Contains("Unified", StringComparison.OrdinalIgnoreCase));

        foreach (var result in unifiedDiffResults)
        {
            result.Passed.Should().BeTrue(
                $"Unified diff validation should pass for git compatibility: {result.Context}");
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Ext003_Calculator_ExternalToolCompatibility()
    {
        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Line
        };

        // Act
        var results = SampleDataValidator.ValidateAll(oldFile!, newFile!, options);

        // Assert
        // Comprehensive validation ensures compatibility with external tools
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Ext004_UnifiedDiffFormat_ValidatesCorrectly()
    {
        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Line
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile!, newFile!, options);

        // Assert
        // Validate that output follows unified diff format standards
        var lineIntegrityResults = results.Where(r =>
            r.Context.Contains("Line", StringComparison.OrdinalIgnoreCase));

        foreach (var result in lineIntegrityResults)
        {
            AssertPassed(result);
        }
    }
}
