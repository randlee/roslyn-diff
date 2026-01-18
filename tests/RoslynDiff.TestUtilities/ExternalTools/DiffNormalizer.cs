using System.Text;
using System.Text.RegularExpressions;
using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Parsers;

namespace RoslynDiff.TestUtilities.ExternalTools;

/// <summary>
/// Provides normalization and comparison utilities for unified diff outputs.
/// Handles differences in whitespace, timestamps, and hunk header formats.
/// </summary>
public static class DiffNormalizer
{
    private static readonly Regex TimestampPattern = new(
        @"\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:\s+[+-]\d{4})?",
        RegexOptions.Compiled);

    private static readonly Regex FileTimestampPattern = new(
        @"(---|\+\+\+)\s+(.+?)\s+\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}",
        RegexOptions.Compiled);

    private static readonly Regex TimeOnlyPattern = new(
        @"\s+\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:\s+[+-]\d{4})?",
        RegexOptions.Compiled);

    private static readonly Regex HunkHeaderPattern = new(
        @"@@\s+-(\d+)(?:,(\d+))?\s+\+(\d+)(?:,(\d+))?\s+@@",
        RegexOptions.Compiled);

    /// <summary>
    /// Normalizes whitespace in diff output.
    /// Converts CRLF to LF and normalizes line endings.
    /// </summary>
    /// <param name="diffOutput">The diff output to normalize.</param>
    /// <returns>The normalized diff output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when diffOutput is null.</exception>
    public static string NormalizeWhitespace(string diffOutput)
    {
        if (diffOutput == null) throw new ArgumentNullException(nameof(diffOutput));

        // Convert CRLF to LF
        var normalized = diffOutput.Replace("\r\n", "\n");

        // Remove trailing whitespace from each line
        var lines = normalized.Split('\n');
        var normalizedLines = lines.Select(line => line.TrimEnd()).ToList();

        // Remove trailing empty lines
        while (normalizedLines.Count > 0 && string.IsNullOrWhiteSpace(normalizedLines[^1]))
        {
            normalizedLines.RemoveAt(normalizedLines.Count - 1);
        }

        return string.Join("\n", normalizedLines);
    }

    /// <summary>
    /// Normalizes hunk headers to ensure consistent format.
    /// Ensures all hunk headers include line counts even when they're 1.
    /// </summary>
    /// <param name="diffOutput">The diff output to normalize.</param>
    /// <returns>The diff output with normalized hunk headers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when diffOutput is null.</exception>
    public static string NormalizeHunkHeaders(string diffOutput)
    {
        if (diffOutput == null) throw new ArgumentNullException(nameof(diffOutput));

        return HunkHeaderPattern.Replace(diffOutput, match =>
        {
            var oldStart = match.Groups[1].Value;
            var oldCount = match.Groups[2].Success ? match.Groups[2].Value : "1";
            var newStart = match.Groups[3].Value;
            var newCount = match.Groups[4].Success ? match.Groups[4].Value : "1";

            return $"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@";
        });
    }

    /// <summary>
    /// Strips file modification timestamps from diff output.
    /// Removes timestamps like "2024-01-17 10:30:45" from file headers.
    /// </summary>
    /// <param name="diffOutput">The diff output to normalize.</param>
    /// <returns>The diff output without timestamps.</returns>
    /// <exception cref="ArgumentNullException">Thrown when diffOutput is null.</exception>
    public static string StripFileTimestamps(string diffOutput)
    {
        if (diffOutput == null) throw new ArgumentNullException(nameof(diffOutput));

        // Remove full date-time timestamps from file headers (--- and +++ lines)
        var result = FileTimestampPattern.Replace(diffOutput, "$1 $2");

        // Remove any remaining date-time timestamps
        result = TimestampPattern.Replace(result, "");

        // Remove any remaining time-only patterns
        result = TimeOnlyPattern.Replace(result, "");

        return result;
    }

    /// <summary>
    /// Applies all normalizations to diff output.
    /// </summary>
    /// <param name="diffOutput">The diff output to normalize.</param>
    /// <returns>The fully normalized diff output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when diffOutput is null.</exception>
    public static string NormalizeAll(string diffOutput)
    {
        if (diffOutput == null) throw new ArgumentNullException(nameof(diffOutput));

        var normalized = diffOutput;
        normalized = NormalizeWhitespace(normalized);
        normalized = StripFileTimestamps(normalized);
        normalized = NormalizeHunkHeaders(normalized);

        return normalized;
    }

    /// <summary>
    /// Compares two unified diff outputs for semantic equivalence.
    /// Normalizes both outputs before comparison.
    /// </summary>
    /// <param name="diff1">The first diff output.</param>
    /// <param name="diff2">The second diff output.</param>
    /// <param name="ignoreFileNames">Whether to ignore differences in file names.</param>
    /// <returns>A TestResult indicating whether the diffs are equivalent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either diff is null.</exception>
    public static TestResult CompareUnifiedDiffs(string diff1, string diff2, bool ignoreFileNames = false)
    {
        if (diff1 == null) throw new ArgumentNullException(nameof(diff1));
        if (diff2 == null) throw new ArgumentNullException(nameof(diff2));

        try
        {
            // Normalize both diffs
            var normalized1 = NormalizeAll(diff1);
            var normalized2 = NormalizeAll(diff2);

            // If ignoring file names, strip --- and +++ lines
            if (ignoreFileNames)
            {
                normalized1 = StripFileHeaders(normalized1);
                normalized2 = StripFileHeaders(normalized2);
            }

            // Compare normalized outputs
            if (normalized1 == normalized2)
            {
                return TestResult.Pass("Unified diff comparison", "Diffs are semantically equivalent");
            }

            // Try to identify specific differences
            var issues = IdentifyDifferences(normalized1, normalized2);

            return TestResult.Fail(
                "Unified diff comparison",
                "Diffs are not semantically equivalent",
                issues);
        }
        catch (Exception ex)
        {
            return TestResult.Fail(
                "Unified diff comparison",
                $"Error comparing diffs: {ex.Message}");
        }
    }

    /// <summary>
    /// Compares two parsed diff results for semantic equivalence.
    /// </summary>
    /// <param name="result1">The first parsed diff result.</param>
    /// <param name="result2">The second parsed diff result.</param>
    /// <returns>A TestResult indicating whether the parsed results are equivalent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either result is null.</exception>
    public static TestResult CompareParsedResults(ParsedDiffResult result1, ParsedDiffResult result2)
    {
        if (result1 == null) throw new ArgumentNullException(nameof(result1));
        if (result2 == null) throw new ArgumentNullException(nameof(result2));

        var issues = new List<string>();

        // Compare change counts
        if (result1.Changes.Count != result2.Changes.Count)
        {
            issues.Add($"Change count mismatch: {result1.Changes.Count} vs {result2.Changes.Count}");
        }

        // Compare each change
        for (int i = 0; i < Math.Min(result1.Changes.Count, result2.Changes.Count); i++)
        {
            var change1 = result1.Changes[i];
            var change2 = result2.Changes[i];

            if (change1.LineRange?.Start != change2.LineRange?.Start ||
                change1.LineRange?.End != change2.LineRange?.End)
            {
                issues.Add($"Change {i}: Line range mismatch - " +
                          $"({change1.LineRange?.Start}-{change1.LineRange?.End}) vs " +
                          $"({change2.LineRange?.Start}-{change2.LineRange?.End})");
            }

            if (change1.OldLineRange?.Start != change2.OldLineRange?.Start ||
                change1.OldLineRange?.End != change2.OldLineRange?.End)
            {
                issues.Add($"Change {i}: Old line range mismatch - " +
                          $"({change1.OldLineRange?.Start}-{change1.OldLineRange?.End}) vs " +
                          $"({change2.OldLineRange?.Start}-{change2.OldLineRange?.End})");
            }
        }

        if (issues.Any())
        {
            return TestResult.Fail("Parsed diff comparison", "Parsed results differ", issues);
        }

        return TestResult.Pass("Parsed diff comparison", "Parsed results are equivalent");
    }

    /// <summary>
    /// Strips file header lines (--- and +++) from diff output.
    /// </summary>
    private static string StripFileHeaders(string diffOutput)
    {
        var lines = diffOutput.Split('\n');
        var filteredLines = lines.Where(line =>
            !line.StartsWith("---") &&
            !line.StartsWith("+++") &&
            !line.StartsWith("diff --git")).ToList();

        return string.Join("\n", filteredLines);
    }

    /// <summary>
    /// Identifies specific differences between two normalized diffs.
    /// </summary>
    private static List<string> IdentifyDifferences(string diff1, string diff2)
    {
        var issues = new List<string>();

        var lines1 = diff1.Split('\n');
        var lines2 = diff2.Split('\n');

        if (lines1.Length != lines2.Length)
        {
            issues.Add($"Line count mismatch: {lines1.Length} vs {lines2.Length}");
        }

        var maxLines = Math.Min(lines1.Length, lines2.Length);
        var differenceCount = 0;

        for (int i = 0; i < maxLines && differenceCount < 5; i++)
        {
            if (lines1[i] != lines2[i])
            {
                issues.Add($"Line {i + 1} differs:");
                issues.Add($"  Diff 1: {TruncateLine(lines1[i])}");
                issues.Add($"  Diff 2: {TruncateLine(lines2[i])}");
                differenceCount++;
            }
        }

        if (differenceCount >= 5)
        {
            issues.Add("... (more differences exist)");
        }

        return issues;
    }

    /// <summary>
    /// Truncates a line for display purposes.
    /// </summary>
    private static string TruncateLine(string line, int maxLength = 100)
    {
        if (line.Length <= maxLength)
        {
            return line;
        }

        return line.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Extracts the content lines (excluding headers and metadata) from a diff.
    /// </summary>
    /// <param name="diffOutput">The diff output to extract from.</param>
    /// <returns>Only the content lines (those starting with +, -, or space).</returns>
    public static string ExtractContentLines(string diffOutput)
    {
        if (diffOutput == null) throw new ArgumentNullException(nameof(diffOutput));

        var lines = diffOutput.Split('\n');
        var contentLines = lines.Where(line =>
        {
            if (string.IsNullOrEmpty(line)) return false;
            var firstChar = line[0];
            // Only include lines that start with +, -, or space (context lines)
            // Exclude --- and +++ file headers and @@ hunk headers
            if (firstChar == '-' && line.StartsWith("---")) return false;
            if (firstChar == '+' && line.StartsWith("+++")) return false;
            if (firstChar == '@' && line.StartsWith("@@")) return false;
            return firstChar == '+' || firstChar == '-' || firstChar == ' ';
        }).ToList();

        return string.Join("\n", contentLines);
    }
}
