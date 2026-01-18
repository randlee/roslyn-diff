using System.Text.RegularExpressions;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Models;

namespace RoslynDiff.TestUtilities.Parsers;

/// <summary>
/// Parses unified diff format (git diff) output.
/// Extracts hunk headers, line numbers, and tracks changes for both old and new versions.
/// </summary>
public class UnifiedDiffParser : ILineNumberParser
{
    // Regex pattern for hunk headers: @@ -10,5 +15,7 @@ optional context
    private static readonly Regex HunkHeaderPattern = new(@"^@@\s+-(\d+)(?:,(\d+))?\s+\+(\d+)(?:,(\d+))?\s+@@(?:\s+(.*))?$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex FileHeaderPattern = new(@"^(?:diff --git|---|\+\+\+)\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <inheritdoc/>
    public string FormatName => "UnifiedDiff";

    /// <inheritdoc/>
    public bool CanParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // Check for unified diff markers
        return content.Contains("@@") || content.Contains("diff --git");
    }

    /// <inheritdoc/>
    public IEnumerable<int> ExtractLineNumbers(string content)
    {
        var result = Parse(content);
        var lineNumbers = new HashSet<int>();

        foreach (var change in result.Changes)
        {
            AddLineNumbersFromChange(change, lineNumbers);
        }

        return lineNumbers.OrderBy(x => x);
    }

    /// <inheritdoc/>
    public IEnumerable<LineRange> ExtractLineRanges(string content)
    {
        var result = Parse(content);
        var ranges = new List<LineRange>();

        foreach (var change in result.Changes)
        {
            CollectLineRangesFromChange(change, ranges);
        }

        return ranges;
    }

    /// <summary>
    /// Parses unified diff output from a string.
    /// </summary>
    /// <param name="diffContent">The diff content to parse.</param>
    /// <returns>A <see cref="ParsedDiffResult"/> containing the extracted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diffContent"/> is null.</exception>
    public static ParsedDiffResult Parse(string diffContent)
    {
        if (diffContent == null)
        {
            throw new ArgumentNullException(nameof(diffContent));
        }

        if (string.IsNullOrWhiteSpace(diffContent))
        {
            return new ParsedDiffResult
            {
                Format = "unified-diff",
                ParsingErrors = new[] { "Input diff content is empty" }
            };
        }

        var changes = new List<ParsedChange>();
        var errors = new List<string>();
        var metadata = new Dictionary<string, string>();

        // Extract file paths
        string? oldPath = null;
        string? newPath = null;

        var fileMatches = FileHeaderPattern.Matches(diffContent);
        foreach (Match match in fileMatches)
        {
            var line = match.Value;
            if (line.StartsWith("---"))
            {
                oldPath = match.Groups[1].Value.Trim();
                if (oldPath.StartsWith("a/"))
                {
                    oldPath = oldPath.Substring(2);
                }
            }
            else if (line.StartsWith("+++"))
            {
                newPath = match.Groups[1].Value.Trim();
                if (newPath.StartsWith("b/"))
                {
                    newPath = newPath.Substring(2);
                }
            }
        }

        // Parse hunks
        var lines = diffContent.Split('\n');
        var hunkHeaders = ExtractHunkHeaders(diffContent);

        foreach (var hunk in hunkHeaders)
        {
            // Create changes for added and removed lines in this hunk
            var oldLineRange = new LineRange(hunk.OldStart, hunk.OldStart + hunk.OldCount - 1, "old");
            var newLineRange = new LineRange(hunk.NewStart, hunk.NewStart + hunk.NewCount - 1, "new");

            changes.Add(new ParsedChange
            {
                ChangeType = "hunk",
                Kind = "diff",
                Name = hunk.Context ?? $"Hunk at line {hunk.NewStart}",
                LineRange = newLineRange,
                OldLineRange = oldLineRange,
                Metadata = new Dictionary<string, string>
                {
                    ["hunk_header"] = hunk.ToString()
                }
            });
        }

        // Calculate summary
        var summary = new ParsedSummary
        {
            TotalChanges = changes.Count,
            Modifications = changes.Count
        };

        return new ParsedDiffResult
        {
            Format = "unified-diff",
            OldPath = oldPath,
            NewPath = newPath,
            Changes = changes,
            Summary = summary,
            Metadata = metadata,
            ParsingErrors = errors
        };
    }

    /// <summary>
    /// Parses unified diff output from a file.
    /// </summary>
    /// <param name="filePath">The path to the diff file.</param>
    /// <returns>A <see cref="ParsedDiffResult"/> containing the extracted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    public static ParsedDiffResult ParseFile(string filePath)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            return new ParsedDiffResult
            {
                Format = "unified-diff",
                ParsingErrors = new[] { $"File not found: {filePath}" }
            };
        }

        try
        {
            var diffContent = File.ReadAllText(filePath);
            return Parse(diffContent);
        }
        catch (Exception ex)
        {
            return new ParsedDiffResult
            {
                Format = "unified-diff",
                ParsingErrors = new[] { $"Failed to read file: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Extracts hunk headers from unified diff content.
    /// </summary>
    /// <param name="content">The diff content to parse.</param>
    /// <returns>A collection of hunk header information.</returns>
    public static List<HunkHeader> ExtractHunkHeaders(string content)
    {
        var headers = new List<HunkHeader>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return headers;
        }

        var matches = HunkHeaderPattern.Matches(content);
        foreach (Match match in matches)
        {
            var oldStart = int.Parse(match.Groups[1].Value);
            var oldCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
            var newStart = int.Parse(match.Groups[3].Value);
            var newCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1;
            var context = match.Groups[5].Success ? match.Groups[5].Value.Trim() : null;

            headers.Add(new HunkHeader(oldStart, oldCount, newStart, newCount, context));
        }

        return headers;
    }

    /// <summary>
    /// Validates that the content follows unified diff format rules.
    /// </summary>
    /// <param name="content">The diff content to validate.</param>
    /// <returns><c>true</c> if the format is valid; otherwise, <c>false</c>.</returns>
    public bool IsValidUnifiedDiffFormat(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // Check for required elements
        var hasHunkHeaders = HunkHeaderPattern.IsMatch(content);
        var hasFileHeaders = content.Contains("---") && content.Contains("+++");

        return hasHunkHeaders || hasFileHeaders;
    }

    /// <summary>
    /// Recursively adds line numbers from a change and its children.
    /// </summary>
    private static void AddLineNumbersFromChange(ParsedChange change, HashSet<int> lineNumbers)
    {
        if (change.LineRange != null)
        {
            for (int i = change.LineRange.Start; i <= change.LineRange.End; i++)
            {
                lineNumbers.Add(i);
            }
        }

        if (change.OldLineRange != null)
        {
            for (int i = change.OldLineRange.Start; i <= change.OldLineRange.End; i++)
            {
                lineNumbers.Add(i);
            }
        }

        foreach (var child in change.Children)
        {
            AddLineNumbersFromChange(child, lineNumbers);
        }
    }

    /// <summary>
    /// Recursively collects line ranges from a change and its children.
    /// </summary>
    /// <remarks>
    /// Only collects line ranges from non-removed, non-container items. Removed items have
    /// line numbers from the OLD file, not the NEW file. Container items (Namespace, Class,
    /// Interface, etc.) have line ranges that span their children, which would cause
    /// false overlap detection.
    /// Also skips parent nodes with children to avoid hierarchical parent-child overlaps.
    /// </remarks>
    private static void CollectLineRangesFromChange(ParsedChange change, List<LineRange> ranges)
    {
        // Skip removed items - their line numbers are from the old file context
        var isRemoved = change.ChangeType.Equals("removed", StringComparison.OrdinalIgnoreCase);

        // Skip container types whose line ranges span their children
        var isContainer = change.Kind != null &&
            (change.Kind.Equals("namespace", StringComparison.OrdinalIgnoreCase) ||
             change.Kind.Equals("class", StringComparison.OrdinalIgnoreCase) ||
             change.Kind.Equals("interface", StringComparison.OrdinalIgnoreCase) ||
             change.Kind.Equals("struct", StringComparison.OrdinalIgnoreCase) ||
             change.Kind.Equals("record", StringComparison.OrdinalIgnoreCase) ||
             change.Kind.Equals("enum", StringComparison.OrdinalIgnoreCase));

        // Only collect line ranges from leaf nodes (changes without children)
        // to avoid false overlap detection from hierarchical parent-child relationships.
        // Parent nodes naturally encompass their children's line ranges.
        if (change.Children.Count == 0)
        {
            // Only collect from non-removed, non-container items (new file context, actual changes)
            if (change.LineRange != null && !isRemoved && !isContainer)
            {
                ranges.Add(change.LineRange);
            }
        }
        else
        {
            // Recursively collect from children only
            foreach (var child in change.Children)
            {
                CollectLineRangesFromChange(child, ranges);
            }
        }
    }
}

/// <summary>
/// Represents a hunk header from a unified diff.
/// </summary>
/// <param name="OldStart">The starting line number in the old file.</param>
/// <param name="OldCount">The number of lines in the old file.</param>
/// <param name="NewStart">The starting line number in the new file.</param>
/// <param name="NewCount">The number of lines in the new file.</param>
/// <param name="Context">Optional context text from the hunk header.</param>
public record HunkHeader(int OldStart, int OldCount, int NewStart, int NewCount, string? Context = null)
{
    /// <summary>
    /// Returns a string representation of this hunk header.
    /// </summary>
    public override string ToString()
    {
        var header = $"@@ -{OldStart},{OldCount} +{NewStart},{NewCount} @@";
        return Context != null ? $"{header} {Context}" : header;
    }
}
