namespace RoslynDiff.Output;

using System.Text;
using RoslynDiff.Core.Models;

/// <summary>
/// Formats diff results as plain text output without ANSI escape codes.
/// Suitable for piping, redirecting, or environments without color support.
/// </summary>
public class PlainTextFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string Format => "plain";

    /// <inheritdoc/>
    public string ContentType => "text/plain";

    /// <inheritdoc/>
    public string FormatResult(DiffResult result, OutputOptions? options = null)
    {
        options ??= new OutputOptions();
        var sb = new StringBuilder();

        FormatHeader(sb, result);

        if (options.IncludeStats)
        {
            FormatSummary(sb, result.Stats);
        }

        FormatChanges(sb, result, options);

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var text = FormatResult(result, options);
        await writer.WriteAsync(text);
    }

    private static void FormatHeader(StringBuilder sb, DiffResult result)
    {
        var oldPath = result.OldPath ?? "(none)";
        var newPath = result.NewPath ?? "(none)";

        sb.AppendLine($"Diff: {oldPath} -> {newPath}");
        sb.AppendLine($"Mode: {GetModeDescription(result.Mode)}");
        sb.AppendLine();
    }

    private static string GetModeDescription(DiffMode mode)
    {
        return mode switch
        {
            DiffMode.Roslyn => "Roslyn (semantic)",
            DiffMode.Line => "Line (text)",
            _ => mode.ToString()
        };
    }

    private static void FormatSummary(StringBuilder sb, DiffStats stats)
    {
        var parts = new List<string>();

        if (stats.Additions > 0)
            parts.Add($"+{stats.Additions}");
        if (stats.Deletions > 0)
            parts.Add($"-{stats.Deletions}");
        if (stats.Modifications > 0)
            parts.Add($"~{stats.Modifications}");
        if (stats.Moves > 0)
            parts.Add($">{stats.Moves}");
        if (stats.Renames > 0)
            parts.Add($"@{stats.Renames}");

        var summary = parts.Count > 0
            ? string.Join(" ", parts)
            : "No changes";

        sb.AppendLine($"Summary: {summary} ({stats.TotalChanges} total changes)");
        sb.AppendLine();
    }

    private static void FormatChanges(StringBuilder sb, DiffResult result, OutputOptions options)
    {
        if (result.FileChanges.Count == 0)
        {
            sb.AppendLine("No changes detected.");
            return;
        }

        sb.AppendLine("Changes:");

        foreach (var fileChange in result.FileChanges)
        {
            if (!string.IsNullOrEmpty(fileChange.Path))
            {
                sb.AppendLine($"  File: {fileChange.Path}");
            }

            foreach (var change in fileChange.Changes)
            {
                FormatChange(sb, change, options, indent: 2);
            }
        }
    }

    private static void FormatChange(StringBuilder sb, Change change, OutputOptions options, int indent)
    {
        var prefix = new string(' ', indent);
        var marker = GetChangeMarker(change.Type);
        var kindLabel = GetKindLabel(change.Kind);
        var name = change.Name ?? "(unnamed)";
        var location = GetLocationString(change);

        sb.AppendLine($"{prefix}[{marker}] {kindLabel}: {name}{location}");

        // Add details for modifications
        if (change.Type == ChangeType.Modified && !options.Compact)
        {
            FormatModificationDetails(sb, change, indent + 4);
        }

        // Format child changes
        if (change.Children is { Count: > 0 })
        {
            foreach (var child in change.Children)
            {
                FormatChange(sb, child, options, indent + 4);
            }
        }
    }

    private static string GetChangeMarker(ChangeType type)
    {
        return type switch
        {
            ChangeType.Added => "+",
            ChangeType.Removed => "-",
            ChangeType.Modified => "~",
            ChangeType.Moved => ">",
            ChangeType.Renamed => "@",
            ChangeType.Unchanged => "=",
            _ => "?"
        };
    }

    private static string GetKindLabel(ChangeKind kind)
    {
        return kind switch
        {
            ChangeKind.File => "File",
            ChangeKind.Namespace => "Namespace",
            ChangeKind.Class => "Class",
            ChangeKind.Method => "Method",
            ChangeKind.Property => "Property",
            ChangeKind.Field => "Field",
            ChangeKind.Statement => "Statement",
            ChangeKind.Line => "Line",
            _ => kind.ToString()
        };
    }

    private static string GetLocationString(Change change)
    {
        var location = change.NewLocation ?? change.OldLocation;
        if (location is null)
            return string.Empty;

        if (location.StartLine == location.EndLine)
            return $" (line {location.StartLine})";

        return $" (line {location.StartLine}-{location.EndLine})";
    }

    private static void FormatModificationDetails(StringBuilder sb, Change change, int indent)
    {
        var prefix = new string(' ', indent);

        // Infer what changed based on available content
        if (change.OldContent is not null && change.NewContent is not null)
        {
            // If we have both contents, we could do a more detailed comparison
            // For now, just indicate it was modified
            sb.AppendLine($"{prefix}Body modified");
        }
    }
}
