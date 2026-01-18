using System.Text.Json;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Models;

namespace RoslynDiff.TestUtilities.Parsers;

/// <summary>
/// Parses JSON output format from roslyn-diff.
/// Supports both stdout JSON and file-based JSON formats.
/// </summary>
public class JsonOutputParser : ILineNumberParser
{
    /// <inheritdoc/>
    public string FormatName => "JSON";

    /// <inheritdoc/>
    public bool CanParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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
    /// Parses JSON output from a string.
    /// </summary>
    /// <param name="jsonContent">The JSON content to parse.</param>
    /// <returns>A <see cref="ParsedDiffResult"/> containing the extracted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonContent"/> is null.</exception>
    public static ParsedDiffResult Parse(string jsonContent)
    {
        if (jsonContent == null)
        {
            throw new ArgumentNullException(nameof(jsonContent));
        }

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new ParsedDiffResult
            {
                Format = "json",
                ParsingErrors = new[] { "Input JSON content is empty" }
            };
        }

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            return ParseDocument(document.RootElement);
        }
        catch (JsonException ex)
        {
            return new ParsedDiffResult
            {
                Format = "json",
                ParsingErrors = new[] { $"Failed to parse JSON: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Parses JSON output from a file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
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
                Format = "json",
                ParsingErrors = new[] { $"File not found: {filePath}" }
            };
        }

        try
        {
            var jsonContent = File.ReadAllText(filePath);
            return Parse(jsonContent);
        }
        catch (Exception ex)
        {
            return new ParsedDiffResult
            {
                Format = "json",
                ParsingErrors = new[] { $"Failed to read file: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Normalizes JSON content for comparison (removes whitespace differences).
    /// </summary>
    /// <param name="content">The JSON content to normalize.</param>
    /// <returns>Normalized JSON string.</returns>
    public string Normalize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
        catch (JsonException)
        {
            return content;
        }
    }

    /// <summary>
    /// Parses the root JSON document.
    /// </summary>
    private static ParsedDiffResult ParseDocument(JsonElement root)
    {
        var errors = new List<string>();
        var metadata = new Dictionary<string, string>();

        // Extract metadata
        DateTime? timestamp = null;
        string? mode = null;
        string? version = null;

        if (root.TryGetProperty("metadata", out var metadataElement))
        {
            timestamp = ParseTimestamp(metadataElement);
            mode = GetStringProperty(metadataElement, "mode");
            version = GetStringProperty(metadataElement, "version");

            // Store all metadata
            foreach (var prop in metadataElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }

        // Extract summary
        ParsedSummary? summary = null;
        if (root.TryGetProperty("summary", out var summaryElement))
        {
            summary = new ParsedSummary
            {
                TotalChanges = GetIntProperty(summaryElement, "totalChanges"),
                Additions = GetIntProperty(summaryElement, "additions"),
                Deletions = GetIntProperty(summaryElement, "deletions"),
                Modifications = GetIntProperty(summaryElement, "modifications"),
                Renames = GetIntProperty(summaryElement, "renames"),
                Moves = GetIntProperty(summaryElement, "moves")
            };
        }

        // Extract changes from files
        var allChanges = new List<ParsedChange>();
        string? oldPath = null;
        string? newPath = null;

        if (root.TryGetProperty("files", out var filesElement) &&
            filesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var fileElement in filesElement.EnumerateArray())
            {
                // Get file paths from the first file entry
                if (oldPath == null)
                {
                    oldPath = GetStringProperty(fileElement, "oldPath");
                    newPath = GetStringProperty(fileElement, "newPath");
                }

                if (fileElement.TryGetProperty("changes", out var changesElement) &&
                    changesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var changeElement in changesElement.EnumerateArray())
                    {
                        var change = ParseChange(changeElement);
                        if (change != null)
                        {
                            allChanges.Add(change);
                        }
                    }
                }
            }
        }

        return new ParsedDiffResult
        {
            Format = "json",
            Timestamp = timestamp,
            OldPath = oldPath,
            NewPath = newPath,
            Mode = mode,
            Version = version,
            Changes = allChanges,
            Summary = summary,
            Metadata = metadata,
            ParsingErrors = errors
        };
    }

    /// <summary>
    /// Parses a single change element.
    /// </summary>
    private static ParsedChange? ParseChange(JsonElement changeElement)
    {
        var changeType = GetStringProperty(changeElement, "type") ?? string.Empty;
        var kind = GetStringProperty(changeElement, "kind");
        var name = GetStringProperty(changeElement, "name");
        var content = GetStringProperty(changeElement, "content");
        var oldContent = GetStringProperty(changeElement, "oldContent");

        // Parse location (new version)
        LineRange? lineRange = null;
        if (changeElement.TryGetProperty("location", out var locationElement))
        {
            lineRange = ParseLocation(locationElement);
        }

        // Parse old location (previous version)
        LineRange? oldLineRange = null;
        if (changeElement.TryGetProperty("oldLocation", out var oldLocationElement))
        {
            oldLineRange = ParseLocation(oldLocationElement);
        }

        // Parse children recursively
        var children = new List<ParsedChange>();
        if (changeElement.TryGetProperty("children", out var childrenElement) &&
            childrenElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var childElement in childrenElement.EnumerateArray())
            {
                var child = ParseChange(childElement);
                if (child != null)
                {
                    children.Add(child);
                }
            }
        }

        // Collect metadata
        var metadata = new Dictionary<string, string>();
        foreach (var prop in changeElement.EnumerateObject())
        {
            if (prop.Name is not ("type" or "kind" or "name" or "location" or "oldLocation" or
                                  "content" or "oldContent" or "children") &&
                prop.Value.ValueKind == JsonValueKind.String)
            {
                metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }

        return new ParsedChange
        {
            ChangeType = changeType,
            Kind = kind,
            Name = name,
            LineRange = lineRange,
            OldLineRange = oldLineRange,
            Content = content,
            OldContent = oldContent,
            Children = children,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Parses a location element to extract line range.
    /// </summary>
    private static LineRange? ParseLocation(JsonElement locationElement)
    {
        var startLine = GetIntProperty(locationElement, "startLine");
        var endLine = GetIntProperty(locationElement, "endLine");

        if (startLine > 0 && endLine > 0)
        {
            return new LineRange(startLine, endLine);
        }

        return null;
    }

    /// <summary>
    /// Parses a timestamp from metadata.
    /// </summary>
    private static DateTime? ParseTimestamp(JsonElement metadataElement)
    {
        var timestampStr = GetStringProperty(metadataElement, "timestamp");
        if (timestampStr != null && DateTime.TryParse(timestampStr, out var timestamp))
        {
            return timestamp;
        }

        return null;
    }

    /// <summary>
    /// Gets a string property value from a JSON element.
    /// </summary>
    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    /// <summary>
    /// Gets an integer property value from a JSON element.
    /// </summary>
    private static int GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number)
        {
            return property.GetInt32();
        }

        return 0;
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
