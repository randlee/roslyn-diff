using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;
using FluentAssertions;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Tests line number integrity across all output formats.
/// Validates that line numbers don't overlap, duplicate, or have other integrity issues.
/// </summary>
public class LineNumberIntegrityTests : SampleValidationTestBase
{
    [Fact]
    [Trait("Category", "SampleValidation")]
    public void LineIntegrity001_AllFormats_NoOverlaps()
    {
        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile!, newFile!, options);

        // Assert
        var overlapResults = results.Where(r =>
            r.Context.Contains("Overlap", StringComparison.OrdinalIgnoreCase));

        foreach (var result in overlapResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void LineIntegrity002_AllFormats_NoDuplicates()
    {
        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile!, newFile!, options);

        // Assert
        var duplicateResults = results.Where(r =>
            r.Context.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));

        foreach (var result in duplicateResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void LineIntegrity003_Calculator_IntegrityCheck()
    {
        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void LineIntegrity004_UserService_IntegrityCheck()
    {
        // Arrange
        var oldFile = FindSampleFile("UserService.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("UserService.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("UserService.cs should exist in before directory");
        newFile.Should().NotBeNull("UserService.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void LineIntegrity005_RoslynMode_SequentialLineNumbers()
    {
        // Arrange
        var oldFile = FindSampleFile("Calculator.cs", inBeforeDirectory: true);
        var newFile = FindSampleFile("Calculator.cs", inBeforeDirectory: false);

        oldFile.Should().NotBeNull("Calculator.cs should exist in before directory");
        newFile.Should().NotBeNull("Calculator.cs should exist in after directory");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn
        };

        // Act
        var results = SampleDataValidator.ValidateLineNumberIntegrity(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void LineIntegrity006_LineMode_SequentialLineNumbers()
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
        AssertAllPassed(results);
    }
}
