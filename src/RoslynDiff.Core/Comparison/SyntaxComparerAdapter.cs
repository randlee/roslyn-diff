namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.Syntax;

/// <summary>
/// Adapts <see cref="SyntaxComparer"/> to implement <see cref="ISyntaxComparer"/>.
/// </summary>
/// <remarks>
/// This adapter bridges the gap between the SyntaxComparer implementation and the
/// ISyntaxComparer interface used by RoslynDifferBase.
/// </remarks>
public sealed class SyntaxComparerAdapter : ISyntaxComparer
{
    private readonly SyntaxComparer _comparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxComparerAdapter"/> class.
    /// </summary>
    public SyntaxComparerAdapter()
    {
        _comparer = new SyntaxComparer();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxComparerAdapter"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The syntax comparer to adapt.</param>
    public SyntaxComparerAdapter(SyntaxComparer comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    /// <inheritdoc/>
    public IReadOnlyList<SyntaxChange> Compare(SyntaxNode oldRoot, SyntaxNode newRoot, SyntaxCompareOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldRoot);
        ArgumentNullException.ThrowIfNull(newRoot);
        ArgumentNullException.ThrowIfNull(options);

        // Convert options
        var diffOptions = new DiffOptions
        {
            IgnoreWhitespace = options.IgnoreWhitespace,
            IgnoreComments = options.IgnoreComments
        };

        // Get syntax trees from roots
        var oldTree = oldRoot.SyntaxTree;
        var newTree = newRoot.SyntaxTree;

        // Compare using the underlying comparer
        var changes = _comparer.Compare(oldTree, newTree, diffOptions);

        // Convert results to SyntaxChange
        return ConvertChanges(changes);
    }

    private static IReadOnlyList<SyntaxChange> ConvertChanges(IReadOnlyList<Change> changes)
    {
        var result = new List<SyntaxChange>();

        foreach (var change in changes)
        {
            result.Add(ConvertChange(change));
        }

        return result;
    }

    private static SyntaxChange ConvertChange(Change change)
    {
        var changeType = change.Type switch
        {
            ChangeType.Added => SyntaxChangeType.Added,
            ChangeType.Removed => SyntaxChangeType.Removed,
            ChangeType.Modified => SyntaxChangeType.Modified,
            ChangeType.Moved => SyntaxChangeType.Moved,
            ChangeType.Renamed => SyntaxChangeType.Renamed,
            _ => SyntaxChangeType.Unchanged
        };

        IReadOnlyList<SyntaxChange>? children = null;
        if (change.Children is { Count: > 0 })
        {
            children = ConvertChanges(change.Children);
        }

        // Note: We don't have access to the original SyntaxNodes here,
        // so OldNode and NewNode will be null. The content is preserved
        // in the Change object but the node references are lost in this adapter.
        return new SyntaxChange
        {
            ChangeType = changeType,
            OldNode = null,
            NewNode = null,
            Children = children
        };
    }
}
