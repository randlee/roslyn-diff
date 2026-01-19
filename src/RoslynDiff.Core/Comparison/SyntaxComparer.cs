namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Models;

/// <summary>
/// Compares two C# syntax trees and identifies structural changes.
/// </summary>
/// <remarks>
/// <para>
/// This comparer uses Roslyn's syntax tree API to understand code structure,
/// enabling semantic-aware diffing that can identify changes at the class,
/// method, property, and field level rather than just line-by-line differences.
/// </para>
/// <para>
/// The comparison delegates to <see cref="RecursiveTreeComparer"/> which uses
/// a recursive, level-by-level algorithm where each node is processed exactly once.
/// This addresses BUG-003 (duplicate node extraction) and provides O(n) complexity
/// with early termination for identical subtrees.
/// </para>
/// </remarks>
public sealed class SyntaxComparer
{
    private readonly ITreeComparer _treeComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxComparer"/> class.
    /// </summary>
    public SyntaxComparer()
    {
        _treeComparer = new RecursiveTreeComparer();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxComparer"/> class with a custom tree comparer.
    /// </summary>
    /// <param name="treeComparer">The tree comparer to use for comparing trees.</param>
    public SyntaxComparer(ITreeComparer treeComparer)
    {
        ArgumentNullException.ThrowIfNull(treeComparer);
        _treeComparer = treeComparer;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxComparer"/> class with a custom node matcher.
    /// </summary>
    /// <param name="matcher">The node matcher to use for comparing trees.</param>
    /// <remarks>
    /// This constructor is provided for backward compatibility and testing purposes.
    /// New code should use the <see cref="SyntaxComparer(ITreeComparer)"/> constructor.
    /// </remarks>
    internal SyntaxComparer(NodeMatcher matcher)
    {
        _treeComparer = new RecursiveTreeComparer(matcher, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
    }

    /// <summary>
    /// Compares two syntax trees and returns a list of detected changes.
    /// </summary>
    /// <param name="oldTree">The original syntax tree.</param>
    /// <param name="newTree">The new syntax tree to compare against the original.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <returns>A list of <see cref="Change"/> objects representing all detected differences.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="oldTree"/>, <paramref name="newTree"/>, or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The returned list contains hierarchical changes where parent changes may contain
    /// nested child changes. For backward compatibility with code expecting a flat list,
    /// use <see cref="ChangeExtensions.Flatten"/> to flatten the hierarchy.
    /// </remarks>
    public List<Change> Compare(SyntaxTree oldTree, SyntaxTree newTree, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldTree);
        ArgumentNullException.ThrowIfNull(newTree);
        ArgumentNullException.ThrowIfNull(options);

        // Delegate to RecursiveTreeComparer for the actual comparison
        var result = _treeComparer.Compare(oldTree, newTree, options);

        // Convert to List<Change> for backward compatibility
        return result.ToList();
    }

    /// <summary>
    /// Compares two C# source code strings and returns a list of detected changes.
    /// </summary>
    /// <param name="oldSource">The original C# source code.</param>
    /// <param name="newSource">The new C# source code to compare against the original.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <returns>A list of <see cref="Change"/> objects representing all detected differences.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="oldSource"/>, <paramref name="newSource"/>, or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public List<Change> CompareSource(string oldSource, string newSource, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldSource);
        ArgumentNullException.ThrowIfNull(newSource);
        ArgumentNullException.ThrowIfNull(options);

        var oldTree = CSharpSyntaxTree.ParseText(oldSource, path: options.OldPath ?? string.Empty);
        var newTree = CSharpSyntaxTree.ParseText(newSource, path: options.NewPath ?? string.Empty);

        return Compare(oldTree, newTree, options);
    }

    /// <summary>
    /// Determines whether two syntax nodes are semantically equivalent.
    /// </summary>
    /// <param name="oldNode">The original syntax node.</param>
    /// <param name="newNode">The new syntax node to compare.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <returns><c>true</c> if the nodes are equivalent; otherwise, <c>false</c>.</returns>
    public bool AreNodesEquivalent(SyntaxNode oldNode, SyntaxNode newNode, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldNode);
        ArgumentNullException.ThrowIfNull(newNode);
        ArgumentNullException.ThrowIfNull(options);

        // Use Roslyn's built-in equivalence check
        // topLevel: false means we do deep structural comparison
        if (SyntaxFactory.AreEquivalent(oldNode, newNode, topLevel: false))
        {
            return true;
        }

        // If ignoring whitespace, normalize and compare
        if (options.IgnoreWhitespace)
        {
            var oldNormalized = NormalizeWhitespace(oldNode);
            var newNormalized = NormalizeWhitespace(newNode);
            return SyntaxFactory.AreEquivalent(oldNormalized, newNormalized, topLevel: false);
        }

        return false;
    }

    private static SyntaxNode NormalizeWhitespace(SyntaxNode node)
    {
        return node.NormalizeWhitespace();
    }
}
