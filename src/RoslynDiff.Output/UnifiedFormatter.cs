namespace RoslynDiff.Output;

using System.Text;
using RoslynDiff.Core.Models;

/// <summary>
/// Formats diff results in unified diff format (similar to git diff).
/// </summary>
public sealed class UnifiedFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string Format => "text";

    /// <inheritdoc/>
    public string ContentType => "text/plain";

    /// <inheritdoc/>
    public string FormatResult(DiffResult result, OutputOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        FormatInternal(result, writer, options);
        return sb.ToString();
    }

    /// <inheritdoc/>
    public Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        FormatInternal(result, writer, options);
        return Task.CompletedTask;
    }

    private static void FormatInternal(DiffResult result, TextWriter writer, OutputOptions? options)
    {
        var useColor = options?.UseColor ?? false;

        // Write header (no blank line after, matching standard diff -u format)
        writer.WriteLine($"--- {result.OldPath ?? "/dev/null"}");
        writer.WriteLine($"+++ {result.NewPath ?? "/dev/null"}");

        foreach (var fileChange in result.FileChanges)
        {
            if (fileChange.Changes.Count == 0)
            {
                continue;
            }

            // For semantic diffs (Roslyn mode), flatten the hierarchy to only show leaf-level changes
            // This avoids duplicate content when parent containers include full content
            // For line-level diffs, use the changes as-is (they're already flat)
            var changesToProcess = result.Mode == DiffMode.Roslyn
                ? FlattenToLeafChanges(fileChange.Changes)
                : fileChange.Changes.ToList();

            if (changesToProcess.Count == 0)
            {
                continue;
            }

            // Group changes into hunks
            var hunks = GroupChangesIntoHunks(changesToProcess);

            foreach (var hunk in hunks)
            {
                WriteHunk(writer, hunk, useColor);
            }
        }

        // Note: Standard unified diff format does not include a summary line
        // Removed to match diff -u and git diff output
    }

    /// <summary>
    /// Flattens hierarchical changes to only include leaf-level changes.
    /// For semantic diffs, parent nodes (namespace, class) may include full content,
    /// but we only want to show the actual changed items (methods, properties, etc.).
    /// Deduplicates based on location to avoid showing the same change multiple times.
    /// </summary>
    private static List<Change> FlattenToLeafChanges(IReadOnlyList<Change> changes)
    {
        var leafChanges = new List<Change>();
        var seenLocations = new HashSet<string>();
        CollectLeafChanges(changes, leafChanges, seenLocations);
        return leafChanges;
    }

    /// <summary>
    /// Recursively collects leaf-level changes (those without children or where children should be shown).
    /// </summary>
    private static void CollectLeafChanges(IReadOnlyList<Change> changes, List<Change> result, HashSet<string> seenLocations)
    {
        foreach (var change in changes)
        {
            if (change.Children is { Count: > 0 })
            {
                // This change has children - recurse into them
                // Don't include this parent change as it would duplicate content
                CollectLeafChanges(change.Children, result, seenLocations);
            }
            else
            {
                // Leaf change - include it if not already seen
                // For unified diff format:
                // - Always include Added and Removed changes
                // - Include Unchanged and Modified changes only for line-level diffs (Kind.Line)
                //   These provide context and show modified lines
                // - Exclude Unchanged/Modified changes from semantic diffs (Kind.Class, Method, etc.)
                //   as they often contain too much content (e.g., entire class body)
                var isLineLevel = change.Kind == ChangeKind.Line;
                var shouldInclude = change.Type == ChangeType.Added ||
                                    change.Type == ChangeType.Removed ||
                                    (isLineLevel && (change.Type == ChangeType.Modified || change.Type == ChangeType.Unchanged));

                if (shouldInclude)
                {
                    // Create a unique key based on location to deduplicate
                    var locationKey = GetLocationKey(change);
                    if (seenLocations.Add(locationKey))
                    {
                        result.Add(change);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates a unique key for a change based on its location and type.
    /// </summary>
    private static string GetLocationKey(Change change)
    {
        var oldLoc = change.OldLocation;
        var newLoc = change.NewLocation;
        return $"{change.Type}|{oldLoc?.StartLine}:{oldLoc?.EndLine}|{newLoc?.StartLine}:{newLoc?.EndLine}";
    }

    private static List<List<Change>> GroupChangesIntoHunks(IReadOnlyList<Change> changes)
    {
        var hunks = new List<List<Change>>();
        if (changes.Count == 0)
        {
            return hunks;
        }

        var currentHunk = new List<Change> { changes[0] };

        for (var i = 1; i < changes.Count; i++)
        {
            var prevChange = changes[i - 1];
            var currChange = changes[i];

            // Get line numbers for both changes
            var prevOldLine = prevChange.OldLocation?.EndLine ?? 0;
            var prevNewLine = prevChange.NewLocation?.EndLine ?? 0;
            var currOldLine = currChange.OldLocation?.StartLine ?? 0;
            var currNewLine = currChange.NewLocation?.StartLine ?? 0;

            // Check for gaps in line numbers (indicates new hunk)
            // A gap exists if the current line doesn't immediately follow the previous
            var oldGap = prevOldLine > 0 && currOldLine > 0 && currOldLine > prevOldLine + 1;
            var newGap = prevNewLine > 0 && currNewLine > 0 && currNewLine > prevNewLine + 1;

            if (oldGap || newGap)
            {
                // Start a new hunk
                hunks.Add(currentHunk);
                currentHunk = new List<Change>();
            }

            currentHunk.Add(currChange);
        }

        if (currentHunk.Count > 0)
        {
            hunks.Add(currentHunk);
        }

        return hunks;
    }

    private static void WriteHunk(TextWriter writer, List<Change> hunk, bool useColor)
    {
        if (hunk.Count == 0)
        {
            return;
        }

        // Calculate hunk header by tracking line numbers separately for old and new files
        // In unified diff: @@ -startOld,oldCount +startNew,newCount @@
        // - oldCount = number of lines from old file (unchanged + removed)
        // - newCount = number of lines in new file (unchanged + added)

        var startOld = 0;
        var startNew = 0;
        var oldCount = 0;
        var newCount = 0;

        // Find the starting line numbers and count actual lines in content
        foreach (var change in hunk)
        {
            var contentLineCount = CountContentLines(change);

            switch (change.Type)
            {
                case ChangeType.Added:
                    if (startNew == 0 && change.NewLocation?.StartLine > 0)
                    {
                        startNew = change.NewLocation.StartLine;
                    }
                    newCount += contentLineCount;
                    break;

                case ChangeType.Removed:
                    if (startOld == 0 && change.OldLocation?.StartLine > 0)
                    {
                        startOld = change.OldLocation.StartLine;
                    }
                    oldCount += contentLineCount;
                    break;

                case ChangeType.Unchanged:
                case ChangeType.Modified:
                default:
                    // Context lines and modified lines exist in both files
                    if (startOld == 0 && change.OldLocation?.StartLine > 0)
                    {
                        startOld = change.OldLocation.StartLine;
                    }
                    if (startNew == 0 && change.NewLocation?.StartLine > 0)
                    {
                        startNew = change.NewLocation.StartLine;
                    }
                    oldCount += contentLineCount;
                    newCount += contentLineCount;
                    break;
            }
        }

        // Handle edge cases for empty files
        // For empty old file going to content: start at 0,0 for old
        // For content going to empty new file: start at 0,0 for new
        if (startOld == 0 && oldCount == 0)
        {
            startOld = 0;
        }
        else if (startOld == 0)
        {
            startOld = 1;
        }

        if (startNew == 0 && newCount == 0)
        {
            startNew = 0;
        }
        else if (startNew == 0)
        {
            startNew = 1;
        }

        writer.WriteLine($"@@ -{startOld},{oldCount} +{startNew},{newCount} @@");

        foreach (var change in hunk)
        {
            var prefix = change.Type switch
            {
                ChangeType.Added => "+",
                ChangeType.Removed => "-",
                _ => " "
            };

            var content = change.Type switch
            {
                ChangeType.Added => change.NewContent ?? "",
                ChangeType.Removed => change.OldContent ?? "",
                _ => change.OldContent ?? change.NewContent ?? ""
            };

            // Split content into lines and prefix each line appropriately
            var lines = content.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                // Remove trailing \r if present (for Windows line endings)
                var line = lines[i].TrimEnd('\r');

                // Skip empty trailing line that results from trailing newline
                if (i == lines.Length - 1 && string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (useColor)
                {
                    var color = change.Type switch
                    {
                        ChangeType.Added => "\x1b[32m",    // Green
                        ChangeType.Removed => "\x1b[31m", // Red
                        _ => ""
                    };
                    var reset = change.Type != ChangeType.Unchanged ? "\x1b[0m" : "";
                    writer.WriteLine($"{color}{prefix}{line}{reset}");
                }
                else
                {
                    writer.WriteLine($"{prefix}{line}");
                }
            }
        }
    }

    /// <summary>
    /// Counts the number of actual lines in a change's content.
    /// </summary>
    private static int CountContentLines(Change change)
    {
        var content = change.Type switch
        {
            ChangeType.Added => change.NewContent ?? "",
            ChangeType.Removed => change.OldContent ?? "",
            _ => change.OldContent ?? change.NewContent ?? ""
        };

        if (string.IsNullOrEmpty(content))
        {
            return 1; // Fallback to 1 line for empty content
        }

        // Count non-empty lines (same logic as the line-splitting in WriteHunk)
        var lines = content.Split('\n');
        var count = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            // Skip empty trailing line that results from trailing newline
            if (i == lines.Length - 1 && string.IsNullOrEmpty(line))
            {
                continue;
            }
            count++;
        }

        return Math.Max(count, 1); // At least 1 line
    }

    /// <inheritdoc/>
    public string FormatMultiFileResult(MultiFileDiffResult result, OutputOptions? options = null)
    {
        var sb = new StringBuilder();

        foreach (var file in result.Files)
        {
            var oldPath = file.OldPath ?? "/dev/null";
            var newPath = file.NewPath ?? "/dev/null";

            sb.AppendLine($"diff --git a/{oldPath} b/{newPath}");
            sb.AppendLine($"--- a/{oldPath}");
            sb.AppendLine($"+++ b/{newPath}");
            sb.AppendLine(FormatResult(file.Result, options));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task FormatMultiFileResultAsync(MultiFileDiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var text = FormatMultiFileResult(result, options);
        await writer.WriteAsync(text);
    }
}
