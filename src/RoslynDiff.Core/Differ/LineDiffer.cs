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
        var changes = new List<Change>();
        var lineNumber = 0;
        var contextBefore = new List<DiffPiece>();
        var pendingChanges = new List<DiffPiece>();

        foreach (var line in diff.Lines)
        {
            lineNumber++;

            switch (line.Type)
            {
                case DiffPlexChangeType.Unchanged:
                    // If we have pending changes, flush them with context
                    if (pendingChanges.Count > 0)
                    {
                        FlushChanges(changes, contextBefore, pendingChanges, options.ContextLines, lineNumber);
                        pendingChanges.Clear();
                        contextBefore.Clear();
                    }

                    // Keep track of context lines
                    contextBefore.Add(line);
                    if (contextBefore.Count > options.ContextLines)
                    {
                        contextBefore.RemoveAt(0);
                    }
                    break;

                case DiffPlexChangeType.Inserted:
                case DiffPlexChangeType.Deleted:
                case DiffPlexChangeType.Modified:
                case DiffPlexChangeType.Imaginary:
                    pendingChanges.Add(line);
                    break;
            }
        }

        // Flush any remaining changes
        if (pendingChanges.Count > 0)
        {
            FlushChanges(changes, contextBefore, pendingChanges, options.ContextLines, lineNumber);
        }

        return changes;
    }

    private static void FlushChanges(
        List<Change> changes,
        List<DiffPiece> contextBefore,
        List<DiffPiece> pendingChanges,
        int contextLines,
        int currentLineNumber)
    {
        // Add context before (if any and if we want context)
        if (contextLines > 0)
        {
            var startLine = currentLineNumber - pendingChanges.Count - contextBefore.Count;
            foreach (var contextLine in contextBefore)
            {
                startLine++;
                changes.Add(new Change
                {
                    Type = Models.ChangeType.Unchanged,
                    Kind = ChangeKind.Line,
                    OldContent = contextLine.Text,
                    NewContent = contextLine.Text,
                    OldLocation = new Location { StartLine = startLine, EndLine = startLine },
                    NewLocation = new Location { StartLine = startLine, EndLine = startLine }
                });
            }
        }

        // Add the actual changes
        var changeLine = currentLineNumber - pendingChanges.Count;
        foreach (var change in pendingChanges)
        {
            changeLine++;
            var changeType = change.Type switch
            {
                DiffPlexChangeType.Inserted => Models.ChangeType.Added,
                DiffPlexChangeType.Deleted => Models.ChangeType.Removed,
                DiffPlexChangeType.Modified => Models.ChangeType.Modified,
                _ => Models.ChangeType.Unchanged
            };

            var newChange = new Change
            {
                Type = changeType,
                Kind = ChangeKind.Line,
                OldContent = change.Type == DiffPlexChangeType.Inserted ? null : change.Text,
                NewContent = change.Type == DiffPlexChangeType.Deleted ? null : change.Text,
                OldLocation = change.Type == DiffPlexChangeType.Inserted ? null : new Location { StartLine = changeLine, EndLine = changeLine },
                NewLocation = change.Type == DiffPlexChangeType.Deleted ? null : new Location { StartLine = changeLine, EndLine = changeLine }
            };

            changes.Add(newChange);
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
            TotalChanges = additions + deletions + modifications,
            Additions = additions,
            Deletions = deletions,
            Modifications = modifications,
            Moves = 0,
            Renames = 0
        };
    }
}
