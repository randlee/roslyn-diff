namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using RoslynDiff.Core.Models;

/// <summary>
/// Compares two VB.NET syntax trees and identifies structural changes.
/// </summary>
/// <remarks>
/// <para>
/// This comparer uses Roslyn's syntax tree API to understand code structure,
/// enabling semantic-aware diffing that can identify changes at the module, class,
/// sub, function, property, and field level rather than just line-by-line differences.
/// </para>
/// <para>
/// The comparison algorithm works in three phases:
/// <list type="number">
///   <item>Extract structural nodes from both trees</item>
///   <item>Match nodes by name and kind (case-insensitive for VB)</item>
///   <item>Generate changes based on matching results</item>
/// </list>
/// </para>
/// </remarks>
public sealed class VisualBasicSyntaxComparer
{
    private readonly VisualBasicNodeMatcher _matcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualBasicSyntaxComparer"/> class.
    /// </summary>
    public VisualBasicSyntaxComparer()
    {
        _matcher = new VisualBasicNodeMatcher();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualBasicSyntaxComparer"/> class with a custom node matcher.
    /// </summary>
    /// <param name="matcher">The node matcher to use for comparing trees.</param>
    internal VisualBasicSyntaxComparer(VisualBasicNodeMatcher matcher)
    {
        _matcher = matcher;
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
    public List<Change> Compare(SyntaxTree oldTree, SyntaxTree newTree, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldTree);
        ArgumentNullException.ThrowIfNull(newTree);
        ArgumentNullException.ThrowIfNull(options);

        var oldRoot = oldTree.GetRoot();
        var newRoot = newTree.GetRoot();

        // Extract structural nodes from both trees
        var oldNodes = _matcher.ExtractStructuralNodes(oldRoot);
        var newNodes = _matcher.ExtractStructuralNodes(newRoot);

        // Match nodes between old and new trees
        var matchResult = _matcher.MatchNodes(oldNodes, newNodes);

        // Build the change list
        var changes = new List<Change>();

        // Process matched pairs for modifications
        ProcessMatchedPairs(matchResult.MatchedPairs, options, changes);

        // Process unmatched old nodes as removals
        ProcessRemovals(matchResult.UnmatchedOld, options, changes);

        // Process unmatched new nodes as additions
        ProcessAdditions(matchResult.UnmatchedNew, options, changes);

        // Sort changes by location for consistent output
        SortChangesByLocation(changes);

        return changes;
    }

    /// <summary>
    /// Compares two VB.NET source code strings and returns a list of detected changes.
    /// </summary>
    /// <param name="oldSource">The original VB.NET source code.</param>
    /// <param name="newSource">The new VB.NET source code to compare against the original.</param>
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

        var oldTree = VisualBasicSyntaxTree.ParseText(oldSource, path: options.OldPath ?? string.Empty);
        var newTree = VisualBasicSyntaxTree.ParseText(newSource, path: options.NewPath ?? string.Empty);

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

    private void ProcessMatchedPairs(
        IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> matchedPairs,
        DiffOptions options,
        List<Change> changes)
    {
        foreach (var (oldNode, newNode) in matchedPairs)
        {
            if (AreNodesEquivalent(oldNode, newNode, options))
            {
                // Nodes are equivalent, no change needed
                continue;
            }

            // Content has changed - create a modification change
            var change = CreateChange(
                ChangeType.Modified,
                VisualBasicNodeMatcher.GetChangeKind(oldNode),
                VisualBasicNodeMatcher.GetNodeName(oldNode),
                oldNode,
                newNode,
                options);

            // Recursively compare children for nested changes
            var childChanges = CompareChildren(oldNode, newNode, options);
            if (childChanges.Count > 0)
            {
                change = change with { Children = childChanges };
            }

            changes.Add(change);
        }
    }

    private void ProcessRemovals(
        IReadOnlyList<SyntaxNode> unmatchedOld,
        DiffOptions options,
        List<Change> changes)
    {
        foreach (var oldNode in unmatchedOld)
        {
            var change = CreateChange(
                ChangeType.Removed,
                VisualBasicNodeMatcher.GetChangeKind(oldNode),
                VisualBasicNodeMatcher.GetNodeName(oldNode),
                oldNode: oldNode,
                newNode: null,
                options);

            changes.Add(change);
        }
    }

    private void ProcessAdditions(
        IReadOnlyList<SyntaxNode> unmatchedNew,
        DiffOptions options,
        List<Change> changes)
    {
        foreach (var newNode in unmatchedNew)
        {
            var change = CreateChange(
                ChangeType.Added,
                VisualBasicNodeMatcher.GetChangeKind(newNode),
                VisualBasicNodeMatcher.GetNodeName(newNode),
                oldNode: null,
                newNode: newNode,
                options);

            changes.Add(change);
        }
    }

    private List<Change> CompareChildren(SyntaxNode oldParent, SyntaxNode newParent, DiffOptions options)
    {
        // Extract child structural nodes
        var oldChildren = ExtractImmediateStructuralChildren(oldParent);
        var newChildren = ExtractImmediateStructuralChildren(newParent);

        if (oldChildren.Count == 0 && newChildren.Count == 0)
        {
            return [];
        }

        // Match children
        var matchResult = _matcher.MatchNodes(oldChildren, newChildren);

        var childChanges = new List<Change>();

        // Process matched children for modifications
        foreach (var (oldChild, newChild) in matchResult.MatchedPairs)
        {
            if (!AreNodesEquivalent(oldChild, newChild, options))
            {
                var change = CreateChange(
                    ChangeType.Modified,
                    VisualBasicNodeMatcher.GetChangeKind(oldChild),
                    VisualBasicNodeMatcher.GetNodeName(oldChild),
                    oldChild,
                    newChild,
                    options);

                childChanges.Add(change);
            }
        }

        // Process removed children
        foreach (var oldChild in matchResult.UnmatchedOld)
        {
            var change = CreateChange(
                ChangeType.Removed,
                VisualBasicNodeMatcher.GetChangeKind(oldChild),
                VisualBasicNodeMatcher.GetNodeName(oldChild),
                oldChild,
                null,
                options);

            childChanges.Add(change);
        }

        // Process added children
        foreach (var newChild in matchResult.UnmatchedNew)
        {
            var change = CreateChange(
                ChangeType.Added,
                VisualBasicNodeMatcher.GetChangeKind(newChild),
                VisualBasicNodeMatcher.GetNodeName(newChild),
                null,
                newChild,
                options);

            childChanges.Add(change);
        }

        return childChanges;
    }

    private IReadOnlyList<VisualBasicNodeMatcher.NodeInfo> ExtractImmediateStructuralChildren(SyntaxNode parent)
    {
        var children = new List<VisualBasicNodeMatcher.NodeInfo>();

        foreach (var child in parent.ChildNodes())
        {
            if (IsStructuralNode(child))
            {
                var name = VisualBasicNodeMatcher.GetNodeName(child);
                var kind = VisualBasicNodeMatcher.GetChangeKind(child);
                var signature = VisualBasicNodeMatcher.GetSignature(child);
                children.Add(new VisualBasicNodeMatcher.NodeInfo(child, name, kind, signature));
            }
        }

        return children;
    }

    private static Change CreateChange(
        ChangeType type,
        ChangeKind kind,
        string? name,
        SyntaxNode? oldNode,
        SyntaxNode? newNode,
        DiffOptions options)
    {
        return new Change
        {
            Type = type,
            Kind = kind,
            Name = name,
            OldLocation = oldNode is not null ? VisualBasicNodeMatcher.CreateLocation(oldNode, options.OldPath) : null,
            NewLocation = newNode is not null ? VisualBasicNodeMatcher.CreateLocation(newNode, options.NewPath) : null,
            OldContent = oldNode?.ToFullString(),
            NewContent = newNode?.ToFullString()
        };
    }

    private static SyntaxNode NormalizeWhitespace(SyntaxNode node)
    {
        return node.NormalizeWhitespace();
    }

    private static void SortChangesByLocation(List<Change> changes)
    {
        changes.Sort((a, b) =>
        {
            // Prefer new location, fall back to old location
            var aLine = a.NewLocation?.StartLine ?? a.OldLocation?.StartLine ?? 0;
            var bLine = b.NewLocation?.StartLine ?? b.OldLocation?.StartLine ?? 0;
            return aLine.CompareTo(bLine);
        });
    }

    private static bool IsStructuralNode(SyntaxNode node)
    {
        // Note: PropertyStatementSyntax is used for auto-properties (e.g., Public Property Name As String)
        // PropertyBlockSyntax is used for properties with explicit Get/Set blocks
        // We need to check that PropertyStatementSyntax is not inside a PropertyBlockSyntax (to avoid duplicates)
        if (node is Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyStatementSyntax propStmt)
        {
            // Only treat as structural if it's an auto-property (parent is not PropertyBlockSyntax)
            return propStmt.Parent is not Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyBlockSyntax;
        }

        return node is Microsoft.CodeAnalysis.VisualBasic.Syntax.NamespaceBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.ModuleBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.StructureBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.InterfaceBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.EnumBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.ConstructorBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyBlockSyntax
            or Microsoft.CodeAnalysis.VisualBasic.Syntax.FieldDeclarationSyntax;
    }
}
