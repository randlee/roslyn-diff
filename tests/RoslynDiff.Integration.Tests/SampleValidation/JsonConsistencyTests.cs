using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;
using FluentAssertions;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Tests JSON output consistency across different flag combinations and sample files.
/// Validates line number integrity, parseability, and format consistency.
/// </summary>
public class JsonConsistencyTests : SampleValidationTestBase
{
    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Json001_FlagCombinationConsistency_JsonVsJsonQuiet()
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
        var results = SampleDataValidator.ValidateJsonConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Json002_LineNumberIntegrity_NoOverlaps()
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
        var overlapsResults = results.Where(r => r.Context.Contains("Overlaps", StringComparison.OrdinalIgnoreCase));
        foreach (var result in overlapsResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Json003_LineNumberIntegrity_NoDuplicates()
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
        var duplicateResults = results.Where(r => r.Context.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
        foreach (var result in duplicateResults)
        {
            AssertPassed(result);
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Json004_Calculator_ValidatesSuccessfully()
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
        var results = SampleDataValidator.ValidateJsonConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Json005_UserService_ValidatesSuccessfully()
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
        var results = SampleDataValidator.ValidateJsonConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Json006_AllSamples_JsonParseable()
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
                var results = SampleDataValidator.ValidateJsonConsistency(oldFile, newFile, options);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                allResults.Add(TestResult.Fail(
                    $"JSON Validation for {name}",
                    $"Exception occurred: {ex.Message}",
                    new[] { ex.ToString() }
                ));
            }
        }

        // Assert
        allResults.Should().NotBeEmpty("Should have validation results");
        AssertAllPassed(allResults);
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Json007_LineMode_Calculator_ValidatesSuccessfully()
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
        var results = SampleDataValidator.ValidateJsonConsistency(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
    }
}
