using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Parsers;

namespace RoslynDiff.TestUtilities.Validators;

/// <summary>
/// Validates text output format from roslyn-diff for consistency and integrity.
/// </summary>
public static class TextValidator
{
    /// <summary>
    /// Validates line references in text content.
    /// </summary>
    /// <param name="textContent">The text content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method extracts and validates line number references from text output, such as:
    /// <list type="bullet">
    /// <item><description>"line 10"</description></item>
    /// <item><description>"lines 5-8"</description></item>
    /// <item><description>"Line 42"</description></item>
    /// </list>
    /// Validates that:
    /// <list type="bullet">
    /// <item><description>Line numbers are positive integers</description></item>
    /// <item><description>Line ranges are valid (start &lt;= end)</description></item>
    /// <item><description>References are consistent and properly formatted</description></item>
    /// </list>
    /// Uses <see cref="TextOutputParser"/> to extract line references.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="textContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateLineReferences(string textContent)
    {
        if (textContent == null)
        {
            throw new ArgumentNullException(nameof(textContent));
        }

        if (string.IsNullOrWhiteSpace(textContent))
        {
            return TestResult.Pass(
                "Text Line References",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new TextOutputParser();
        var issues = new List<string>();

        var lineReferences = parser.ExtractLineReferences(textContent);

        if (!lineReferences.Any())
        {
            return TestResult.Pass(
                "Text Line References",
                "No line references found in text (valid for content without line references)"
            );
        }

        // Validate each line reference
        foreach (var reference in lineReferences)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                issues.Add("Found empty or whitespace-only line reference");
                continue;
            }

            // Check for range format (e.g., "5-8")
            if (reference.Contains('-'))
            {
                var parts = reference.Split('-');
                if (parts.Length != 2)
                {
                    issues.Add($"Invalid line range format: '{reference}' (expected format: 'start-end')");
                    continue;
                }

                if (!int.TryParse(parts[0].Trim(), out var start) || start <= 0)
                {
                    issues.Add($"Invalid start line in range '{reference}': must be a positive integer");
                }

                if (!int.TryParse(parts[1].Trim(), out var end) || end <= 0)
                {
                    issues.Add($"Invalid end line in range '{reference}': must be a positive integer");
                }
                else if (start > end)
                {
                    issues.Add($"Invalid line range '{reference}': start ({start}) must be <= end ({end})");
                }
            }
            // Check for single line number
            else
            {
                if (!int.TryParse(reference.Trim(), out var lineNumber) || lineNumber <= 0)
                {
                    issues.Add($"Invalid line number '{reference}': must be a positive integer");
                }
            }
        }

        if (issues.Any())
        {
            return TestResult.Fail(
                "Text Line References",
                $"Found {issues.Count} invalid line reference(s)",
                issues
            );
        }

        return TestResult.Pass(
            "Text Line References",
            $"Validated {lineReferences.Count} line reference(s)"
        );
    }

    /// <summary>
    /// Validates change type indicators in text content.
    /// </summary>
    /// <param name="textContent">The text content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method validates that change type indicators are consistent and properly formatted:
    /// <list type="bullet">
    /// <item><description>[+] for added lines</description></item>
    /// <item><description>[-] for removed lines</description></item>
    /// <item><description>[M] for modified lines</description></item>
    /// </list>
    /// Validates that:
    /// <list type="bullet">
    /// <item><description>Indicators are properly formatted</description></item>
    /// <item><description>Line numbers associated with indicators are valid</description></item>
    /// <item><description>No duplicate indicators for the same line</description></item>
    /// </list>
    /// Uses <see cref="TextOutputParser"/> to extract change type information.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="textContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateChangeTypeIndicators(string textContent)
    {
        if (textContent == null)
        {
            throw new ArgumentNullException(nameof(textContent));
        }

        if (string.IsNullOrWhiteSpace(textContent))
        {
            return TestResult.Pass(
                "Text Change Type Indicators",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new TextOutputParser();
        var issues = new List<string>();

        var changeIndicators = parser.ExtractChangeTypeIndicators(textContent);

        if (!changeIndicators.Any())
        {
            return TestResult.Pass(
                "Text Change Type Indicators",
                "No change type indicators found in text (valid for content without change markers)"
            );
        }

        var validIndicators = new[] { "[+]", "[-]", "[M]", "[+] ", "[-] ", "[M] " };
        var lineNumberCounts = new Dictionary<int, int>();

        // Validate each change type indicator
        foreach (var kvp in changeIndicators)
        {
            var lineNumber = kvp.Key;
            var indicator = kvp.Value;

            // Validate line number
            if (lineNumber <= 0)
            {
                issues.Add($"Invalid line number {lineNumber} for change indicator '{indicator}'");
            }

            // Count occurrences per line
            if (!lineNumberCounts.ContainsKey(lineNumber))
            {
                lineNumberCounts[lineNumber] = 0;
            }
            lineNumberCounts[lineNumber]++;

            // Validate indicator format
            if (!validIndicators.Any(valid => indicator.Contains(valid, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add($"Invalid change type indicator '{indicator}' at line {lineNumber} (expected [+], [-], or [M])");
            }
        }

        // Check for duplicate indicators on the same line
        var duplicates = lineNumberCounts.Where(kvp => kvp.Value > 1).ToList();
        if (duplicates.Any())
        {
            issues.AddRange(duplicates.Select(dup =>
                $"Line {dup.Key} has {dup.Value} change indicators (should have at most 1)"));
        }

        if (issues.Any())
        {
            return TestResult.Fail(
                "Text Change Type Indicators",
                $"Found {issues.Count} change type indicator issue(s)",
                issues
            );
        }

        return TestResult.Pass(
            "Text Change Type Indicators",
            $"Validated {changeIndicators.Count} change type indicator(s)"
        );
    }

    /// <summary>
    /// Validates that the provided content is valid text output.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the content is valid text.</returns>
    /// <remarks>
    /// Text format is the most permissive format and primarily checks for:
    /// <list type="bullet">
    /// <item><description>Non-null content</description></item>
    /// <item><description>Readable character encoding</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    public static TestResult ValidateTextFormat(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return TestResult.Pass(
                "Text Format Validation",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new TextOutputParser();

        if (!parser.CanParse(content))
        {
            return TestResult.Fail(
                "Text Format Validation",
                "Content is not valid text",
                new[] { "The provided content could not be parsed as text" }
            );
        }

        return TestResult.Pass(
            "Text Format Validation",
            "Content is valid text"
        );
    }

    /// <summary>
    /// Validates line number integrity in text content.
    /// </summary>
    /// <param name="textContent">The text content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method extracts line numbers and ranges from text content and validates:
    /// <list type="bullet">
    /// <item><description>No overlapping ranges</description></item>
    /// <item><description>No duplicate line numbers</description></item>
    /// <item><description>All line numbers are positive</description></item>
    /// </list>
    /// Uses <see cref="TextOutputParser"/> and <see cref="LineNumberValidator"/> for validation.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="textContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateLineNumberIntegrity(string textContent)
    {
        if (textContent == null)
        {
            throw new ArgumentNullException(nameof(textContent));
        }

        if (string.IsNullOrWhiteSpace(textContent))
        {
            return TestResult.Pass(
                "Text Line Number Integrity",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new TextOutputParser();
        var allIssues = new List<string>();

        // Extract and validate line ranges
        var lineRanges = parser.ExtractLineRanges(textContent).ToList();
        if (lineRanges.Any())
        {
            var rangeResult = LineNumberValidator.ValidateNoOverlaps(
                lineRanges,
                "Text Line Ranges"
            );

            if (!rangeResult.Passed)
            {
                allIssues.AddRange(rangeResult.Issues);
            }
        }

        // Extract and validate individual line numbers
        var lineNumbers = parser.ExtractLineNumbers(textContent).ToList();
        if (lineNumbers.Any())
        {
            // Check for duplicates
            var duplicateResult = LineNumberValidator.ValidateNoDuplicates(
                lineNumbers,
                "Text Line Numbers"
            );

            if (!duplicateResult.Passed)
            {
                allIssues.AddRange(duplicateResult.Issues);
            }

            // Check for invalid line numbers
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
                "Text Line Number Integrity",
                $"Found {allIssues.Count} line number integrity issue(s)",
                allIssues
            );
        }

        var summary = lineRanges.Any() || lineNumbers.Any()
            ? $"Validated {lineRanges.Count} range(s) and {lineNumbers.Count} line number(s)"
            : "No line numbers found in text (valid for content without line references)";

        return TestResult.Pass("Text Line Number Integrity", summary);
    }

    /// <summary>
    /// Performs comprehensive validation on text content.
    /// </summary>
    /// <param name="content">The text content to validate.</param>
    /// <returns>An enumerable of <see cref="TestResult"/> objects for each validation performed.</returns>
    /// <remarks>
    /// This method runs all available text validations:
    /// <list type="bullet">
    /// <item><description>Format validation</description></item>
    /// <item><description>Line reference validation</description></item>
    /// <item><description>Change type indicator validation</description></item>
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

        yield return ValidateTextFormat(content);
        yield return ValidateLineReferences(content);
        yield return ValidateChangeTypeIndicators(content);
        yield return ValidateLineNumberIntegrity(content);
    }
}
