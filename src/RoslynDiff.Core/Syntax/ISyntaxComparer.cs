namespace RoslynDiff.Core.Syntax;

using Microsoft.CodeAnalysis;

/// <summary>
/// Defines the contract for comparing Roslyn syntax trees and producing syntax-level changes.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by Stream B (SyntaxComparer) and consumed by Stream A (RoslynDifferBase).
/// It provides a language-agnostic way to compare syntax trees from different .NET languages.
/// </para>
/// <para>
/// Implementations should handle:
/// <list type="bullet">
/// <item>Matching corresponding nodes between old and new trees</item>
/// <item>Detecting additions, removals, modifications, moves, and renames</item>
/// <item>Building a hierarchical tree of changes reflecting the code structure</item>
/// </list>
/// </para>
/// </remarks>
public interface ISyntaxComparer
{
    /// <summary>
    /// Compares two syntax tree roots and produces a list of syntax changes.
    /// </summary>
    /// <param name="oldRoot">The root node of the old (original) syntax tree.</param>
    /// <param name="newRoot">The root node of the new syntax tree.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <returns>A list of <see cref="SyntaxChange"/> objects representing the differences.</returns>
    IReadOnlyList<SyntaxChange> Compare(SyntaxNode oldRoot, SyntaxNode newRoot, SyntaxCompareOptions options);
}

/// <summary>
/// Options for controlling syntax tree comparison behavior.
/// </summary>
public record SyntaxCompareOptions
{
    /// <summary>
    /// Gets a value indicating whether whitespace differences should be ignored.
    /// </summary>
    public bool IgnoreWhitespace { get; init; }

    /// <summary>
    /// Gets a value indicating whether comment differences should be ignored.
    /// </summary>
    public bool IgnoreComments { get; init; }

    /// <summary>
    /// Gets a value indicating whether trivia (whitespace and comments) should be included in content comparison.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the comparer will use <see cref="SyntaxNode.ToFullString"/> for comparison.
    /// When <c>false</c>, it will use <see cref="SyntaxNode.ToString"/> which excludes trivia.
    /// </remarks>
    public bool IncludeTrivia { get; init; }
}

/// <summary>
/// Represents a change detected between two syntax nodes.
/// </summary>
public record SyntaxChange
{
    /// <summary>
    /// Gets the type of change detected.
    /// </summary>
    public SyntaxChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the syntax node from the old tree, if applicable.
    /// </summary>
    /// <remarks>
    /// This is <c>null</c> for added nodes.
    /// </remarks>
    public SyntaxNode? OldNode { get; init; }

    /// <summary>
    /// Gets the syntax node from the new tree, if applicable.
    /// </summary>
    /// <remarks>
    /// This is <c>null</c> for removed nodes.
    /// </remarks>
    public SyntaxNode? NewNode { get; init; }

    /// <summary>
    /// Gets the child changes within this change.
    /// </summary>
    /// <remarks>
    /// This allows for hierarchical representation of changes.
    /// For example, a modified class may have child changes for its modified methods.
    /// </remarks>
    public IReadOnlyList<SyntaxChange>? Children { get; init; }
}

/// <summary>
/// Specifies the type of change detected for a syntax node.
/// </summary>
public enum SyntaxChangeType
{
    /// <summary>
    /// The node was added in the new tree.
    /// </summary>
    Added,

    /// <summary>
    /// The node was removed from the old tree.
    /// </summary>
    Removed,

    /// <summary>
    /// The node exists in both trees but has been modified.
    /// </summary>
    Modified,

    /// <summary>
    /// The node was moved to a different location in the tree.
    /// </summary>
    Moved,

    /// <summary>
    /// The node was renamed (identifier changed).
    /// </summary>
    Renamed,

    /// <summary>
    /// The node is unchanged between trees.
    /// </summary>
    Unchanged
}
