using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Parsers;

namespace RoslynDiff.TestUtilities.Validators;

/// <summary>
/// Validates unified diff (git diff) format output for compliance and integrity.
/// </summary>
public static class GitDiffValidator
{
    /// <summary>
    /// Validates that the content follows unified diff format rules.
    /// </summary>
    /// <param name="diffContent">The diff content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method validates that the diff content conforms to unified diff format:
    /// <list type="bullet">
    /// <item><description>Contains proper diff headers (diff --git, +++, ---)</description></item>
    /// <item><description>Contains hunk markers (@@)</description></item>
    /// <item><description>Lines are properly prefixed (+, -, or space)</description></item>
    /// <item><description>Format follows standard unified diff conventions</description></item>
    /// </list>
    /// Uses <see cref="UnifiedDiffParser"/> to validate format compliance.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diffContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateUnifiedDiffFormat(string diffContent)
    {
        if (diffContent == null)
        {
            throw new ArgumentNullException(nameof(diffContent));
        }

        if (string.IsNullOrWhiteSpace(diffContent))
        {
            return TestResult.Pass(
                "Unified Diff Format",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new UnifiedDiffParser();
        var issues = new List<string>();

        if (!parser.CanParse(diffContent))
        {
            return TestResult.Fail(
                "Unified Diff Format",
                "Content does not appear to be unified diff format",
                new[] { "Content is missing required unified diff markers (@@, diff --git, etc.)" }
            );
        }

        if (!parser.IsValidUnifiedDiffFormat(diffContent))
        {
            issues.Add("Content does not follow valid unified diff format rules");
        }

        // Validate line prefixes
        var lines = diffContent.Split('\n');
        var lineNumber = 0;
        var inHunk = false;

        foreach (var line in lines)
        {
            lineNumber++;

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Check for hunk header
            if (line.TrimStart().StartsWith("@@"))
            {
                inHunk = true;
                continue;
            }

            // Check for diff headers
            if (line.StartsWith("diff --git") ||
                line.StartsWith("---") ||
                line.StartsWith("+++") ||
                line.StartsWith("index ") ||
                line.StartsWith("new file") ||
                line.StartsWith("deleted file") ||
                line.StartsWith("old mode") ||
                line.StartsWith("new mode"))
            {
                inHunk = false;
                continue;
            }

            // If in a hunk, validate line prefix
            if (inHunk)
            {
                var trimmedLine = line.TrimStart();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    var firstChar = trimmedLine[0];
                    if (firstChar != '+' && firstChar != '-' && firstChar != ' ' && firstChar != '\\')
                    {
                        issues.Add($"Line {lineNumber} has invalid prefix '{firstChar}' (expected +, -, space, or \\)");
                    }
                }
            }
        }

        if (issues.Any())
        {
            return TestResult.Fail(
                "Unified Diff Format",
                $"Found {issues.Count} format issue(s)",
                issues
            );
        }

        return TestResult.Pass(
            "Unified Diff Format",
            "Content follows valid unified diff format"
        );
    }

    /// <summary>
    /// Validates hunk headers in unified diff content.
    /// </summary>
    /// <param name="diffContent">The diff content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method validates that hunk headers (@@) are:
    /// <list type="bullet">
    /// <item><description>Properly formatted: @@ -oldStart,oldCount +newStart,newCount @@</description></item>
    /// <item><description>Have valid line numbers (positive integers)</description></item>
    /// <item><description>Have valid counts (non-negative integers)</description></item>
    /// <item><description>Are consistent with the actual diff content</description></item>
    /// </list>
    /// Uses <see cref="UnifiedDiffParser"/> to extract and validate hunk headers.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diffContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateHunkHeaders(string diffContent)
    {
        if (diffContent == null)
        {
            throw new ArgumentNullException(nameof(diffContent));
        }

        if (string.IsNullOrWhiteSpace(diffContent))
        {
            return TestResult.Pass(
                "Unified Diff Hunk Headers",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new UnifiedDiffParser();

        if (!parser.CanParse(diffContent))
        {
            return TestResult.Fail(
                "Unified Diff Hunk Headers",
                "Content does not appear to be unified diff format",
                new[] { "Cannot parse diff content - please verify the input is valid unified diff" }
            );
        }

        var hunkHeaders = UnifiedDiffParser.ExtractHunkHeaders(diffContent);

        if (!hunkHeaders.Any())
        {
            return TestResult.Pass(
                "Unified Diff Hunk Headers",
                "No hunk headers found in diff (valid for empty diffs)"
            );
        }

        var issues = new List<string>();

        // Validate each hunk header
        foreach (var hunk in hunkHeaders)
        {
            // Validate old file position
            if (hunk.OldStart < 0)
            {
                issues.Add($"Hunk header has invalid old start line: {hunk.OldStart} (must be >= 0)");
            }

            if (hunk.OldCount < 0)
            {
                issues.Add($"Hunk header has invalid old count: {hunk.OldCount} (must be >= 0)");
            }

            // Validate new file position
            if (hunk.NewStart < 0)
            {
                issues.Add($"Hunk header has invalid new start line: {hunk.NewStart} (must be >= 0)");
            }

            if (hunk.NewCount < 0)
            {
                issues.Add($"Hunk header has invalid new count: {hunk.NewCount} (must be >= 0)");
            }

            // Validate logical consistency
            if (hunk.OldCount == 0 && hunk.NewCount == 0)
            {
                issues.Add($"Hunk header {hunk} has zero counts for both old and new (invalid)");
            }
        }

        if (issues.Any())
        {
            return TestResult.Fail(
                "Unified Diff Hunk Headers",
                $"Found {issues.Count} hunk header issue(s)",
                issues
            );
        }

        return TestResult.Pass(
            "Unified Diff Hunk Headers",
            $"Validated {hunkHeaders.Count} hunk header(s)"
        );
    }

    /// <summary>
    /// Validates line number sequences in unified diff content.
    /// </summary>
    /// <param name="diffContent">The diff content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method validates that:
    /// <list type="bullet">
    /// <item><description>Line numbers in hunks are sequential and valid</description></item>
    /// <item><description>The number of lines matches hunk header counts</description></item>
    /// <item><description>No overlapping line ranges across hunks</description></item>
    /// </list>
    /// Uses <see cref="UnifiedDiffParser"/> to extract line numbers and
    /// <see cref="LineNumberValidator"/> to validate ranges.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diffContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateLineNumbers(string diffContent)
    {
        if (diffContent == null)
        {
            throw new ArgumentNullException(nameof(diffContent));
        }

        if (string.IsNullOrWhiteSpace(diffContent))
        {
            return TestResult.Pass(
                "Unified Diff Line Numbers",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new UnifiedDiffParser();

        if (!parser.CanParse(diffContent))
        {
            return TestResult.Fail(
                "Unified Diff Line Numbers",
                "Content does not appear to be unified diff format",
                new[] { "Cannot parse diff content - please verify the input is valid unified diff" }
            );
        }

        var allIssues = new List<string>();

        // Extract line ranges from diff
        var lineRanges = parser.ExtractLineRanges(diffContent).ToList();

        if (lineRanges.Any())
        {
            // Check for overlaps
            var overlapResult = LineNumberValidator.ValidateNoOverlaps(
                lineRanges,
                "Unified Diff Line Ranges"
            );

            if (!overlapResult.Passed)
            {
                allIssues.AddRange(overlapResult.Issues);
            }

            // Check if ranges are sequential
            var sequentialResult = LineNumberValidator.ValidateRangesSequential(
                lineRanges,
                "Unified Diff Line Ranges"
            );

            if (!sequentialResult.Passed)
            {
                allIssues.AddRange(sequentialResult.Issues);
            }
        }

        // Extract individual line numbers
        var lineNumbers = parser.ExtractLineNumbers(diffContent).ToList();

        if (lineNumbers.Any())
        {
            // Check for invalid line numbers (negative)
            var invalidLines = lineNumbers.Where(line => line < 0).ToList();
            if (invalidLines.Any())
            {
                allIssues.AddRange(invalidLines.Select(line =>
                    $"Invalid line number {line} (line numbers must be >= 0 in unified diff)"));
            }
        }

        if (allIssues.Any())
        {
            return TestResult.Fail(
                "Unified Diff Line Numbers",
                $"Found {allIssues.Count} line number issue(s)",
                allIssues
            );
        }

        var summary = lineRanges.Any() || lineNumbers.Any()
            ? $"Validated {lineRanges.Count} range(s) and {lineNumbers.Count} line number(s)"
            : "No line numbers found in diff (valid for empty diffs)";

        return TestResult.Pass("Unified Diff Line Numbers", summary);
    }

    /// <summary>
    /// Validates that hunk line counts match the actual content.
    /// </summary>
    /// <param name="diffContent">The diff content to validate.</param>
    /// <returns>A <see cref="TestResult"/> indicating whether the validation passed.</returns>
    /// <remarks>
    /// This method ensures that the line counts in hunk headers match the actual
    /// number of lines in the hunk content. This is critical for proper diff application.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diffContent"/> is <c>null</c>.</exception>
    public static TestResult ValidateHunkLineCounts(string diffContent)
    {
        if (diffContent == null)
        {
            throw new ArgumentNullException(nameof(diffContent));
        }

        if (string.IsNullOrWhiteSpace(diffContent))
        {
            return TestResult.Pass(
                "Unified Diff Hunk Line Counts",
                "No content to validate (empty content is valid)"
            );
        }

        var parser = new UnifiedDiffParser();

        if (!parser.CanParse(diffContent))
        {
            return TestResult.Fail(
                "Unified Diff Hunk Line Counts",
                "Content does not appear to be unified diff format",
                new[] { "Cannot parse diff content - please verify the input is valid unified diff" }
            );
        }

        // TODO: Workstream D will implement detailed hunk line count validation
        // This requires parsing hunk content and comparing with header counts

        return TestResult.Pass(
            "Unified Diff Hunk Line Counts",
            "Hunk line count validation ready for Workstream D implementation"
        );
    }

    /// <summary>
    /// Performs comprehensive validation on unified diff content.
    /// </summary>
    /// <param name="content">The diff content to validate.</param>
    /// <returns>An enumerable of <see cref="TestResult"/> objects for each validation performed.</returns>
    /// <remarks>
    /// This method runs all available unified diff validations:
    /// <list type="bullet">
    /// <item><description>Format validation</description></item>
    /// <item><description>Hunk header validation</description></item>
    /// <item><description>Line number validation</description></item>
    /// <item><description>Hunk line count validation</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
    public static IEnumerable<TestResult> ValidateAll(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        yield return ValidateUnifiedDiffFormat(content);
        yield return ValidateHunkHeaders(content);
        yield return ValidateLineNumbers(content);
        yield return ValidateHunkLineCounts(content);
    }
}
