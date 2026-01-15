namespace RoslynDiff.Core.Models;

/// <summary>
/// Configuration options for controlling diff behavior.
/// </summary>
public record DiffOptions
{
    /// <summary>
    /// Gets the diff mode to use.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the differ will automatically select the appropriate mode
    /// based on the file type and content.
    /// </remarks>
    public DiffMode? Mode { get; init; }

    /// <summary>
    /// Gets a value indicating whether whitespace differences should be ignored.
    /// </summary>
    public bool IgnoreWhitespace { get; init; }

    /// <summary>
    /// Gets a value indicating whether comment differences should be ignored.
    /// </summary>
    /// <remarks>
    /// This option is only applicable when using Roslyn semantic diff mode.
    /// </remarks>
    public bool IgnoreComments { get; init; }

    /// <summary>
    /// Gets the number of unchanged context lines to include around each change.
    /// </summary>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// Gets the path to the old (original) file being compared.
    /// </summary>
    /// <remarks>
    /// This is used for display purposes and to determine the file type for automatic mode selection.
    /// </remarks>
    public string? OldPath { get; init; }

    /// <summary>
    /// Gets the path to the new file being compared.
    /// </summary>
    /// <remarks>
    /// This is used for display purposes and to determine the file type for automatic mode selection.
    /// </remarks>
    public string? NewPath { get; init; }
}
