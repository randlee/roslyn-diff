namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Models;

/// <summary>
/// Performs semantic comparison to detect renames and moves.
/// </summary>
/// <remarks>
/// The SemanticComparer analyzes changes from the SyntaxComparer to detect
/// patterns that indicate renames (same body, different name) or moves
/// (same code moved to different location/parent).
/// </remarks>
public sealed class SemanticComparer
{
    private readonly SymbolMatcher _matcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticComparer"/> class.
    /// </summary>
    public SemanticComparer()
    {
        _matcher = new SymbolMatcher();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticComparer"/> class
    /// with a custom symbol matcher.
    /// </summary>
    /// <param name="matcher">The symbol matcher to use.</param>
    internal SemanticComparer(SymbolMatcher matcher)
    {
        _matcher = matcher;
    }

    /// <summary>
    /// Analyzes changes from SyntaxComparer to detect renames and moves.
    /// </summary>
    /// <param name="syntaxChanges">The list of changes from the SyntaxComparer.</param>
    /// <param name="oldTree">The original syntax tree.</param>
    /// <param name="newTree">The new syntax tree.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <returns>An enhanced list of changes with renames and moves detected.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public IReadOnlyList<Change> EnhanceWithSemantics(
        IReadOnlyList<Change> syntaxChanges,
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(syntaxChanges);
        ArgumentNullException.ThrowIfNull(oldTree);
        ArgumentNullException.ThrowIfNull(newTree);
        ArgumentNullException.ThrowIfNull(options);

        // Flatten all changes (including nested children) to find all Added/Removed
        var allChanges = FlattenChanges(syntaxChanges);

        // Separate changes by type
        var removedChanges = allChanges
            .Where(c => c.Type == ChangeType.Removed)
            .ToList();

        var addedChanges = allChanges
            .Where(c => c.Type == ChangeType.Added)
            .ToList();

        if (removedChanges.Count == 0 || addedChanges.Count == 0)
        {
            // No possibility of renames or moves
            return syntaxChanges;
        }

        // Track which changes have been matched and their replacements
        var replacements = new Dictionary<Change, Change>();

        // Detect renames first (higher priority than moves)
        DetectRenames(
            removedChanges,
            addedChanges,
            oldTree,
            newTree,
            replacements);

        // Detect moves from remaining unmatched changes
        var matchedChanges = new HashSet<Change>(replacements.Keys);
        var remainingRemoved = removedChanges.Where(c => !matchedChanges.Contains(c)).ToList();
        var remainingAdded = addedChanges.Where(c => !replacements.Values.Any(v => v == c || 
            (v.OldLocation == c.NewLocation && v.Kind == c.Kind && v.Name == c.Name))).ToList();

        DetectMoves(
            remainingRemoved,
            remainingAdded,
            oldTree,
            newTree,
            replacements);

        // Rebuild the change tree with replacements applied
        var result = RebuildChangesWithReplacements(syntaxChanges, replacements);

        return result;
    }

    /// <summary>
    /// Analyzes changes from SyntaxComparer using source code strings.
    /// </summary>
    /// <param name="syntaxChanges">The list of changes from the SyntaxComparer.</param>
    /// <param name="oldSource">The original C# source code.</param>
    /// <param name="newSource">The new C# source code.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <returns>An enhanced list of changes with renames and moves detected.</returns>
    public IReadOnlyList<Change> EnhanceWithSemantics(
        IReadOnlyList<Change> syntaxChanges,
        string oldSource,
        string newSource,
        DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(syntaxChanges);
        ArgumentNullException.ThrowIfNull(oldSource);
        ArgumentNullException.ThrowIfNull(newSource);
        ArgumentNullException.ThrowIfNull(options);

        var oldTree = CSharpSyntaxTree.ParseText(oldSource, path: options.OldPath ?? string.Empty);
        var newTree = CSharpSyntaxTree.ParseText(newSource, path: options.NewPath ?? string.Empty);

        return EnhanceWithSemantics(syntaxChanges, oldTree, newTree, options);
    }

    /// <summary>
    /// Flattens a hierarchical change list into a flat list of all changes.
    /// </summary>
    private static List<Change> FlattenChanges(IReadOnlyList<Change> changes)
    {
        var result = new List<Change>();
        FlattenChangesRecursive(changes, result);
        return result;
    }

    private static void FlattenChangesRecursive(IReadOnlyList<Change>? changes, List<Change> result)
    {
        if (changes is null)
            return;

        foreach (var change in changes)
        {
            result.Add(change);
            FlattenChangesRecursive(change.Children, result);
        }
    }

    /// <summary>
    /// Rebuilds the change tree, applying replacements for matched Added/Removed pairs.
    /// </summary>
    private static IReadOnlyList<Change> RebuildChangesWithReplacements(
        IReadOnlyList<Change> changes,
        Dictionary<Change, Change> replacements)
    {
        var result = new List<Change>();
        var processedAdded = new HashSet<Change>();

        // Flatten all changes to search for corresponding Added changes
        var allChangesFlat = FlattenChanges(changes);

        // Track which Added changes were part of a rename/move
        foreach (var kvp in replacements)
        {
            // The value contains info about both old (removed) and new (added)
            // We need to track which Added changes to skip
            var addedChange = FindCorrespondingAdded(kvp.Value, allChangesFlat);
            if (addedChange is not null)
            {
                processedAdded.Add(addedChange);
            }
        }

        foreach (var change in changes)
        {
            var rebuilt = RebuildChangeWithReplacements(change, replacements, processedAdded);
            if (rebuilt is not null)
            {
                result.Add(rebuilt);
            }
        }

        return result;
    }

    private static Change? FindCorrespondingAdded(Change semanticChange, IReadOnlyList<Change> allChanges)
    {
        // Find the Added change that corresponds to this semantic change's new location
        return allChanges.FirstOrDefault(c =>
            c.Type == ChangeType.Added &&
            c.Kind == semanticChange.Kind &&
            c.Name == semanticChange.Name &&
            c.NewLocation?.StartLine == semanticChange.NewLocation?.StartLine);
    }

    private static Change? RebuildChangeWithReplacements(
        Change change,
        Dictionary<Change, Change> replacements,
        HashSet<Change> processedAdded)
    {
        // Check if this change should be replaced
        if (replacements.TryGetValue(change, out var replacement))
        {
            // Return the semantic change (Renamed or Moved) instead
            return RebuildChildrenIfNeeded(replacement, replacements, processedAdded);
        }

        // Check if this is an Added change that was part of a rename/move
        if (change.Type == ChangeType.Added && processedAdded.Contains(change))
        {
            // Skip this change as it's been converted to a rename/move
            return null;
        }

        // Rebuild children
        List<Change>? newChildren = null;
        if (change.Children is not null && change.Children.Count > 0)
        {
            newChildren = [];
            foreach (var child in change.Children)
            {
                var rebuiltChild = RebuildChangeWithReplacements(child, replacements, processedAdded);
                if (rebuiltChild is not null)
                {
                    newChildren.Add(rebuiltChild);
                }
            }
        }

        // If children changed, create new change with new children
        if (newChildren is not null && !ChildrenEqual(change.Children, newChildren))
        {
            return change with { Children = newChildren.Count > 0 ? newChildren : null };
        }

        return change;
    }

    private static Change RebuildChildrenIfNeeded(
        Change change,
        Dictionary<Change, Change> replacements,
        HashSet<Change> processedAdded)
    {
        if (change.Children is null || change.Children.Count == 0)
            return change;

        var newChildren = new List<Change>();
        foreach (var child in change.Children)
        {
            var rebuiltChild = RebuildChangeWithReplacements(child, replacements, processedAdded);
            if (rebuiltChild is not null)
            {
                newChildren.Add(rebuiltChild);
            }
        }

        return change with { Children = newChildren.Count > 0 ? newChildren : null };
    }

    private static bool ChildrenEqual(IReadOnlyList<Change>? a, List<Change>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!ReferenceEquals(a[i], b[i]))
                return false;
        }
        return true;
    }

    private void DetectRenames(
        IReadOnlyList<Change> removedChanges,
        IReadOnlyList<Change> addedChanges,
        SyntaxTree oldTree,
        SyntaxTree newTree,
        Dictionary<Change, Change> replacements)
    {
        // Find rename candidates
        var candidates = _matcher.FindRenameMatches(removedChanges, addedChanges, oldTree, newTree);

        // Track which changes have been matched
        var matchedRemoved = new HashSet<Change>();
        var matchedAdded = new HashSet<Change>();

        // Process candidates in order of similarity
        foreach (var candidate in candidates)
        {
            // Skip if either change is already matched
            if (matchedRemoved.Contains(candidate.RemovedChange) ||
                matchedAdded.Contains(candidate.AddedChange))
            {
                continue;
            }

            // Create a Renamed change
            var renamedChange = new Change
            {
                Type = ChangeType.Renamed,
                Kind = candidate.RemovedChange.Kind,
                Name = candidate.AddedChange.Name,
                OldName = candidate.RemovedChange.Name,
                OldLocation = candidate.RemovedChange.OldLocation,
                NewLocation = candidate.AddedChange.NewLocation,
                OldContent = candidate.RemovedChange.OldContent,
                NewContent = candidate.AddedChange.NewContent
            };

            replacements[candidate.RemovedChange] = renamedChange;
            matchedRemoved.Add(candidate.RemovedChange);
            matchedAdded.Add(candidate.AddedChange);
        }
    }

    private void DetectMoves(
        IReadOnlyList<Change> removedChanges,
        IReadOnlyList<Change> addedChanges,
        SyntaxTree oldTree,
        SyntaxTree newTree,
        Dictionary<Change, Change> replacements)
    {
        // Find move candidates
        var candidates = _matcher.FindMoveMatches(removedChanges, addedChanges, oldTree, newTree);

        // Track which changes have been matched
        var matchedRemoved = new HashSet<Change>(replacements.Keys);
        var matchedAdded = new HashSet<Change>();

        // Process candidates
        foreach (var candidate in candidates)
        {
            // Skip if either change is already matched
            if (matchedRemoved.Contains(candidate.RemovedChange) ||
                matchedAdded.Contains(candidate.AddedChange))
            {
                continue;
            }

            // Create a Moved change
            var movedChange = new Change
            {
                Type = ChangeType.Moved,
                Kind = candidate.RemovedChange.Kind,
                Name = candidate.RemovedChange.Name,
                OldLocation = candidate.RemovedChange.OldLocation,
                NewLocation = candidate.AddedChange.NewLocation,
                OldContent = candidate.RemovedChange.OldContent,
                NewContent = candidate.AddedChange.NewContent
            };

            replacements[candidate.RemovedChange] = movedChange;
            matchedRemoved.Add(candidate.RemovedChange);
            matchedAdded.Add(candidate.AddedChange);
        }
    }
}
