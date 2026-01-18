using HtmlAgilityPack;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Models;

namespace RoslynDiff.TestUtilities.Parsers;

/// <summary>
/// Parses HTML output format from roslyn-diff.
/// Uses HtmlAgilityPack to extract diff sections, line numbers, and metadata.
/// </summary>
public class HtmlOutputParser : ILineNumberParser
{
    /// <inheritdoc/>
    public string FormatName => "HTML";

    /// <inheritdoc/>
    public bool CanParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // Basic check for HTML structure
        return content.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
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
    /// Parses HTML output from a string.
    /// </summary>
    /// <param name="htmlContent">The HTML content to parse.</param>
    /// <returns>A <see cref="ParsedDiffResult"/> containing the extracted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="htmlContent"/> is null.</exception>
    public static ParsedDiffResult Parse(string htmlContent)
    {
        if (htmlContent == null)
        {
            throw new ArgumentNullException(nameof(htmlContent));
        }

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return new ParsedDiffResult
            {
                Format = "html",
                ParsingErrors = new[] { "Input HTML content is empty" }
            };
        }

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            return ParseDocument(doc);
        }
        catch (Exception ex)
        {
            return new ParsedDiffResult
            {
                Format = "html",
                ParsingErrors = new[] { $"Failed to parse HTML: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Parses HTML output from a file.
    /// </summary>
    /// <param name="filePath">The path to the HTML file.</param>
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
                Format = "html",
                ParsingErrors = new[] { $"File not found: {filePath}" }
            };
        }

        try
        {
            var doc = new HtmlDocument();
            doc.Load(filePath);
            return ParseDocument(doc);
        }
        catch (Exception ex)
        {
            return new ParsedDiffResult
            {
                Format = "html",
                ParsingErrors = new[] { $"Failed to read file: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Extracts data attributes from HTML content.
    /// </summary>
    /// <param name="content">The HTML content to parse.</param>
    /// <returns>A dictionary of element IDs to their data attributes.</returns>
    public Dictionary<string, Dictionary<string, string>> ExtractDataAttributes(string content)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var nodes = doc.DocumentNode.SelectNodes("//*[@id]");
        if (nodes == null)
        {
            return result;
        }

        foreach (var node in nodes)
        {
            var id = node.GetAttributeValue("id", string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var attributes = new Dictionary<string, string>();
            foreach (var attr in node.Attributes)
            {
                if (attr.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
                {
                    attributes[attr.Name] = attr.Value;
                }
            }

            if (attributes.Count > 0)
            {
                result[id] = attributes;
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts sections from HTML content.
    /// </summary>
    /// <param name="content">The HTML content to parse.</param>
    /// <returns>A collection of section identifiers and their line ranges.</returns>
    public Dictionary<string, List<LineRange>> ExtractSections(string content)
    {
        var result = new Dictionary<string, List<LineRange>>();
        var parsedResult = Parse(content);

        foreach (var change in parsedResult.Changes)
        {
            var sectionKey = $"{change.ChangeType}_{change.Kind}";
            if (!result.ContainsKey(sectionKey))
            {
                result[sectionKey] = new List<LineRange>();
            }

            if (change.LineRange != null)
            {
                result[sectionKey].Add(change.LineRange);
            }
        }

        return result;
    }

    /// <summary>
    /// Parses the HTML document.
    /// </summary>
    private static ParsedDiffResult ParseDocument(HtmlDocument doc)
    {
        var errors = new List<string>();
        var metadata = new Dictionary<string, string>();
        var changes = new List<ParsedChange>();

        // Extract title for file paths
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        var title = titleNode?.InnerText ?? string.Empty;

        string? oldPath = null;
        string? newPath = null;

        // Try to extract paths from title (format: "Diff: oldFile → newFile")
        if (!string.IsNullOrEmpty(title) && title.Contains("→"))
        {
            var parts = title.Replace("Diff:", "").Split('→');
            if (parts.Length == 2)
            {
                oldPath = parts[0].Trim();
                newPath = parts[1].Trim();
            }
        }

        // Extract change bodies with data attributes
        var changeNodes = doc.DocumentNode.SelectNodes("//div[@class='change-body']");
        if (changeNodes != null)
        {
            foreach (var node in changeNodes)
            {
                var change = ParseChangeNode(node);
                if (change != null)
                {
                    changes.Add(change);
                }
            }
        }

        // Calculate summary
        var summary = new ParsedSummary
        {
            TotalChanges = changes.Count,
            Additions = changes.Count(c => c.ChangeType.Equals("added", StringComparison.OrdinalIgnoreCase)),
            Deletions = changes.Count(c => c.ChangeType.Equals("removed", StringComparison.OrdinalIgnoreCase)),
            Modifications = changes.Count(c => c.ChangeType.Equals("modified", StringComparison.OrdinalIgnoreCase)),
            Renames = changes.Count(c => c.ChangeType.Equals("renamed", StringComparison.OrdinalIgnoreCase)),
            Moves = changes.Count(c => c.ChangeType.Equals("moved", StringComparison.OrdinalIgnoreCase))
        };

        return new ParsedDiffResult
        {
            Format = "html",
            OldPath = oldPath,
            NewPath = newPath,
            Changes = changes,
            Summary = summary,
            Metadata = metadata,
            ParsingErrors = errors
        };
    }

    /// <summary>
    /// Parses a change node from HTML.
    /// </summary>
    private static ParsedChange? ParseChangeNode(HtmlNode node)
    {
        var changeType = node.GetAttributeValue("data-type", string.Empty);
        var kind = node.GetAttributeValue("data-kind", string.Empty);
        var name = node.GetAttributeValue("data-name", string.Empty);
        var oldContent = HtmlEntity.DeEntitize(node.GetAttributeValue("data-old-content", string.Empty));
        var newContent = HtmlEntity.DeEntitize(node.GetAttributeValue("data-new-content", string.Empty));

        if (string.IsNullOrEmpty(changeType))
        {
            return null;
        }

        // Parse line numbers
        LineRange? lineRange = null;
        var newLineStr = node.GetAttributeValue("data-new-line", string.Empty);
        if (!string.IsNullOrEmpty(newLineStr) && int.TryParse(newLineStr, out var newLine))
        {
            // For single line changes or as a start, use the line number
            // In a real implementation, we might need to count lines in content
            var endLine = newLine + (newContent?.Split('\n').Length ?? 1) - 1;
            lineRange = new LineRange(newLine, endLine);
        }

        LineRange? oldLineRange = null;
        var oldLineStr = node.GetAttributeValue("data-old-line", string.Empty);
        if (!string.IsNullOrEmpty(oldLineStr) && int.TryParse(oldLineStr, out var oldLine))
        {
            var endLine = oldLine + (oldContent?.Split('\n').Length ?? 1) - 1;
            oldLineRange = new LineRange(oldLine, endLine);
        }

        // Collect all data attributes as metadata
        var metadata = new Dictionary<string, string>();
        foreach (var attr in node.Attributes)
        {
            if (attr.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
            {
                metadata[attr.Name] = attr.Value;
            }
        }

        return new ParsedChange
        {
            ChangeType = changeType,
            Kind = kind,
            Name = name,
            LineRange = lineRange,
            OldLineRange = oldLineRange,
            Content = newContent,
            OldContent = oldContent,
            Metadata = metadata
        };
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
    /// Interface, etc.) have computed line ranges from content that may incorrectly overlap
    /// with their children.
    /// Also skips parent nodes with children to avoid hierarchical parent-child overlaps.
    /// </remarks>
    private static void CollectLineRangesFromChange(ParsedChange change, List<LineRange> ranges)
    {
        // Skip removed items - their line numbers are from the old file context
        var isRemoved = change.ChangeType.Equals("removed", StringComparison.OrdinalIgnoreCase);

        // Skip container types whose computed line ranges from content spans children
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
