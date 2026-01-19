namespace RoslynDiff.TestUtilities.Models;

/// <summary>
/// Represents the complete parsed result from any diff output format.
/// This is the common structure returned by all parsers.
/// </summary>
public record ParsedDiffResult
{
    /// <summary>
    /// Gets the format that was parsed (json, html, text, unified-diff).
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp from the output (if available).
    /// </summary>
    public DateTime? Timestamp { get; init; }

    /// <summary>
    /// Gets the old file path.
    /// </summary>
    public string? OldPath { get; init; }

    /// <summary>
    /// Gets the new file path.
    /// </summary>
    public string? NewPath { get; init; }

    /// <summary>
    /// Gets the tool/mode used to generate the diff.
    /// </summary>
    public string? Mode { get; init; }

    /// <summary>
    /// Gets the version of the tool that generated the output.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the collection of all changes extracted from the output.
    /// </summary>
    public IReadOnlyList<ParsedChange> Changes { get; init; } = Array.Empty<ParsedChange>();

    /// <summary>
    /// Gets summary statistics about the changes.
    /// </summary>
    public ParsedSummary? Summary { get; init; }

    /// <summary>
    /// Gets additional metadata extracted from the output.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Gets any parsing errors or warnings encountered.
    /// </summary>
    public IReadOnlyList<string> ParsingErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets a value indicating whether the parsing was successful.
    /// </summary>
    public bool IsValid => ParsingErrors.Count == 0;

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    public override string ToString()
    {
        return $"{Format} - {Changes.Count} changes" +
               (ParsingErrors.Any() ? $" ({ParsingErrors.Count} errors)" : "");
    }
}

/// <summary>
/// Represents summary statistics from a diff output.
/// </summary>
public record ParsedSummary
{
    /// <summary>
    /// Gets the total number of changes.
    /// </summary>
    public int TotalChanges { get; init; }

    /// <summary>
    /// Gets the number of additions.
    /// </summary>
    public int Additions { get; init; }

    /// <summary>
    /// Gets the number of deletions.
    /// </summary>
    public int Deletions { get; init; }

    /// <summary>
    /// Gets the number of modifications.
    /// </summary>
    public int Modifications { get; init; }

    /// <summary>
    /// Gets the number of renames.
    /// </summary>
    public int Renames { get; init; }

    /// <summary>
    /// Gets the number of moves.
    /// </summary>
    public int Moves { get; init; }

    /// <summary>
    /// Returns a string representation of this summary.
    /// </summary>
    public override string ToString()
    {
        return $"Total: {TotalChanges}, +{Additions}, -{Deletions}, ~{Modifications}";
    }
}
