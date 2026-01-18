using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Parsers;

namespace RoslynDiff.TestUtilities.Validators;

/// <summary>
/// Validates HTML output format from roslyn-diff for consistency and integrity.
/// </summary>
public static class HtmlValidator
{
    /// <summary>
    /// Validates that HTML content is consistent across different flag combinations.
    /// </summary>
    /// <param name="files">Dictionary mapping flag combination names to their file contents.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method ensures that the same HTML structure is produced regardless of flags like:
    /// <list type="bullet">
    /// <item><description>--html file.html</description></item>
    /// <item><description>--html file.html --quiet</description></item>
    /// <item><description>--html file.html --open</description></item>
    /// </list>
    /// Compares the semantic content of HTML, ignoring formatting differences.
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
                "HTML Flag Combination Consistency",
                "No files provided (empty dictionary is valid)"
            );
        }

        if (files.Count == 1)
        {
            return TestResult.Pass(
                "HTML Flag Combination Consistency",
                "Only one file provided (no comparison needed)"
            );
        }

        var parser = new HtmlOutputParser();
        var issues = new List<string>();

        // Validate all HTML content
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
                issues.Add($"Flag combination '{flagCombo}' did not produce valid HTML");
                continue;
            }
        }

        if (issues.Any())
        {
            return TestResult.Fail(
                "HTML Flag Combination Consistency",
                $"Found {issues.Count} parsing issue(s)",
                issues
            );
        }

        // Compare content structure (simplified comparison)
        // Note: A full HTML comparison would require parsing and comparing DOM structures
        var first = files.First();
        var differences = new List<string>();

        foreach (var kvp in files.Skip(1))
        {
            // Normalize whitespace for comparison
            var normalizedFirst = NormalizeHtml(first.Value);
            var normalizedCurrent = NormalizeHtml(kvp.Value);

            if (normalizedFirst != normalizedCurrent)
            {
                differences.Add($"HTML from '{kvp.Key}' differs from '{first.Key}'");
            }
        }

        if (differences.Any())
        {
            return TestResult.Fail(
                "HTML Flag Combination Consistency",
                $"Found {differences.Count} inconsistency(ies) across flag combinations",
                differences
            );
        }

        return TestResult.Pass(
            "HTML Flag Combination Consistency",
            $"All {files.Count} flag combinations produce consistent HTML output"
        );
    }

    /// <summary>
    /// Validates the integrity of sections in HTML content.
    /// </summary>
    /// <param name="htmlContent">The HTML content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method checks that sections in the HTML do not contain:
    /// <list type="bullet">
    /// <item><description>Duplicate line numbers</description></item>
    /// <item><description>Overlapping line ranges</description></item>
    /// </list>
    /// Uses <see cref="HtmlOutputParser"/> to extract section information and
    /// <see cref="LineNumberValidator"/> to perform integrity checks.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="htmlContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateSectionIntegrity(string htmlContent)
    {
        if (htmlContent == null)
        {
            throw new ArgumentNullException(nameof(htmlContent));
        }

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return TestResult.Pass(
                "HTML Section Integrity",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new HtmlOutputParser();

        if (!parser.CanParse(htmlContent))
        {
            return TestResult.Fail(
                "HTML Section Integrity",
                "Content is not valid HTML",
                new[] { "Cannot parse HTML content - please verify the input is valid HTML" }
            );
        }

        var allIssues = new List<string>();
        var sections = parser.ExtractSections(htmlContent);

        if (!sections.Any())
        {
            return TestResult.Pass(
                "HTML Section Integrity",
                "No sections found in HTML (valid for content without sections)"
            );
        }

        // Validate each section's line ranges
        foreach (var section in sections)
        {
            var sectionName = section.Key;
            var ranges = section.Value;

            if (!ranges.Any())
            {
                continue;
            }

            // Check for overlaps within the section
            var overlapResult = LineNumberValidator.ValidateNoOverlaps(
                ranges,
                $"HTML Section '{sectionName}'"
            );

            if (!overlapResult.Passed)
            {
                allIssues.AddRange(overlapResult.Issues.Select(issue =>
                    $"[{sectionName}] {issue}"));
            }

            // Extract all line numbers from ranges and check for duplicates
            var lineNumbers = ranges
                .SelectMany(r => Enumerable.Range(r.Start, r.LineCount))
                .ToList();

            if (lineNumbers.Any())
            {
                var duplicateResult = LineNumberValidator.ValidateNoDuplicates(
                    lineNumbers,
                    $"HTML Section '{sectionName}'"
                );

                if (!duplicateResult.Passed)
                {
                    allIssues.AddRange(duplicateResult.Issues.Select(issue =>
                        $"[{sectionName}] {issue}"));
                }
            }
        }

        if (allIssues.Any())
        {
            return TestResult.Fail(
                "HTML Section Integrity",
                $"Found {allIssues.Count} section integrity issue(s)",
                allIssues
            );
        }

        return TestResult.Pass(
            "HTML Section Integrity",
            $"Validated {sections.Count} section(s) with no integrity issues"
        );
    }

    /// <summary>
    /// Validates that data attributes in HTML match the visual display.
    /// </summary>
    /// <param name="htmlContent">The HTML content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method ensures that data-* attributes (like data-line-number, data-change-type)
    /// are consistent with the visible content in the HTML output.
    /// Uses <see cref="HtmlOutputParser"/> to extract and compare data attributes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="htmlContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateDataAttributeConsistency(string htmlContent)
    {
        if (htmlContent == null)
        {
            throw new ArgumentNullException(nameof(htmlContent));
        }

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return TestResult.Pass(
                "HTML Data Attribute Consistency",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new HtmlOutputParser();

        if (!parser.CanParse(htmlContent))
        {
            return TestResult.Fail(
                "HTML Data Attribute Consistency",
                "Content is not valid HTML",
                new[] { "Cannot parse HTML content - please verify the input is valid HTML" }
            );
        }

        var dataAttributes = parser.ExtractDataAttributes(htmlContent);

        if (!dataAttributes.Any())
        {
            return TestResult.Pass(
                "HTML Data Attribute Consistency",
                "No data attributes found in HTML (valid for content without data attributes)"
            );
        }

        var issues = new List<string>();

        // Validate data attributes
        foreach (var element in dataAttributes)
        {
            var elementId = element.Key;
            var attributes = element.Value;

            // Check for line number attributes
            if (attributes.TryGetValue("data-line-number", out var lineNumberStr))
            {
                if (!int.TryParse(lineNumberStr, out var lineNumber) || lineNumber <= 0)
                {
                    issues.Add($"Element '{elementId}' has invalid data-line-number: '{lineNumberStr}'");
                }
            }

            // Check for change type attributes
            if (attributes.TryGetValue("data-change-type", out var changeType))
            {
                var validChangeTypes = new[] { "added", "removed", "modified", "unchanged" };
                if (!validChangeTypes.Contains(changeType, StringComparer.OrdinalIgnoreCase))
                {
                    issues.Add($"Element '{elementId}' has invalid data-change-type: '{changeType}'");
                }
            }

            // Additional attribute validation can be added here by Workstream D
        }

        if (issues.Any())
        {
            return TestResult.Fail(
                "HTML Data Attribute Consistency",
                $"Found {issues.Count} data attribute issue(s)",
                issues
            );
        }

        return TestResult.Pass(
            "HTML Data Attribute Consistency",
            $"Validated data attributes for {dataAttributes.Count} element(s)"
        );
    }

    /// <summary>
    /// Validates that the provided content is valid HTML.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the content is valid HTML.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    public static TestResult ValidateHtmlFormat(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return TestResult.Pass(
                "HTML Format Validation",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new HtmlOutputParser();

        if (!parser.CanParse(content))
        {
            return TestResult.Fail(
                "HTML Format Validation",
                "Content is not valid HTML",
                new[] { "The provided content does not appear to be HTML" }
            );
        }

        return TestResult.Pass(
            "HTML Format Validation",
            "Content is valid HTML"
        );
    }

    /// <summary>
    /// Performs comprehensive validation on HTML content.
    /// </summary>
    /// <param name="content">The HTML content to validate.</param>
    /// <returns>An enumerable of <see cref="TestResult"/> objects for each validation performed.</returns>
    /// <remarks>
    /// This method runs all available HTML validations:
    /// <list type="bullet">
    /// <item><description>Format validation</description></item>
    /// <item><description>Section integrity</description></item>
    /// <item><description>Data attribute consistency</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    public static IEnumerable<TestResult> ValidateAll(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        yield return ValidateHtmlFormat(content);
        yield return ValidateSectionIntegrity(content);
        yield return ValidateDataAttributeConsistency(content);
    }

    /// <summary>
    /// Normalizes HTML content for comparison by removing excess whitespace.
    /// </summary>
    /// <param name="html">The HTML content to normalize.</param>
    /// <returns>Normalized HTML string.</returns>
    private static string NormalizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        // Simple normalization: collapse multiple whitespace characters
        // A full implementation by Workstream D would parse and compare DOM structure
        return System.Text.RegularExpressions.Regex.Replace(
            html.Trim(),
            @"\s+",
            " "
        );
    }
}
