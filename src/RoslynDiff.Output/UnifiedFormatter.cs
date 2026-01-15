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

            // Group changes into hunks
            var hunks = GroupChangesIntoHunks(fileChange.Changes);

            foreach (var hunk in hunks)
            {
                WriteHunk(writer, hunk, useColor);
            }
        }

        // Note: Standard unified diff format does not include a summary line
        // Removed to match diff -u and git diff output
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

        // Find the starting line numbers and count lines
        foreach (var change in hunk)
        {
            switch (change.Type)
            {
                case ChangeType.Added:
                    if (startNew == 0 && change.NewLocation?.StartLine > 0)
                    {
                        startNew = change.NewLocation.StartLine;
                    }
                    newCount++;
                    break;

                case ChangeType.Removed:
                    if (startOld == 0 && change.OldLocation?.StartLine > 0)
                    {
                        startOld = change.OldLocation.StartLine;
                    }
                    oldCount++;
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
                    oldCount++;
                    newCount++;
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

            if (useColor)
            {
                var color = change.Type switch
                {
                    ChangeType.Added => "\x1b[32m",    // Green
                    ChangeType.Removed => "\x1b[31m", // Red
                    _ => ""
                };
                var reset = change.Type != ChangeType.Unchanged ? "\x1b[0m" : "";
                writer.WriteLine($"{color}{prefix}{content}{reset}");
            }
            else
            {
                writer.WriteLine($"{prefix}{content}");
            }
        }
    }

}
