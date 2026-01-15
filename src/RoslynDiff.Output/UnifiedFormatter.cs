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

        // Write header
        writer.WriteLine($"--- {result.OldPath ?? "/dev/null"}");
        writer.WriteLine($"+++ {result.NewPath ?? "/dev/null"}");
        writer.WriteLine();

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

        // Write summary
        writer.WriteLine();
        WriteSummary(writer, result.Stats, useColor);
    }

    private static List<List<Change>> GroupChangesIntoHunks(IReadOnlyList<Change> changes)
    {
        var hunks = new List<List<Change>>();
        var currentHunk = new List<Change>();

        foreach (var change in changes)
        {
            currentHunk.Add(change);

            // Start a new hunk if we encounter a gap (in a real implementation,
            // we would track line numbers and detect gaps)
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

        // Calculate hunk header
        var firstChange = hunk.FirstOrDefault(c => c.OldLocation != null || c.NewLocation != null);
        var startOld = firstChange?.OldLocation?.StartLine ?? firstChange?.NewLocation?.StartLine ?? 1;
        var startNew = firstChange?.NewLocation?.StartLine ?? firstChange?.OldLocation?.StartLine ?? 1;

        var oldCount = hunk.Count(c => c.Type != ChangeType.Added);
        var newCount = hunk.Count(c => c.Type != ChangeType.Removed);

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

    private static void WriteSummary(TextWriter writer, DiffStats stats, bool useColor)
    {
        var addColor = useColor ? "\x1b[32m" : "";
        var delColor = useColor ? "\x1b[31m" : "";
        var reset = useColor ? "\x1b[0m" : "";

        if (stats.TotalChanges == 0)
        {
            writer.WriteLine("No differences found.");
        }
        else
        {
            writer.WriteLine($"{stats.TotalChanges} change(s): {addColor}+{stats.Additions}{reset}, {delColor}-{stats.Deletions}{reset}, ~{stats.Modifications}");
        }
    }
}
