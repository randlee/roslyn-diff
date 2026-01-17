namespace RoslynDiff.Core.Differ;

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
    public DiffResult Compare(string oldContent, string newContent, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldContent);
        ArgumentNullException.ThrowIfNull(newContent);
        ArgumentNullException.ThrowIfNull(options);

        var diffBuilder = new InlineDiffBuilder(_differ);
        var diff = diffBuilder.BuildDiffModel(oldContent, newContent, options.IgnoreWhitespace);

        var changes = BuildChanges(diff, options);
        var stats = CalculateStats(changes);

        return new DiffResult
        {
            OldPath = options.OldPath,
            NewPath = options.NewPath,
            Mode = DiffMode.Line,
            FileChanges = [new FileChange
            {
                Path = options.NewPath ?? options.OldPath,
                Changes = changes
            }],
            Stats = stats
        };
    }

    private static List<Change> BuildChanges(DiffPaneModel diff, DiffOptions options)
    {
        // Filter out trailing empty lines that result from files ending with newlines
        // This matches standard diff behavior which doesn't count trailing newlines as lines
        var diffLines = diff.Lines.ToList();
        while (diffLines.Count > 0 && string.IsNullOrEmpty(diffLines[^1].Text))
        {
            diffLines.RemoveAt(diffLines.Count - 1);
        }

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
                    FlushPendingChanges(changes, pendingDeletions, pendingAdditions);
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
            FlushPendingChanges(changes, pendingDeletions, pendingAdditions);
        }

        return changes;
    }

    private static void FlushPendingChanges(
        List<Change> changes,
        List<(DiffPiece Piece, int? OldLine, int? NewLine)> deletions,
        List<(DiffPiece Piece, int? OldLine, int? NewLine)> additions)
    {
        // Add all deletions first
        foreach (var (piece, oldLine, newLine) in deletions)
        {
            changes.Add(new Change
            {
                Type = Models.ChangeType.Removed,
                Kind = ChangeKind.Line,
                OldContent = piece.Text,
                NewContent = null,
                OldLocation = oldLine.HasValue ? new Location { StartLine = oldLine.Value, EndLine = oldLine.Value } : null,
                NewLocation = null
            });
        }

        // Then add all additions
        foreach (var (piece, oldLine, newLine) in additions)
        {
            changes.Add(new Change
            {
                Type = Models.ChangeType.Added,
                Kind = ChangeKind.Line,
                OldContent = null,
                NewContent = piece.Text,
                OldLocation = null,
                NewLocation = newLine.HasValue ? new Location { StartLine = newLine.Value, EndLine = newLine.Value } : null
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
}
