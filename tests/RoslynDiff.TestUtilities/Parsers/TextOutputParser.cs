using System.Text.RegularExpressions;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Models;

namespace RoslynDiff.TestUtilities.Parsers;

/// <summary>
/// Parses plain text output format from roslyn-diff.
/// Extracts line references, change type indicators, and symbol names.
/// </summary>
public class TextOutputParser : ILineNumberParser
{
    // Regex patterns for parsing text output
    private static readonly Regex LineReferencePattern = new(@"\(line (\d+)(?:-(\d+))?\)", RegexOptions.Compiled);
    private static readonly Regex LineReferencePattern2 = new(@"line (\d+)(?:-(\d+))?", RegexOptions.Compiled);
    private static readonly Regex ChangeLinePattern = new(@"^\s*(\[[\+\-M]\])\s+(\w+):\s+(.+?)(?:\s+\(line (\d+)(?:-(\d+))?\))?$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <inheritdoc/>
    public string FormatName => "Text";

    /// <inheritdoc/>
    public bool CanParse(string content)
    {
        // Text format is the default and most permissive
        return !string.IsNullOrWhiteSpace(content);
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
    /// Parses plain text output from a string.
    /// </summary>
    /// <param name="textContent">The text content to parse.</param>
    /// <returns>A <see cref="ParsedDiffResult"/> containing the extracted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="textContent"/> is null.</exception>
    public static ParsedDiffResult Parse(string textContent)
    {
        if (textContent == null)
        {
            throw new ArgumentNullException(nameof(textContent));
        }

        if (string.IsNullOrWhiteSpace(textContent))
        {
            return new ParsedDiffResult
            {
                Format = "text",
                ParsingErrors = new[] { "Input text content is empty" }
            };
        }

        var changes = new List<ParsedChange>();
        var errors = new List<string>();
        var metadata = new Dictionary<string, string>();

        // Parse header lines for metadata
        var lines = textContent.Split('\n');
        string? oldPath = null;
        string? newPath = null;
        string? mode = null;

        foreach (var line in lines.Take(10)) // Check first 10 lines for header info
        {
            if (line.StartsWith("Diff:", StringComparison.OrdinalIgnoreCase))
            {
                var pathMatch = Regex.Match(line, @"Diff:\s*(.+?)\s*->\s*(.+)");
                if (pathMatch.Success)
                {
                    oldPath = pathMatch.Groups[1].Value.Trim();
                    newPath = pathMatch.Groups[2].Value.Trim();
                }
            }
            else if (line.StartsWith("Mode:", StringComparison.OrdinalIgnoreCase))
            {
                mode = line.Substring(5).Trim();
            }
        }

        // Parse change lines (e.g., "[+] Method: Multiply (line 5-8)")
        var matches = ChangeLinePattern.Matches(textContent);
        foreach (Match match in matches)
        {
            var changeIndicator = match.Groups[1].Value;
            var kind = match.Groups[2].Value;
            var name = match.Groups[3].Value.Trim();

            string changeType = changeIndicator switch
            {
                "[+]" => "added",
                "[-]" => "removed",
                "[M]" => "modified",
                _ => "unknown"
            };

            LineRange? lineRange = null;
            if (match.Groups[4].Success)
            {
                var startLine = int.Parse(match.Groups[4].Value);
                var endLine = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : startLine;
                lineRange = new LineRange(startLine, endLine);
            }

            changes.Add(new ParsedChange
            {
                ChangeType = changeType,
                Kind = kind,
                Name = name,
                LineRange = lineRange
            });
        }

        // Calculate summary
        var summary = new ParsedSummary
        {
            TotalChanges = changes.Count,
            Additions = changes.Count(c => c.ChangeType == "added"),
            Deletions = changes.Count(c => c.ChangeType == "removed"),
            Modifications = changes.Count(c => c.ChangeType == "modified")
        };

        return new ParsedDiffResult
        {
            Format = "text",
            OldPath = oldPath,
            NewPath = newPath,
            Mode = mode,
            Changes = changes,
            Summary = summary,
            Metadata = metadata,
            ParsingErrors = errors
        };
    }

    /// <summary>
    /// Parses plain text output from a file.
    /// </summary>
    /// <param name="filePath">The path to the text file.</param>
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
                Format = "text",
                ParsingErrors = new[] { $"File not found: {filePath}" }
            };
        }

        try
        {
            var textContent = File.ReadAllText(filePath);
            return Parse(textContent);
        }
        catch (Exception ex)
        {
            return new ParsedDiffResult
            {
                Format = "text",
                ParsingErrors = new[] { $"Failed to read file: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Extracts change type indicators from text content.
    /// </summary>
    /// <param name="content">The text content to parse.</param>
    /// <returns>A dictionary mapping line numbers to their change type indicators ([+], [-], [M]).</returns>
    public Dictionary<int, string> ExtractChangeTypeIndicators(string content)
    {
        var result = new Dictionary<int, string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        var matches = ChangeLinePattern.Matches(content);
        foreach (Match match in matches)
        {
            var indicator = match.Groups[1].Value;
            if (match.Groups[4].Success && int.TryParse(match.Groups[4].Value, out var startLine))
            {
                result[startLine] = indicator;
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts line references from text content (e.g., "line 10", "lines 5-8").
    /// </summary>
    /// <param name="content">The text content to parse.</param>
    /// <returns>A collection of referenced line numbers or ranges.</returns>
    public List<string> ExtractLineReferences(string content)
    {
        var references = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return references;
        }

        // Match patterns like "(line 5-8)" or "line 10"
        var matches = LineReferencePattern.Matches(content);
        foreach (Match match in matches)
        {
            references.Add(match.Value);
        }

        // Also try the simpler pattern
        var matches2 = LineReferencePattern2.Matches(content);
        foreach (Match match in matches2)
        {
            if (!references.Contains(match.Value))
            {
                references.Add(match.Value);
            }
        }

        return references;
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
    private static void CollectLineRangesFromChange(ParsedChange change, List<LineRange> ranges)
    {
        if (change.LineRange != null)
        {
            ranges.Add(change.LineRange);
        }

        if (change.OldLineRange != null)
        {
            ranges.Add(change.OldLineRange);
        }

        foreach (var child in change.Children)
        {
            CollectLineRangesFromChange(child, ranges);
        }
    }
}
