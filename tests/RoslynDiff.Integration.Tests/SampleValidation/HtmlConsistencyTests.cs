using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;
using FluentAssertions;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Tests HTML output consistency across different flag combinations and sample files.
/// Validates section integrity, data attributes, and format consistency.
/// </summary>
public class HtmlConsistencyTests : SampleValidationTestBase
{
    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Html001_FlagCombinationConsistency_HtmlToFile()
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
        var results = SampleDataValidator.ValidateHtmlConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Html002_SectionLineNumberIntegrity_NoOverlaps()
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
        var results = SampleDataValidator.ValidateHtmlConsistency(oldFile!, newFile!, options);

        // Assert
        var overlapsResults = results.Where(r =>
            r.Context.Contains("Overlap", StringComparison.OrdinalIgnoreCase) ||
            r.Context.Contains("Section", StringComparison.OrdinalIgnoreCase));

        foreach (var result in overlapsResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Html003_DataAttributeConsistency_MatchVisualDisplay()
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
        var results = SampleDataValidator.ValidateHtmlConsistency(oldFile!, newFile!, options);

        // Assert
        var dataAttributeResults = results.Where(r =>
            r.Context.Contains("Data Attribute", StringComparison.OrdinalIgnoreCase));

        foreach (var result in dataAttributeResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Html004_Calculator_ValidatesSuccessfully()
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
        var results = SampleDataValidator.ValidateHtmlConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Html005_UserService_ValidatesSuccessfully()
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
        var results = SampleDataValidator.ValidateHtmlConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Html006_AllSamples_HtmlParseable()
    {
        // Arrange
        var samplePairs = GetSampleFilePairs().ToList();

        samplePairs.Should().NotBeEmpty("Sample files should exist in before/after directories");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn
        };

        var allResults = new List<TestResult>();

        // Act
        foreach (var (oldFile, newFile, name) in samplePairs)
        {
            try
            {
                var results = SampleDataValidator.ValidateHtmlConsistency(oldFile, newFile, options);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                allResults.Add(TestResult.Fail(
                    $"HTML Validation for {name}",
                    $"Exception occurred: {ex.Message}",
                    new[] { ex.ToString() }
                ));
            }
        }

        // Assert
        allResults.Should().NotBeEmpty("Should have validation results");
        AssertAllPassed(allResults);
    }
}
