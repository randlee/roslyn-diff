namespace RoslynDiff.Core.Differ;

using System.Text.RegularExpressions;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using RoslynDiff.Core.Models;

// Alias to avoid ambiguity with DiffPlex.DiffBuilder.Model.ChangeType
using DiffPlexChangeType = DiffPlex.DiffBuilder.Model.ChangeType;

/// <summary>
/// Performs line-by-line diff comparison using DiffPlex.
/// </summary>
/// <remarks>
/// This differ is used as a fallback when Roslyn semantic diff is not applicable,
/// such as for non-.NET files or when explicitly requested via options.
/// </remarks>
public sealed class LineDiffer : IDiffer
{
    private readonly DiffPlex.IDiffer _differ;

    /// <summary>
    /// Initializes a new instance of the <see cref="LineDiffer"/> class.
    /// </summary>
    public LineDiffer()
    {
        _differ = new DiffPlex.Differ();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LineDiffer"/> class with a custom differ.
    /// </summary>
    /// <param name="differ">The DiffPlex differ to use for comparison.</param>
    internal LineDiffer(DiffPlex.IDiffer differ)
    {
        _differ = differ;
    }

    /// <inheritdoc/>
    public bool CanHandle(string filePath, DiffOptions options)
    {
        // LineDiffer can handle any text file
        // If mode is explicitly set to Line, always handle it
        if (options.Mode == DiffMode.Line)
        {
            return true;
        }

        // If mode is explicitly set to Roslyn, don't handle it
        if (options.Mode == DiffMode.Roslyn)
        {
            return false;
        }

        // In auto mode, LineDiffer is typically the fallback
        // Return true for non-.NET files
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension is not (".cs" or ".vb");
    }

    /// <inheritdoc/>
    /// <inheritdoc/>
    public DiffResult Compare(string oldContent, string newContent, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldContent);
        ArgumentNullException.ThrowIfNull(newContent);
        ArgumentNullException.ThrowIfNull(options);

        // Determine effective whitespace mode (backward compatibility with IgnoreWhitespace)
        var effectiveMode = ResolveWhitespaceMode(options);
        var filePath = options.NewPath ?? options.OldPath;

        // Preprocess content and determine DiffPlex ignoreWhitespace flag based on mode
        var (processedOld, processedNew, diffPlexIgnoreWs) = PrepareContentForComparison(
            oldContent, newContent, effectiveMode, filePath);

        var diffBuilder = new InlineDiffBuilder(_differ);
        var diff = diffBuilder.BuildDiffModel(processedOld, processedNew, diffPlexIgnoreWs);

        var changes = BuildChanges(diff, options, oldContent, newContent, effectiveMode, filePath);
        var stats = CalculateStats(changes);

        return new DiffResult
        {
            OldPath = options.OldPath,
            NewPath = options.NewPath,
            Mode = DiffMode.Line,
            FileChanges = [new FileChange
            {
                Path = filePath,
                Changes = changes
            }],
            Stats = stats
        };
    }

    private static List<Change> BuildChanges(
        DiffPaneModel diff, 
        DiffOptions options, 
        string oldContent, 
        string newContent,
        WhitespaceMode effectiveMode,
        string? filePath)
    {
        // Filter out trailing empty lines that result from files ending with newlines
        // This matches standard diff behavior which doesn't count trailing newlines as lines
        var diffLines = diff.Lines.ToList();
        while (diffLines.Count > 0 && string.IsNullOrEmpty(diffLines[^1].Text))
        {
            diffLines.RemoveAt(diffLines.Count - 1);
        }

        // Determine if we should analyze whitespace issues (only for significant languages in LanguageAware mode)
        var analyzeWhitespaceIssues = effectiveMode == WhitespaceMode.LanguageAware &&
                                       LanguageClassifier.GetSensitivity(filePath) == WhitespaceSensitivity.Significant;

        // Parse original content into lines for whitespace analysis
        var oldLines = oldContent.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var newLines = newContent.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        // First pass: Collect all lines with their positions and types
        var allLines = new List<(DiffPiece Piece, int? OldLine, int? NewLine, bool IsChange)>();
        var oldLineNum = 0;
        var newLineNum = 0;

        foreach (var line in diffLines)
        {
            switch (line.Type)
            {
                case DiffPlexChangeType.Unchanged:
                    oldLineNum++;
                    newLineNum++;
                    allLines.Add((line, oldLineNum, newLineNum, false));
                    break;

                case DiffPlexChangeType.Inserted:
                    newLineNum++;
                    allLines.Add((line, null, newLineNum, true));
                    break;

                case DiffPlexChangeType.Deleted:
                    oldLineNum++;
                    allLines.Add((line, oldLineNum, null, true));
                    break;

                case DiffPlexChangeType.Modified:
                    oldLineNum++;
                    newLineNum++;
                    allLines.Add((line, oldLineNum, newLineNum, true));
                    break;

                case DiffPlexChangeType.Imaginary:
                    // Imaginary lines are placeholders, skip them
                    break;
            }
        }

        // Second pass: Group changes into hunks based on proximity
        var changes = new List<Change>();
        var contextLines = options.ContextLines;

        // Find change boundaries and build hunks with context
        var changeIndices = allLines
            .Select((line, idx) => (line, idx))
            .Where(x => x.line.IsChange)
            .Select(x => x.idx)
            .ToList();

        if (changeIndices.Count == 0)
        {
            return changes; // No changes
        }

        // Group changes into hunks (merge if gap <= 2 * contextLines)
        var hunkRanges = new List<(int Start, int End)>();
        var currentHunkStart = changeIndices[0];
        var currentHunkEnd = changeIndices[0];

        for (var i = 1; i < changeIndices.Count; i++)
        {
            var prevEnd = currentHunkEnd;
            var currStart = changeIndices[i];

            // If gap between changes is small enough, merge into same hunk
            // Gap must be <= 2 * contextLines (context after prev + context before curr can overlap)
            if (currStart - prevEnd <= 2 * contextLines)
            {
                currentHunkEnd = currStart;
            }
            else
            {
                // Save current hunk and start new one
                hunkRanges.Add((currentHunkStart, currentHunkEnd));
                currentHunkStart = currStart;
                currentHunkEnd = currStart;
            }
        }
        // Add the last hunk
        hunkRanges.Add((currentHunkStart, currentHunkEnd));

        // Third pass: Build changes for each hunk with context
        // Group consecutive deletions before additions (standard unified diff ordering)
        foreach (var (hunkStart, hunkEnd) in hunkRanges)
        {
            // Calculate context boundaries
            var contextStart = Math.Max(0, hunkStart - contextLines);
            var contextEnd = Math.Min(allLines.Count - 1, hunkEnd + contextLines);

            // Process lines, grouping consecutive deletions before additions
            var pendingDeletions = new List<(DiffPiece Piece, int? OldLine, int? NewLine)>();
            var pendingAdditions = new List<(DiffPiece Piece, int? OldLine, int? NewLine)>();

            for (var i = contextStart; i <= contextEnd; i++)
            {
                var (piece, oldLine, newLine, isChange) = allLines[i];

                if (piece.Type == DiffPlexChangeType.Deleted)
                {
                    pendingDeletions.Add((piece, oldLine, newLine));
                }
                else if (piece.Type == DiffPlexChangeType.Inserted)
                {
                    pendingAdditions.Add((piece, oldLine, newLine));
                }
                else
                {
                    // Unchanged or Modified - flush any pending changes first
                    FlushPendingChanges(changes, pendingDeletions, pendingAdditions, 
                                       analyzeWhitespaceIssues, oldLines, newLines);
                    pendingDeletions.Clear();
                    pendingAdditions.Clear();

                    // Add the context/modified line
                    var changeType = piece.Type == DiffPlexChangeType.Modified
                        ? Models.ChangeType.Modified
                        : Models.ChangeType.Unchanged;

                    changes.Add(new Change
                    {
                        Type = changeType,
                        Kind = ChangeKind.Line,
                        OldContent = piece.Text,
                        NewContent = piece.Text,
                        OldLocation = oldLine.HasValue ? new Location { StartLine = oldLine.Value, EndLine = oldLine.Value } : null,
                        NewLocation = newLine.HasValue ? new Location { StartLine = newLine.Value, EndLine = newLine.Value } : null
                    });
                }
            }

            // Flush any remaining pending changes
            FlushPendingChanges(changes, pendingDeletions, pendingAdditions,
                               analyzeWhitespaceIssues, oldLines, newLines);
        }

        return changes;
    }

    private static void FlushPendingChanges(
        List<Change> changes,
        List<(DiffPiece Piece, int? OldLine, int? NewLine)> deletions,
        List<(DiffPiece Piece, int? OldLine, int? NewLine)> additions,
        bool analyzeWhitespaceIssues,
        string[] oldLines,
        string[] newLines)
    {
        // Try to pair deletions with additions for whitespace analysis
        // When a deletion is followed by an addition, they likely represent a modification
        var pairedCount = Math.Min(deletions.Count, additions.Count);

        // Add all deletions first
        for (var i = 0; i < deletions.Count; i++)
        {
            var (piece, oldLine, newLine) = deletions[i];
            var whitespaceIssues = WhitespaceIssue.None;

            // If this deletion is paired with an addition, analyze whitespace issues
            if (analyzeWhitespaceIssues && i < pairedCount)
            {
                var oldIdx = oldLine.GetValueOrDefault() - 1;
                var newIdx = additions[i].NewLine.GetValueOrDefault() - 1;
                var oldLineContent = oldLine.HasValue && oldIdx >= 0 && oldIdx < oldLines.Length
                    ? oldLines[oldIdx] : null;
                var newLineContent = additions[i].NewLine.HasValue && newIdx >= 0 && newIdx < newLines.Length
                    ? newLines[newIdx] : null;
                whitespaceIssues = WhitespaceAnalyzer.Analyze(oldLineContent, newLineContent);
            }

            changes.Add(new Change
            {
                Type = Models.ChangeType.Removed,
                Kind = ChangeKind.Line,
                OldContent = piece.Text,
                NewContent = null,
                OldLocation = oldLine.HasValue ? new Location { StartLine = oldLine.Value, EndLine = oldLine.Value } : null,
                NewLocation = null,
                WhitespaceIssues = whitespaceIssues
            });
        }

        // Then add all additions
        for (var i = 0; i < additions.Count; i++)
        {
            var (piece, oldLine, newLine) = additions[i];
            var whitespaceIssues = WhitespaceIssue.None;

            // If this addition is paired with a deletion, analyze whitespace issues
            if (analyzeWhitespaceIssues && i < pairedCount)
            {
                var oldIdx = deletions[i].OldLine.GetValueOrDefault() - 1;
                var newIdx = newLine.GetValueOrDefault() - 1;
                var oldLineContent = deletions[i].OldLine.HasValue && oldIdx >= 0 && oldIdx < oldLines.Length
                    ? oldLines[oldIdx] : null;
                var newLineContent = newLine.HasValue && newIdx >= 0 && newIdx < newLines.Length
                    ? newLines[newIdx] : null;
                whitespaceIssues = WhitespaceAnalyzer.Analyze(oldLineContent, newLineContent);
            }
            else if (analyzeWhitespaceIssues)
            {
                // Pure addition - check for whitespace issues in the new line
                var newIdx = newLine.GetValueOrDefault() - 1;
                var newLineContent = newLine.HasValue && newIdx >= 0 && newIdx < newLines.Length
                    ? newLines[newIdx] : null;
                whitespaceIssues = WhitespaceAnalyzer.Analyze(null, newLineContent);
            }

            changes.Add(new Change
            {
                Type = Models.ChangeType.Added,
                Kind = ChangeKind.Line,
                OldContent = null,
                NewContent = piece.Text,
                OldLocation = null,
                NewLocation = newLine.HasValue ? new Location { StartLine = newLine.Value, EndLine = newLine.Value } : null,
                WhitespaceIssues = whitespaceIssues
            });
        }
    }

    private static DiffStats CalculateStats(IReadOnlyList<Change> changes)
    {
        var additions = 0;
        var deletions = 0;
        var modifications = 0;

        foreach (var change in changes)
        {
            switch (change.Type)
            {
                case Models.ChangeType.Added:
                    additions++;
                    break;
                case Models.ChangeType.Removed:
                    deletions++;
                    break;
                case Models.ChangeType.Modified:
                    modifications++;
                    break;
            }
        }

        return new DiffStats
        {
            Additions = additions,
            Deletions = deletions,
            Modifications = modifications,
            Moves = 0,
            Renames = 0
        };
    }

    /// <summary>
    /// Resolves the effective whitespace mode, accounting for backward compatibility with IgnoreWhitespace.
    /// </summary>
    /// <param name="options">The diff options.</param>
    /// <returns>The effective whitespace mode to use.</returns>
    private static WhitespaceMode ResolveWhitespaceMode(DiffOptions options)
    {
        // If WhitespaceMode is explicitly set to something other than default, use it
        if (options.WhitespaceMode != WhitespaceMode.Exact)
        {
            return options.WhitespaceMode;
        }

        // Backward compatibility: map IgnoreWhitespace to IgnoreLeadingTrailing
        // This matches the previous DiffPlex ignoreWhitespace=true behavior
        if (options.IgnoreWhitespace)
        {
            return WhitespaceMode.IgnoreLeadingTrailing;
        }

        return WhitespaceMode.Exact;
    }

    /// <summary>
    /// Prepares content for comparison based on the whitespace mode.
    /// </summary>
    /// <param name="oldContent">The original content.</param>
    /// <param name="newContent">The new content.</param>
    /// <param name="mode">The whitespace handling mode.</param>
    /// <param name="filePath">The file path for language-aware processing.</param>
    /// <returns>A tuple of (processedOld, processedNew, diffPlexIgnoreWhitespace).</returns>
    private static (string ProcessedOld, string ProcessedNew, bool DiffPlexIgnoreWs) PrepareContentForComparison(
        string oldContent,
        string newContent,
        WhitespaceMode mode,
        string? filePath)
    {
        return mode switch
        {
            WhitespaceMode.Exact => (oldContent, newContent, false),
            WhitespaceMode.IgnoreLeadingTrailing => (oldContent, newContent, true),
            WhitespaceMode.IgnoreAll => (CollapseWhitespace(oldContent), CollapseWhitespace(newContent), false),
            WhitespaceMode.LanguageAware => HandleLanguageAware(oldContent, newContent, filePath),
            _ => (oldContent, newContent, false)
        };
    }

    /// <summary>
    /// Handles language-aware whitespace processing based on the file type.
    /// </summary>
    /// <param name="oldContent">The original content.</param>
    /// <param name="newContent">The new content.</param>
    /// <param name="filePath">The file path for language classification.</param>
    /// <returns>A tuple of (processedOld, processedNew, diffPlexIgnoreWhitespace).</returns>
    private static (string ProcessedOld, string ProcessedNew, bool DiffPlexIgnoreWs) HandleLanguageAware(
        string oldContent,
        string newContent,
        string? filePath)
    {
        var sensitivity = LanguageClassifier.GetSensitivity(filePath);

        return sensitivity switch
        {
            // Whitespace-significant languages: preserve exact whitespace
            WhitespaceSensitivity.Significant => (oldContent, newContent, false),
            // Brace languages: safe to ignore leading/trailing whitespace
            WhitespaceSensitivity.Insignificant => (oldContent, newContent, true),
            // Unknown: preserve exact whitespace for safety
            WhitespaceSensitivity.Unknown => (oldContent, newContent, false),
            _ => (oldContent, newContent, false)
        };
    }

    /// <summary>
    /// Collapses all whitespace in content: multiple spaces/tabs become single space, leading/trailing trimmed per line.
    /// </summary>
    /// <param name="content">The content to process.</param>
    /// <returns>The content with collapsed whitespace.</returns>
    private static string CollapseWhitespace(string content)
    {
        // Process line by line to preserve line structure
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            // Remove carriage returns and collapse whitespace
            var line = lines[i].TrimEnd('\r');
            // Collapse multiple whitespace characters to single space
            line = Regex.Replace(line, @"\s+", " ");
            // Trim leading and trailing whitespace
            lines[i] = line.Trim();
        }
        return string.Join("\n", lines);
    }
}
