namespace RoslynDiff.Core.Models;

/// <summary>
/// Specifies the type of change detected for a code element.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// The element was added in the new version.
    /// </summary>
    Added,

    /// <summary>
    /// The element was removed from the old version.
    /// </summary>
    Removed,

    /// <summary>
    /// The element exists in both versions but has been modified.
    /// </summary>
    Modified,

    /// <summary>
    /// The element was moved to a different location without content changes.
    /// </summary>
    Moved,

    /// <summary>
    /// The element was renamed (the symbol name changed).
    /// </summary>
    Renamed,

    /// <summary>
    /// The element is unchanged between versions.
    /// </summary>
    Unchanged
}
