using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;
using Xunit;
using FluentAssertions;

namespace RoslynDiff.Integration.Tests.SampleValidation;

/// <summary>
/// Tests coverage of all sample files in the samples directory.
/// Ensures comprehensive validation of all provided test cases.
/// </summary>
public class SampleCoverageTests : SampleValidationTestBase
{
    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Samp001_AllSamplesDirectory_ValidateAll()
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
                var results = SampleDataValidator.ValidateAll(oldFile, newFile, options);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                allResults.Add(TestResult.Fail(
                    $"Validation for {name}",
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
    public void Samp002_Calculator_CompleteValidation()
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
        var results = SampleDataValidator.ValidateAll(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");

        // Verify we ran multiple validations
        var resultsList = results.ToList();
        resultsList.Should().HaveCountGreaterThan(3, "Should run multiple validation types");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Samp003_UserService_CompleteValidation()
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
        var results = SampleDataValidator.ValidateAll(oldFile!, newFile!, options);

        // Assert
        AssertAllPassed(results);
        GetResultsSummary(results).Should().Contain("passed");

        // Verify we ran multiple validations
        var resultsList = results.ToList();
        resultsList.Should().HaveCountGreaterThan(3, "Should run multiple validation types");
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Samp004_SampleCount_MatchesExpected()
    {
        // Arrange
        var samplePairs = GetSampleFilePairs().ToList();

        // Assert
        samplePairs.Should().NotBeEmpty("Sample files should exist in before/after directories");

        // Verify we have expected samples
        var calculatorPair = samplePairs.FirstOrDefault(p =>
            p.Name.Contains("Calculator", StringComparison.OrdinalIgnoreCase));

        calculatorPair.Should().NotBe(default, "Calculator.cs should be in samples");

        var userServicePair = samplePairs.FirstOrDefault(p =>
            p.Name.Contains("UserService", StringComparison.OrdinalIgnoreCase));

        userServicePair.Should().NotBe(default, "UserService.cs should be in samples");

        // Verify sample files are accessible
        foreach (var (oldFile, newFile, name) in samplePairs)
        {
            File.Exists(oldFile).Should().BeTrue($"Old file should exist: {name}");
            File.Exists(newFile).Should().BeTrue($"New file should exist: {name}");
        }
    }

    [Fact]
    [Trait("Category", "SampleValidation")]
    public void Samp005_AllSamples_LineMode_ValidateAll()
    {
        // Arrange
        var samplePairs = GetSampleFilePairs().ToList();

        samplePairs.Should().NotBeEmpty("Sample files should exist in before/after directories");

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Line
        };

        var allResults = new List<TestResult>();

        // Act
        foreach (var (oldFile, newFile, name) in samplePairs)
        {
            try
            {
                var results = SampleDataValidator.ValidateAll(oldFile, newFile, options);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                allResults.Add(TestResult.Fail(
                    $"Line Mode Validation for {name}",
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
