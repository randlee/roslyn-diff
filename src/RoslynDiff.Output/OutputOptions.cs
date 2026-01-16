namespace RoslynDiff.Output;

/// <summary>
/// Options for controlling diff output formatting.
/// </summary>
public record OutputOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include the content of changed elements.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the output will include the source code content for each change.
    /// When <c>false</c>, only metadata (type, kind, name, location) will be included.
    /// </remarks>
    public bool IncludeContent { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to pretty-print JSON output (with indentation).
    /// </summary>
    public bool PrettyPrint { get; init; } = true;

    /// <summary>
    /// Gets or sets the number of context lines to show around changes.
    /// </summary>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to use colored output.
    /// </summary>
    public bool UseColor { get; init; }

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
    [Obsolete("Use PrettyPrint instead.")]
    public bool IndentJson { get => PrettyPrint; init => PrettyPrint = value; }

    /// <summary>
    /// Gets or sets the list of available editors detected on the system.
    /// Used by HTML formatter to conditionally show editor buttons.
    /// </summary>
    public IReadOnlyList<string> AvailableEditors { get; init; } = Array.Empty<string>();
}
