using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;
using FluentAssertions;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Tests consistency across different output formats (JSON, HTML, Text).
/// Validates that all formats report the same line numbers and changes.
/// </summary>
public class CrossFormatConsistencyTests : SampleValidationTestBase
{
    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Xfmt001_JsonVsHtml_LineNumbersMatch()
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
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile!, newFile!, options);

        // Assert
        var lineNumberResults = results.Where(r =>
            r.Context.Contains("Line Number", StringComparison.OrdinalIgnoreCase));

        foreach (var result in lineNumberResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Xfmt002_JsonVsText_LineNumbersMatch()
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
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Xfmt003_AllFormats_RoslynMode_Agreement()
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
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile!, newFile!, options);

        // Assert
        var rangeCountResults = results.Where(r =>
            r.Context.Contains("Range Count", StringComparison.OrdinalIgnoreCase));

        foreach (var result in rangeCountResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Xfmt004_AllFormats_LineMode_Agreement()
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
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Xfmt005_Calculator_AllFormatsConsistent()
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
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Xfmt006_UserService_AllFormatsConsistent()
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
        var results = SampleDataValidator.ValidateCrossFormatConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }
}
