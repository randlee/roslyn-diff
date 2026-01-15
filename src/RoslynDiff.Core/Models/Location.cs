namespace RoslynDiff.Core.Models;

/// <summary>
/// Represents a location span within a source file.
/// </summary>
public record Location
{
    /// <summary>
    /// Gets the file path where this location exists.
    /// </summary>
    public string? File { get; init; }

    /// <summary>
    /// Gets the 1-based starting line number.
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// Gets the 1-based ending line number.
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// Gets the 1-based starting column number.
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// Gets the 1-based ending column number.
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// Gets the number of lines spanned by this location.
    /// </summary>
    public int LineCount => EndLine - StartLine + 1;

    /// <summary>
    /// Determines whether this location contains the specified line.
    /// </summary>
    /// <param name="line">The 1-based line number to check.</param>
    /// <returns><c>true</c> if the location spans the specified line; otherwise, <c>false</c>.</returns>
    public bool ContainsLine(int line) => line >= StartLine && line <= EndLine;
}
