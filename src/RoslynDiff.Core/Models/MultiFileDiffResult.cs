namespace RoslynDiff.Core.Models;

/// <summary>
/// Represents the result of comparing multiple files (e.g., git branch comparison or folder comparison).
/// </summary>
/// <remarks>
/// Contains a collection of individual file diffs along with aggregate statistics across all files.
/// </remarks>
public record MultiFileDiffResult
{
    /// <summary>
    /// Gets the collection of individual file diff results.
    /// </summary>
    public IReadOnlyList<FileDiffResult> Files { get; init; } = [];

    /// <summary>
    /// Gets the aggregate summary statistics for all files.
    /// </summary>
    public MultiFileSummary Summary { get; init; } = new();

    /// <summary>
    /// Gets the metadata about this multi-file comparison.
    /// </summary>
    public MultiFileMetadata Metadata { get; init; } = new();
}

/// <summary>
/// Represents the diff result for a single file within a multi-file comparison.
/// </summary>
public record FileDiffResult
{
    /// <summary>
    /// Gets the detailed diff result for this file.
    /// </summary>
    public DiffResult Result { get; init; } = new();

    /// <summary>
    /// Gets the change status of this file (modified, added, removed, renamed, unchanged).
    /// </summary>
    public FileChangeStatus Status { get; init; }

    /// <summary>
    /// Gets the path to the old version of the file.
    /// Null for added files.
    /// </summary>
    public string? OldPath { get; init; }

    /// <summary>
    /// Gets the path to the new version of the file.
    /// Null for removed files.
    /// </summary>
    public string? NewPath { get; init; }
}

/// <summary>
/// Represents the change status of a file in a multi-file comparison.
/// </summary>
public enum FileChangeStatus
{
    /// <summary>
    /// File was modified (exists in both versions with changes).
    /// </summary>
    Modified,

    /// <summary>
    /// File was added (exists only in new version).
    /// </summary>
    Added,

    /// <summary>
    /// File was removed (exists only in old version).
    /// </summary>
    Removed,

    /// <summary>
    /// File was renamed (same content, different path).
    /// </summary>
    Renamed,

    /// <summary>
    /// File exists in both versions but has no changes.
    /// </summary>
    Unchanged
}

/// <summary>
/// Provides aggregate summary statistics for a multi-file comparison.
/// </summary>
public record MultiFileSummary
{
    /// <summary>
    /// Gets the total number of files in the comparison.
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Gets the number of files that were modified.
    /// </summary>
    public int ModifiedFiles { get; init; }

    /// <summary>
    /// Gets the number of files that were added.
    /// </summary>
    public int AddedFiles { get; init; }

    /// <summary>
    /// Gets the number of files that were removed.
    /// </summary>
    public int RemovedFiles { get; init; }

    /// <summary>
    /// Gets the number of files that were renamed.
    /// </summary>
    public int RenamedFiles { get; init; }

    /// <summary>
    /// Gets the total number of changes across all files.
    /// </summary>
    public int TotalChanges { get; init; }

    /// <summary>
    /// Gets the breakdown of changes by impact category.
    /// </summary>
    public ImpactBreakdown ImpactBreakdown { get; init; } = new();
}

/// <summary>
/// Provides a breakdown of changes by their impact classification.
/// </summary>
public record ImpactBreakdown
{
    /// <summary>
    /// Gets the number of breaking public API changes.
    /// </summary>
    public int BreakingPublicApi { get; init; }

    /// <summary>
    /// Gets the number of breaking internal API changes.
    /// </summary>
    public int BreakingInternalApi { get; init; }

    /// <summary>
    /// Gets the number of non-breaking changes.
    /// </summary>
    public int NonBreaking { get; init; }

    /// <summary>
    /// Gets the number of formatting-only changes.
    /// </summary>
    public int FormattingOnly { get; init; }
}

/// <summary>
/// Contains metadata about a multi-file comparison operation.
/// </summary>
public record MultiFileMetadata
{
    /// <summary>
    /// Gets the comparison mode (e.g., "git" or "folder").
    /// </summary>
    public string Mode { get; init; } = string.Empty;

    /// <summary>
    /// Gets the git ref range for git-based comparisons (e.g., "main..feature-branch").
    /// Null for non-git comparisons.
    /// </summary>
    public string? GitRefRange { get; init; }

    /// <summary>
    /// Gets the root path of the old version for folder-based comparisons.
    /// Null for git comparisons.
    /// </summary>
    public string? OldRoot { get; init; }

    /// <summary>
    /// Gets the root path of the new version for folder-based comparisons.
    /// Null for git comparisons.
    /// </summary>
    public string? NewRoot { get; init; }

    /// <summary>
    /// Gets the timestamp when this comparison was performed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
