namespace RoslynDiff.Output;

/// <summary>
/// Options for controlling diff output formatting.
/// </summary>
public record OutputOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use colored output.
    /// </summary>
    public bool UseColor { get; init; }

    /// <summary>
    /// Gets or sets the number of context lines to show around changes.
    /// </summary>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to include statistics in the output.
    /// </summary>
    public bool IncludeStats { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use compact output format.
    /// </summary>
    public bool Compact { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON output should be indented.
    /// </summary>
    public bool IndentJson { get; init; } = true;
}
