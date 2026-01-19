using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Validators;

namespace RoslynDiff.TestUtilities.Tests.Examples;

/// <summary>
/// Demonstrates how to use the SampleDataValidator in your tests.
/// This is an example class showing various usage patterns.
/// </summary>
public class SampleDataValidatorUsageExample
{
    /// <summary>
    /// Example 1: Basic usage with default options.
    /// Validates all aspects of the diff output.
    /// </summary>
    public void Example1_BasicUsage()
    {
        var oldFile = "path/to/old.cs";
        var newFile = "path/to/new.cs";

        // Run all validations with default options
        var results = SampleDataValidator.ValidateAll(oldFile, newFile);

        // Process results
        foreach (var result in results)
        {
            Console.WriteLine(result);

            if (!result.Passed)
            {
                Console.WriteLine($"  Failed: {result.Message}");
                foreach (var issue in result.Issues)
                {
                    Console.WriteLine($"    - {issue}");
                }
            }
        }
    }

    /// <summary>
    /// Example 2: Usage with custom options.
    /// Demonstrates configuring validation behavior.
    /// </summary>
    public void Example2_CustomOptions()
    {
        var oldFile = "path/to/old.cs";
        var newFile = "path/to/new.cs";

        var options = new SampleDataValidatorOptions
        {
            DiffMode = DiffMode.Roslyn,          // Force Roslyn mode
            PreserveTempFiles = true,             // Keep temp files for debugging
            TempOutputDirectory = "/tmp/roslyn",  // Use specific temp directory
            CliTimeoutMs = 60000                  // 60 second timeout
        };

        var results = SampleDataValidator.ValidateAll(oldFile, newFile, options);

        Console.WriteLine($"Validation completed with {results.Count()} results");
    }

    /// <summary>
    /// Example 3: Running specific validations individually.
    /// Useful when you only need certain validations.
    /// </summary>
    public void Example3_IndividualValidations()
    {
        var oldFile = "path/to/old.cs";
        var newFile = "path/to/new.cs";
        var options = new SampleDataValidatorOptions();

        // Run only JSON validation
        var jsonResults = SampleDataValidator.ValidateJsonConsistency(oldFile, newFile, options);
        Console.WriteLine($"JSON validation: {jsonResults.Count()} results");

        // Run only HTML validation
        var htmlResults = SampleDataValidator.ValidateHtmlConsistency(oldFile, newFile, options);
        Console.WriteLine($"HTML validation: {htmlResults.Count()} results");

        // Run only line number integrity checks
        var lineResults = SampleDataValidator.ValidateLineNumberIntegrity(oldFile, newFile, options);
        Console.WriteLine($"Line number validation: {lineResults.Count()} results");

        // Run only cross-format consistency checks
        var crossResults = SampleDataValidator.ValidateCrossFormatConsistency(oldFile, newFile, options);
        Console.WriteLine($"Cross-format validation: {crossResults.Count()} results");
    }

    /// <summary>
    /// Example 4: Aggregating results for summary reporting.
    /// </summary>
    public void Example4_AggregatingResults()
    {
        var oldFile = "path/to/old.cs";
        var newFile = "path/to/new.cs";

        var allResults = SampleDataValidator.ValidateAll(oldFile, newFile);

        // Get an aggregated summary
        var summary = SampleDataValidator.AggregateResults(allResults);

        Console.WriteLine(summary);

        if (!summary.Passed)
        {
            Console.WriteLine("Validation failed! Details:");
            foreach (var issue in summary.Issues)
            {
                Console.WriteLine($"  - {issue}");
            }
        }
    }

    /// <summary>
    /// Example 5: Using in xUnit tests with assertions.
    /// </summary>
    public void Example5_InXUnitTests()
    {
        var oldFile = "path/to/old.cs";
        var newFile = "path/to/new.cs";

        var results = SampleDataValidator.ValidateAll(oldFile, newFile);
        var summary = SampleDataValidator.AggregateResults(results);

        // Use xUnit assertions (in actual tests)
        // Assert.True(summary.Passed, summary.Message);

        // Or check specific validations
        var lineNumberResults = results.Where(r => r.Context.Contains("Line Number"));
        // Assert.True(lineNumberResults.All(r => r.Passed));
    }

    /// <summary>
    /// Example 6: Comparing different diff modes.
    /// Validates that Roslyn and Line modes produce consistent results.
    /// </summary>
    public void Example6_ComparingDiffModes()
    {
        var oldFile = "path/to/old.cs";
        var newFile = "path/to/new.cs";

        // Validate with Roslyn mode
        var roslynOptions = new SampleDataValidatorOptions { DiffMode = DiffMode.Roslyn };
        var roslynResults = SampleDataValidator.ValidateAll(oldFile, newFile, roslynOptions);

        // Validate with Line mode
        var lineOptions = new SampleDataValidatorOptions { DiffMode = DiffMode.Line };
        var lineResults = SampleDataValidator.ValidateAll(oldFile, newFile, lineOptions);

        Console.WriteLine($"Roslyn mode: {roslynResults.Count(r => r.Passed)} passed, {roslynResults.Count(r => !r.Passed)} failed");
        Console.WriteLine($"Line mode: {lineResults.Count(r => r.Passed)} passed, {lineResults.Count(r => !r.Passed)} failed");
    }

    /// <summary>
    /// Example 7: Batch validation of multiple file pairs.
    /// </summary>
    public void Example7_BatchValidation()
    {
        var filePairs = new[]
        {
            ("old1.cs", "new1.cs"),
            ("old2.cs", "new2.cs"),
            ("old3.cs", "new3.cs")
        };

        var allResults = new List<TestResult>();

        foreach (var (oldFile, newFile) in filePairs)
        {
            Console.WriteLine($"Validating {oldFile} -> {newFile}");
            var results = SampleDataValidator.ValidateAll(oldFile, newFile);
            allResults.AddRange(results);
        }

        // Get overall summary
        var overallSummary = SampleDataValidator.AggregateResults(allResults);
        Console.WriteLine($"\nOverall: {overallSummary}");
    }

    /// <summary>
    /// Example 8: Error handling and graceful degradation.
    /// </summary>
    public void Example8_ErrorHandling()
    {
        var oldFile = "path/to/old.cs";
        var newFile = "path/to/new.cs";

        try
        {
            var results = SampleDataValidator.ValidateAll(oldFile, newFile);

            // Check for validation errors
            var errors = results.Where(r => !r.Passed).ToList();
            if (errors.Any())
            {
                Console.WriteLine($"Found {errors.Count} validation errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  {error.Context}: {error.Message}");
                }
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File not found: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"CLI execution failed: {ex.Message}");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"Validation timed out: {ex.Message}");
        }
    }
}
