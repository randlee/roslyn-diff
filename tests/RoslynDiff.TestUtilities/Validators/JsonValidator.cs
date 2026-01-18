using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Parsers;

namespace RoslynDiff.TestUtilities.Validators;

/// <summary>
/// Validates JSON output format from roslyn-diff for consistency and integrity.
/// </summary>
public static class JsonValidator
{
    /// <summary>
    /// Validates that JSON content is consistent across different flag combinations.
    /// </summary>
    /// <param name="files">Dictionary mapping flag combination names to their file contents.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method ensures that the same JSON structure is produced regardless of flags like:
    /// <list type="bullet">
    /// <item><description>--json</description></item>
    /// <item><description>--json --quiet</description></item>
    /// <item><description>--json file.json</description></item>
    /// </list>
    /// Whitespace differences are normalized before comparison.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="files"/> is <c>null</c>.</exception>
    public static TestResult ValidateFlagCombinationConsistency(Dictionary<string, string> files)
    {
        if (files == null)
        {
            throw new ArgumentNullException(nameof(files));
        }

        if (!files.Any())
        {
            return TestResult.Pass(
                "JSON Flag Combination Consistency",
                "No files provided (empty dictionary is valid)"
            );
        }

        if (files.Count == 1)
        {
            return TestResult.Pass(
                "JSON Flag Combination Consistency",
                "Only one file provided (no comparison needed)"
            );
        }

        var parser = new JsonOutputParser();
        var issues = new List<string>();
        var normalizedContents = new Dictionary<string, string>();

        // Parse and normalize all JSON content
        foreach (var kvp in files)
        {
            var flagCombo = kvp.Key;
            var content = kvp.Value;

            if (string.IsNullOrWhiteSpace(content))
            {
                issues.Add($"Flag combination '{flagCombo}' produced empty or null content");
                continue;
            }

            if (!parser.CanParse(content))
            {
                issues.Add($"Flag combination '{flagCombo}' did not produce valid JSON");
                continue;
            }

            normalizedContents[flagCombo] = parser.Normalize(content);
        }

        if (issues.Any())
        {
            return TestResult.Fail(
                "JSON Flag Combination Consistency",
                $"Found {issues.Count} parsing issue(s)",
                issues
            );
        }

        // Compare all normalized contents
        var first = normalizedContents.First();
        var differences = new List<string>();

        foreach (var kvp in normalizedContents.Skip(1))
        {
            if (kvp.Value != first.Value)
            {
                differences.Add($"JSON from '{kvp.Key}' differs from '{first.Key}'");
            }
        }

        if (differences.Any())
        {
            return TestResult.Fail(
                "JSON Flag Combination Consistency",
                $"Found {differences.Count} inconsistency(ies) across flag combinations",
                differences
            );
        }

        return TestResult.Pass(
            "JSON Flag Combination Consistency",
            $"All {files.Count} flag combinations produce identical JSON output"
        );
    }

    /// <summary>
    /// Validates the integrity of line numbers in JSON content.
    /// </summary>
    /// <param name="jsonContent">The JSON content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method checks for:
    /// <list type="bullet">
    /// <item><description>Overlapping line ranges</description></item>
    /// <item><description>Duplicate line numbers</description></item>
    /// <item><description>Invalid line number values (negative, zero)</description></item>
    /// </list>
    /// Uses <see cref="JsonOutputParser"/> to extract line information and
    /// <see cref="LineNumberValidator"/> to perform integrity checks.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateLineNumberIntegrity(string jsonContent)
    {
        if (jsonContent == null)
        {
            throw new ArgumentNullException(nameof(jsonContent));
        }

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return TestResult.Pass(
                "JSON Line Number Integrity",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new JsonOutputParser();

        if (!parser.CanParse(jsonContent))
        {
            return TestResult.Fail(
                "JSON Line Number Integrity",
                "Content is not valid JSON",
                new[] { "Cannot parse JSON content - please verify the input is valid JSON" }
            );
        }

        var allIssues = new List<string>();

        // Extract and validate line ranges
        var lineRanges = parser.ExtractLineRanges(jsonContent).ToList();
        if (lineRanges.Any())
        {
            var rangeResult = LineNumberValidator.ValidateNoOverlaps(
                lineRanges,
                "JSON Line Ranges"
            );

            if (!rangeResult.Passed)
            {
                allIssues.AddRange(rangeResult.Issues);
            }
        }

        // Extract and validate individual line numbers
        var lineNumbers = parser.ExtractLineNumbers(jsonContent).ToList();
        if (lineNumbers.Any())
        {
            // Check for duplicates
            var duplicateResult = LineNumberValidator.ValidateNoDuplicates(
                lineNumbers,
                "JSON Line Numbers"
            );

            if (!duplicateResult.Passed)
            {
                allIssues.AddRange(duplicateResult.Issues);
            }

            // Check for invalid line numbers (negative or zero)
            var invalidLines = lineNumbers.Where(line => line <= 0).ToList();
            if (invalidLines.Any())
            {
                allIssues.AddRange(invalidLines.Select(line =>
                    $"Invalid line number {line} (line numbers must be positive)"));
            }
        }

        if (allIssues.Any())
        {
            return TestResult.Fail(
                "JSON Line Number Integrity",
                $"Found {allIssues.Count} line number integrity issue(s)",
                allIssues
            );
        }

        var summary = lineRanges.Any() || lineNumbers.Any()
            ? $"Validated {lineRanges.Count} range(s) and {lineNumbers.Count} line number(s)"
            : "No line numbers found in JSON (valid for content without line references)";

        return TestResult.Pass("JSON Line Number Integrity", summary);
    }

    /// <summary>
    /// Validates that the provided content is valid JSON.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the content is valid JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    public static TestResult ValidateJsonFormat(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return TestResult.Pass(
                "JSON Format Validation",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new JsonOutputParser();

        if (!parser.CanParse(content))
        {
            return TestResult.Fail(
                "JSON Format Validation",
                "Content is not valid JSON",
                new[] { "The provided content could not be parsed as JSON" }
            );
        }

        return TestResult.Pass(
            "JSON Format Validation",
            "Content is valid JSON"
        );
    }

    /// <summary>
    /// Performs comprehensive validation on JSON content.
    /// </summary>
    /// <param name="content">The JSON content to validate.</param>
    /// <returns>An enumerable of <see cref="TestResult"/> objects for each validation performed.</returns>
    /// <remarks>
    /// This method runs all available JSON validations:
    /// <list type="bullet">
    /// <item><description>Format validation</description></item>
    /// <item><description>Line number integrity</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    public static IEnumerable<TestResult> ValidateAll(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        yield return ValidateJsonFormat(content);
        yield return ValidateLineNumberIntegrity(content);
    }
}
