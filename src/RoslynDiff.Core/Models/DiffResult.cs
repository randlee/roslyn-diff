namespace RoslynDiff.Core.Models;

/// <summary>
/// Represents the complete result of a diff operation between two versions of content.
/// </summary>
/// <remarks>
/// Contains all file changes detected during the comparison along with aggregate statistics.
/// </remarks>
public record DiffResult
{
    /// <summary>
    /// Gets the path to the old (original) file being compared.
    /// </summary>
    public string? OldPath { get; init; }

    /// <summary>
    /// Gets the path to the new file being compared.
    /// </summary>
    public string? NewPath { get; init; }

    /// <summary>
    /// Gets the diff mode used for this comparison (Roslyn semantic or Line-based).
    /// </summary>
    public DiffMode Mode { get; init; }

    /// <summary>
    /// Gets the collection of file changes detected during the comparison.
    /// </summary>
    public IReadOnlyList<FileChange> FileChanges { get; init; } = [];

    /// <summary>
    /// Gets the aggregate statistics for all changes in this diff result.
    /// </summary>
    public DiffStats Stats { get; init; } = new();
}

/// <summary>
/// Represents all changes detected within a single file.
/// </summary>
public record FileChange
{
    /// <summary>
    /// Gets the path to the file containing the changes.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets the collection of individual changes within this file.
    /// </summary>
    public IReadOnlyList<Change> Changes { get; init; } = [];
}

/// <summary>
/// Provides aggregate statistics about the changes in a diff result.
/// </summary>
public record DiffStats
{
    /// <summary>
    /// Gets the total number of changes across all categories.
    /// Computed as the sum of Additions, Deletions, Modifications, Moves, and Renames.
    /// </summary>
    public int TotalChanges => Additions + Deletions + Modifications + Moves + Renames;

    /// <summary>
    /// Gets the number of additions (new code or lines).
    /// </summary>
    public int Additions { get; init; }

    /// <summary>
    /// Gets the number of deletions (removed code or lines).
    /// </summary>
    public int Deletions { get; init; }

    /// <summary>
    /// Gets the number of modifications (changed code or lines).
    /// </summary>
    public int Modifications { get; init; }

    /// <summary>
    /// Gets the number of code elements that were moved to a different location.
    /// </summary>
    public int Moves { get; init; }

    /// <summary>
    /// Gets the number of renamed symbols or elements.
    /// </summary>
    public int Renames { get; init; }
}

/// <summary>
/// Specifies the diff algorithm mode used for comparison.
/// </summary>
public enum DiffMode
{
    /// <summary>
    /// Roslyn-based semantic diff that understands code structure.
    /// </summary>
    Roslyn,

    /// <summary>
    /// Traditional line-by-line text comparison.
    /// </summary>
    Line
}
