namespace RoslynDiff.Core.Models;

/// <summary>
/// Represents a single change detected during a diff operation.
/// </summary>
/// <remarks>
/// Changes can be hierarchical, with nested changes represented via the <see cref="Children"/> property.
/// For example, a modified class may contain child changes for individual methods or properties.
/// </remarks>
public record Change
{
    /// <summary>
    /// Gets the type of change (Added, Removed, Modified, Moved, Renamed, Unchanged).
    /// </summary>
    public ChangeType Type { get; init; }

    /// <summary>
    /// Gets the kind of code element affected (Class, Method, Property, Statement, Line, etc.).
    /// </summary>
    public ChangeKind Kind { get; init; }

    /// <summary>
    /// Gets the name of the symbol affected by this change, if applicable.
    /// </summary>
    /// <remarks>
    /// This is populated for semantic diffs where the changed element has a name (e.g., class, method, property).
    /// For line-based diffs, this may be <c>null</c>.
    /// For renamed elements, this is the new name.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the original name of the symbol before it was renamed.
    /// </summary>
    /// <remarks>
    /// This is only populated for <see cref="ChangeType.Renamed"/> changes.
    /// For all other change types, this will be <c>null</c>.
    /// </remarks>
    public string? OldName { get; init; }

    /// <summary>
    /// Gets the location of this element in the old (original) content.
    /// </summary>
    /// <remarks>
    /// This is <c>null</c> for newly added elements.
    /// </remarks>
    public Location? OldLocation { get; init; }

    /// <summary>
    /// Gets the location of this element in the new content.
    /// </summary>
    /// <remarks>
    /// This is <c>null</c> for removed elements.
    /// </remarks>
    public Location? NewLocation { get; init; }

    /// <summary>
    /// Gets the original content of the changed element.
    /// </summary>
    /// <remarks>
    /// For added elements, this is <c>null</c>.
    /// </remarks>
    public string? OldContent { get; init; }

    /// <summary>
    /// Gets the new content of the changed element.
    /// </summary>
    /// <remarks>
    /// For removed elements, this is <c>null</c>.
    /// </remarks>
    public string? NewContent { get; init; }

    /// <summary>
    /// Gets the nested child changes within this change.
    /// </summary>
    /// <remarks>
    /// This allows for hierarchical representation of changes. For example, a modified class
    /// can have child changes representing modified methods, properties, or fields within it.
    /// </remarks>
    public IReadOnlyList<Change>? Children { get; init; }

    /// <summary>
    /// Gets the impact level of this change.
    /// </summary>
    /// <remarks>
    /// Indicates whether this change affects public API, internal API, or is non-breaking.
    /// </remarks>
    public ChangeImpact Impact { get; init; }

    /// <summary>
    /// Gets the visibility/accessibility of the affected symbol.
    /// </summary>
    /// <remarks>
    /// This is <c>null</c> for changes that don't involve symbols with visibility modifiers
    /// (e.g., line-based diffs or certain statement-level changes).
    /// </remarks>
    public Visibility? Visibility { get; init; }

    /// <summary>
    /// Gets any caveats or warnings about the impact classification.
    /// </summary>
    /// <remarks>
    /// Caveats provide additional context about the impact analysis, such as cases where
    /// the impact might be understated or where further review is recommended.
    /// </remarks>
    public IReadOnlyList<string>? Caveats { get; init; }
}
