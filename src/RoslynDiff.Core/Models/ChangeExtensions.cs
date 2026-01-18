namespace RoslynDiff.Core.Models;

/// <summary>
/// Extension methods for working with hierarchical Change structures.
/// </summary>
public static class ChangeExtensions
{
    /// <summary>
    /// Flattens a hierarchical change tree into a single-level enumerable.
    /// Useful for backward compatibility or simple change counting.
    /// </summary>
    /// <param name="changes">The hierarchical changes to flatten.</param>
    /// <returns>All changes including nested children, depth-first.</returns>
    public static IEnumerable<Change> Flatten(this IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            yield return change;

            if (change.Children is not null)
            {
                foreach (var child in change.Children.Flatten())
                {
                    yield return child;
                }
            }
        }
    }

    /// <summary>
    /// Counts all changes including nested children.
    /// </summary>
    public static int CountAll(this IEnumerable<Change> changes)
    {
        return changes.Flatten().Count();
    }

    /// <summary>
    /// Finds a change by name at any nesting level.
    /// </summary>
    public static Change? FindByName(this IEnumerable<Change> changes, string name)
    {
        return changes.Flatten().FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// Gets all changes of a specific kind at any nesting level.
    /// </summary>
    public static IEnumerable<Change> OfKind(this IEnumerable<Change> changes, ChangeKind kind)
    {
        return changes.Flatten().Where(c => c.Kind == kind);
    }
}
