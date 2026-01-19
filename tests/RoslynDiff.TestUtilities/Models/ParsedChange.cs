namespace RoslynDiff.TestUtilities.Models;

using RoslynDiff.TestUtilities.Comparers;

/// <summary>
/// Represents a single parsed change from a diff output format.
/// </summary>
public record ParsedChange
{
    /// <summary>
    /// Gets the type of change (added, removed, modified, moved, renamed).
    /// </summary>
    public string ChangeType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the kind of code element (class, method, property, etc.).
    /// </summary>
    public string? Kind { get; init; }

    /// <summary>
    /// Gets the name of the changed element.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the line range in the new/current version of the file.
    /// </summary>
    public LineRange? LineRange { get; init; }

    /// <summary>
    /// Gets the line range in the old/previous version of the file.
    /// </summary>
    public LineRange? OldLineRange { get; init; }

    /// <summary>
    /// Gets the content of the change.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Gets the old content (for modified changes).
    /// </summary>
    public string? OldContent { get; init; }

    /// <summary>
    /// Gets the child changes (for hierarchical structures).
    /// </summary>
    public IReadOnlyList<ParsedChange> Children { get; init; } = Array.Empty<ParsedChange>();

    /// <summary>
    /// Gets additional metadata about the change.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Returns a string representation of this change.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string> { ChangeType };

        if (!string.IsNullOrEmpty(Kind))
        {
            parts.Add(Kind);
        }

        if (!string.IsNullOrEmpty(Name))
        {
            parts.Add(Name);
        }

        if (LineRange != null)
        {
            parts.Add($"({LineRange})");
        }

        return string.Join(" ", parts);
    }
}
