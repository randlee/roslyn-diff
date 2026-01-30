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

        // Add TFM information if available
        if (result.AnalyzedTfms is { Count: > 0 })
        {
            var tfmList = string.Join(", ", result.AnalyzedTfms);
            sb.AppendLine($"Target Frameworks: {tfmList}");
        }

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
        var tfmAnnotation = GetTfmAnnotation(change);

        sb.AppendLine($"{prefix}[{marker}] {kindLabel}: {name}{location}{tfmAnnotation}");

        // Add whitespace warnings if present
        if (change.WhitespaceIssues != WhitespaceIssue.None)
        {
            var issueNames = GetWhitespaceIssueNames(change.WhitespaceIssues);
            var issueList = string.Join(", ", issueNames);
            sb.AppendLine($"{prefix}  WARNING: Whitespace issues: {issueList}");
        }

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

    private static string GetTfmAnnotation(Change change)
    {
        // If ApplicableToTfms is null, no TFM analysis was performed
        if (change.ApplicableToTfms is null)
            return string.Empty;

        // If ApplicableToTfms is empty, the change applies to all analyzed TFMs
        if (change.ApplicableToTfms.Count == 0)
            return string.Empty;

        // Format TFM annotation based on count
        var tfmDisplay = FormatTfmList(change.ApplicableToTfms);
        return $" [{tfmDisplay}]";
    }

    private static string FormatTfmList(IReadOnlyList<string> tfms)
    {
        if (tfms.Count == 0)
            return string.Empty;

        // Convert TFMs to friendly display names
        var displayNames = tfms.Select(FormatTfmName).ToList();

        // For single TFM with a +, show as range (e.g., ".NET 10+")
        if (displayNames.Count == 1 && IsRangeTfm(tfms[0]))
        {
            return displayNames[0];
        }

        // For multiple TFMs, show as comma-separated list
        return string.Join(", ", displayNames);
    }

    private static string FormatTfmName(string tfm)
    {
        // Handle common TFM patterns
        if (string.IsNullOrEmpty(tfm))
            return tfm;

        // Handle range notation (e.g., "net10.0+")
        if (tfm.EndsWith('+'))
        {
            var baseTfm = tfm.TrimEnd('+');
            var displayName = GetTfmDisplayName(baseTfm);
            return $"{displayName}+";
        }

        return GetTfmDisplayName(tfm);
    }

    private static string GetTfmDisplayName(string tfm)
    {
        // Handle .NET Standard: netstandard2.0, netstandard2.1, etc.
        if (tfm.StartsWith("netstandard") && tfm.Length > 11 && char.IsDigit(tfm[11]))
        {
            var versionPart = tfm.Substring(11); // Remove "netstandard" prefix
            return $".NET Standard {versionPart}";
        }

        // Handle .NET (Core) versions with platform extensions: net8.0-windows, net8.0-android, etc.
        if (tfm.StartsWith("net") && tfm.Contains('.') && tfm.Contains('-'))
        {
            // Keep platform-specific TFMs as-is
            return tfm;
        }

        // Handle .NET (Core) versions: net5.0, net6.0, net7.0, net8.0, net9.0, net10.0, etc.
        if (tfm.StartsWith("net") && tfm.Contains('.'))
        {
            var versionPart = tfm.Substring(3); // Remove "net" prefix
            return $".NET {versionPart}";
        }

        // Handle .NET Framework versions: net48, net472, net471, net47, net462, etc.
        if (tfm.StartsWith("net") && !tfm.Contains('.') && tfm.Length > 3 && char.IsDigit(tfm[3]))
        {
            var versionDigits = tfm.Substring(3);
            if (versionDigits.Length >= 2)
            {
                var major = versionDigits.Substring(0, 1);
                var minor = versionDigits.Substring(1, 1);
                var patch = versionDigits.Length > 2 ? versionDigits.Substring(2) : "0";
                return $".NET Framework {major}.{minor}.{patch}";
            }
        }

        // Return as-is for unrecognized formats
        return tfm;
    }

    private static bool IsRangeTfm(string tfm)
    {
        return tfm.EndsWith('+');
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

    /// <summary>
    /// Converts WhitespaceIssue flags to a list of issue names.
    /// </summary>
    private static IEnumerable<string> GetWhitespaceIssueNames(WhitespaceIssue issues)
    {
        if (issues.HasFlag(WhitespaceIssue.IndentationChanged))
            yield return "IndentationChanged";
        if (issues.HasFlag(WhitespaceIssue.MixedTabsSpaces))
            yield return "MixedTabsSpaces";
        if (issues.HasFlag(WhitespaceIssue.TrailingWhitespace))
            yield return "TrailingWhitespace";
        if (issues.HasFlag(WhitespaceIssue.LineEndingChanged))
            yield return "LineEndingChanged";
        if (issues.HasFlag(WhitespaceIssue.AmbiguousTabWidth))
            yield return "AmbiguousTabWidth";
    }

    /// <inheritdoc/>
    public string FormatMultiFileResult(MultiFileDiffResult result, OutputOptions? options = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Multi-File Diff Report: {result.Summary.TotalFiles} files changed");
        sb.AppendLine($"Total Changes: {result.Summary.TotalChanges}");
        sb.AppendLine();

        foreach (var file in result.Files)
        {
            var fileName = file.NewPath ?? file.OldPath ?? "Unknown";
            sb.AppendLine($"--- File: {fileName} ({file.Status}) ---");
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
