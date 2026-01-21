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
    /// Gets the Target Framework Monikers (TFMs) that were analyzed during this diff operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property indicates which target frameworks were analyzed when comparing multi-targeted projects.
    /// The value interpretation is as follows:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>null</c> - No TFM analysis was performed. This is the default for projects that are
    /// not multi-targeted or when TFM analysis is not requested via <see cref="DiffOptions.TargetFrameworks"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// List with values - The specified TFMs were analyzed during the diff operation. For example,
    /// <c>["net8.0", "net10.0"]</c> indicates both .NET 8.0 and .NET 10.0 were analyzed.
    /// Individual <see cref="Change"/> objects may specify which TFMs they apply to via
    /// <see cref="Change.ApplicableToTfms"/>.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// When performing TFM analysis, the differ compiles the project for each specified framework
    /// and identifies changes that are specific to certain TFMs (e.g., due to conditional compilation
    /// or framework-specific APIs).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Result from analyzing both .NET 8.0 and .NET 10.0
    /// var result = new DiffResult
    /// {
    ///     Mode = DiffMode.Roslyn,
    ///     AnalyzedTfms = new[] { "net8.0", "net10.0" },
    ///     FileChanges = fileChanges
    /// };
    /// </code>
    /// </example>
    public IReadOnlyList<string>? AnalyzedTfms { get; init; }

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

    // Impact breakdown statistics

    /// <summary>
    /// Gets the number of breaking public API changes.
    /// </summary>
    public int BreakingPublicApiCount { get; init; }

    /// <summary>
    /// Gets the number of breaking internal API changes.
    /// </summary>
    public int BreakingInternalApiCount { get; init; }

    /// <summary>
    /// Gets the number of non-breaking changes.
    /// </summary>
    public int NonBreakingCount { get; init; }

    /// <summary>
    /// Gets the number of formatting-only changes.
    /// </summary>
    public int FormattingOnlyCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are any breaking changes (public or internal API).
    /// </summary>
    public bool HasBreakingChanges => BreakingPublicApiCount > 0 || BreakingInternalApiCount > 0;

    /// <summary>
    /// Gets a value indicating whether the changes require review.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> if there are any changes that are not purely formatting-only.
    /// </remarks>
    public bool RequiresReview => BreakingPublicApiCount > 0 || BreakingInternalApiCount > 0 || NonBreakingCount > 0;
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
