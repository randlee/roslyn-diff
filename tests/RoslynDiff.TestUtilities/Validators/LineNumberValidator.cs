using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Models;

namespace RoslynDiff.TestUtilities.Validators;

/// <summary>
/// Provides validation methods for line numbers and line ranges to ensure correctness in diff operations.
/// </summary>
public static class LineNumberValidator
{
    /// <summary>
    /// Validates that the provided line ranges do not overlap with each other.
    /// </summary>
    /// <param name="ranges">The collection of line ranges to validate.</param>
    /// <param name="context">A description of the context being validated (e.g., "Old file changes", "New file changes").</param>
    /// <returns>
    /// A <see cref="TestResult"/> indicating whether validation passed.
    /// If overlaps are found, the result will contain details about each overlapping pair.
    /// </returns>
    /// <remarks>
    /// Overlapping ranges indicate a problem in diff output where the same line numbers
    /// are referenced by multiple changes, which could lead to incorrect change application.
    /// Adjacent ranges (e.g., 1-5 and 6-10) are valid and not considered overlapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ranges"/> or <paramref name="context"/> is <c>null</c>.</exception>
    public static TestResult ValidateNoOverlaps(IEnumerable<LineRange> ranges, string context)
    {
        if (ranges == null)
        {
            throw new ArgumentNullException(nameof(ranges));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var overlaps = LineRangeComparer.DetectOverlaps(ranges);

        if (!overlaps.Any())
        {
            return TestResult.Pass(
                context,
                "No overlapping line ranges detected"
            );
        }

        var issues = overlaps
            .Select(overlap => $"{overlap.First} overlaps with {overlap.Second}")
            .ToList();

        return TestResult.Fail(
            context,
            $"Found {overlaps.Count} overlapping line range(s)",
            issues
        );
    }

    /// <summary>
    /// Validates that the provided line numbers do not contain duplicates.
    /// </summary>
    /// <param name="lineNumbers">The collection of line numbers to validate.</param>
    /// <param name="context">A description of the context being validated.</param>
    /// <returns>
    /// A <see cref="TestResult"/> indicating whether validation passed.
    /// If duplicates are found, the result will contain details about each duplicate line number.
    /// </returns>
    /// <remarks>
    /// Duplicate line numbers in diff output may indicate that the same line is being
    /// processed multiple times, which could lead to incorrect results.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="lineNumbers"/> or <paramref name="context"/> is <c>null</c>.</exception>
    public static TestResult ValidateNoDuplicates(IEnumerable<int> lineNumbers, string context)
    {
        if (lineNumbers == null)
        {
            throw new ArgumentNullException(nameof(lineNumbers));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var duplicates = LineRangeComparer.DetectDuplicates(lineNumbers);

        if (!duplicates.Any())
        {
            return TestResult.Pass(
                context,
                "No duplicate line numbers detected"
            );
        }

        var issues = duplicates
            .Select(line => $"Line {line} appears multiple times")
            .ToList();

        return TestResult.Fail(
            context,
            $"Found {duplicates.Count} duplicate line number(s)",
            issues
        );
    }

    /// <summary>
    /// Validates that the provided line ranges are sequential (non-overlapping and in ascending order).
    /// </summary>
    /// <param name="ranges">The collection of line ranges to validate.</param>
    /// <param name="context">A description of the context being validated.</param>
    /// <returns>
    /// A <see cref="TestResult"/> indicating whether validation passed.
    /// If ranges are not sequential, the result will contain details about the violations.
    /// </returns>
    /// <remarks>
    /// Sequential validation ensures that ranges appear in order and do not overlap.
    /// This is useful for validating that changes are organized logically in diff output.
    /// Adjacent ranges (e.g., 1-5 and 6-10) are considered valid and sequential.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ranges"/> or <paramref name="context"/> is <c>null</c>.</exception>
    public static TestResult ValidateRangesSequential(IEnumerable<LineRange> ranges, string context)
    {
        if (ranges == null)
        {
            throw new ArgumentNullException(nameof(ranges));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var rangeList = ranges.ToList();

        if (!rangeList.Any())
        {
            return TestResult.Pass(context, "No ranges to validate (empty collection is considered sequential)");
        }

        var issues = new List<string>();

        for (int i = 0; i < rangeList.Count - 1; i++)
        {
            var current = rangeList[i];
            var next = rangeList[i + 1];

            if (next.Start <= current.End)
            {
                issues.Add($"{current} is not before {next} (ranges must be sequential)");
            }
        }

        if (!issues.Any())
        {
            return TestResult.Pass(
                context,
                $"All {rangeList.Count} range(s) are sequential"
            );
        }

        return TestResult.Fail(
            context,
            $"Found {issues.Count} sequential ordering violation(s)",
            issues
        );
    }

    /// <summary>
    /// Performs comprehensive validation on a collection of line ranges.
    /// </summary>
    /// <param name="ranges">The collection of line ranges to validate.</param>
    /// <param name="context">A description of the context being validated.</param>
    /// <param name="requireSequential">
    /// If <c>true</c>, validates that ranges are sequential in addition to checking for overlaps.
    /// If <c>false</c>, only checks for overlaps.
    /// </param>
    /// <returns>
    /// A <see cref="TestResult"/> indicating whether all validations passed.
    /// If multiple validation failures occur, all issues are combined in the result.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ranges"/> or <paramref name="context"/> is <c>null</c>.</exception>
    public static TestResult ValidateRanges(
        IEnumerable<LineRange> ranges,
        string context,
        bool requireSequential = false)
    {
        if (ranges == null)
        {
            throw new ArgumentNullException(nameof(ranges));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var rangeList = ranges.ToList();
        var allIssues = new List<string>();

        // Check for overlaps
        var overlapResult = ValidateNoOverlaps(rangeList, context);
        if (!overlapResult.Passed)
        {
            allIssues.AddRange(overlapResult.Issues);
        }

        // Check sequential ordering if required
        if (requireSequential)
        {
            var sequentialResult = ValidateRangesSequential(rangeList, context);
            if (!sequentialResult.Passed)
            {
                allIssues.AddRange(sequentialResult.Issues);
            }
        }

        if (!allIssues.Any())
        {
            var checks = requireSequential ? "overlap and sequential" : "overlap";
            return TestResult.Pass(
                context,
                $"All {checks} validations passed for {rangeList.Count} range(s)"
            );
        }

        return TestResult.Fail(
            context,
            $"Found {allIssues.Count} validation issue(s)",
            allIssues
        );
    }
}
