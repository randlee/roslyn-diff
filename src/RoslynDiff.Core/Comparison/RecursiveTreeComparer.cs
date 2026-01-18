namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Models;
using System.Runtime.CompilerServices;

/// <summary>
/// Compares syntax trees using a recursive, level-by-level algorithm.
/// Each node is processed exactly once at its natural tree level.
/// </summary>
/// <remarks>
/// <para>
/// This comparer addresses BUG-003 (duplicate node extraction) by using a unified
/// recursive algorithm instead of flat extraction + child comparison.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
///   <item>O(n) complexity with early termination for identical subtrees</item>
///   <item>Hierarchical output matching code structure</item>
///   <item>Parallel subtree comparison via ValueTask</item>
///   <item>Cancellation support for long-running comparisons</item>
/// </list>
/// </para>
/// </remarks>
public sealed class RecursiveTreeComparer : ITreeComparer
{
    private readonly NodeMatcher _matcher;
    private readonly ParallelOptions _parallelOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecursiveTreeComparer"/> class
    /// with default settings.
    /// </summary>
    public RecursiveTreeComparer()
        : this(new NodeMatcher(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecursiveTreeComparer"/> class
    /// with custom node matcher and parallel options.
    /// </summary>
    /// <param name="matcher">The node matcher to use for comparing trees.</param>
    /// <param name="parallelOptions">Options controlling parallel processing behavior.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="matcher"/> or <paramref name="parallelOptions"/> is <c>null</c>.
    /// </exception>
    public RecursiveTreeComparer(NodeMatcher matcher, ParallelOptions parallelOptions)
    {
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(parallelOptions);

        _matcher = matcher;
        _parallelOptions = parallelOptions;
    }

    /// <inheritdoc />
    public IReadOnlyList<Change> Compare(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options)
    {
        return CompareAsync(oldTree, newTree, options, CancellationToken.None)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<Change>> CompareAsync(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(oldTree);
        ArgumentNullException.ThrowIfNull(newTree);
        ArgumentNullException.ThrowIfNull(options);

        var oldRoot = oldTree.GetRoot(cancellationToken);
        var newRoot = newTree.GetRoot(cancellationToken);

        // Start recursive comparison at compilation unit level
        var changes = await CompareNodesAsync(oldRoot, newRoot, options, cancellationToken)
            .ConfigureAwait(false);

        return changes;
    }

    /// <summary>
    /// Recursively compares two nodes and their descendants.
    /// This is the core algorithm - each node is processed exactly once.
    /// </summary>
    private async ValueTask<List<Change>> CompareNodesAsync(
        SyntaxNode oldParent,
        SyntaxNode newParent,
        DiffOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var changes = new List<Change>();

        // Step 1: Extract IMMEDIATE structural children only (not recursive)
        var oldChildren = _matcher.ExtractImmediateStructuralChildren(oldParent);
        var newChildren = _matcher.ExtractImmediateStructuralChildren(newParent);

        // Step 2: Match siblings at this level using O(n) hash-based matching
        var matchResult = MatchSiblings(oldChildren, newChildren);

        // Step 3: Process matched pairs (parallel when beneficial)
        var matchedChanges = await ProcessMatchedPairsAsync(
            matchResult.MatchedPairs,
            options,
            cancellationToken).ConfigureAwait(false);
        changes.AddRange(matchedChanges);

        // Step 4: Process removals
        foreach (var oldChild in matchResult.UnmatchedOld)
        {
            cancellationToken.ThrowIfCancellationRequested();
            changes.Add(CreateRemovalChange(oldChild, options));
        }

        // Step 5: Process additions
        foreach (var newChild in matchResult.UnmatchedNew)
        {
            cancellationToken.ThrowIfCancellationRequested();
            changes.Add(CreateAdditionChange(newChild, options));
        }

        // Sort by location for consistent output
        SortByLocation(changes);

        return changes;
    }

    /// <summary>
    /// Processes matched node pairs, detecting modifications and recursing into children.
    /// Uses parallel execution when the number of pairs exceeds threshold.
    /// </summary>
    private async ValueTask<List<Change>> ProcessMatchedPairsAsync(
        IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> matchedPairs,
        DiffOptions options,
        CancellationToken cancellationToken)
    {
        const int ParallelThreshold = 4; // Minimum pairs to justify parallel overhead

        if (matchedPairs.Count == 0)
            return [];

        if (matchedPairs.Count < ParallelThreshold)
        {
            // Sequential processing for small sets
            return await ProcessMatchedPairsSequentialAsync(matchedPairs, options, cancellationToken)
                .ConfigureAwait(false);
        }

        // Parallel processing for larger sets
        return await ProcessMatchedPairsParallelAsync(matchedPairs, options, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sequential processing of matched pairs.
    /// </summary>
    private async ValueTask<List<Change>> ProcessMatchedPairsSequentialAsync(
        IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> matchedPairs,
        DiffOptions options,
        CancellationToken cancellationToken)
    {
        var changes = new List<Change>();

        foreach (var (oldNode, newNode) in matchedPairs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var change = await ProcessSingleMatchAsync(oldNode, newNode, options, cancellationToken)
                .ConfigureAwait(false);

            if (change is not null)
                changes.Add(change);
        }

        return changes;
    }

    /// <summary>
    /// Parallel processing of matched pairs using ValueTask for efficiency.
    /// </summary>
    private async ValueTask<List<Change>> ProcessMatchedPairsParallelAsync(
        IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> matchedPairs,
        DiffOptions options,
        CancellationToken cancellationToken)
    {
        // Create tasks for parallel execution
        var tasks = matchedPairs.Select(pair =>
            ProcessSingleMatchAsync(pair.Old, pair.New, options, cancellationToken).AsTask());

        // Wait for all to complete
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // Filter out nulls (unchanged nodes) and return
        return results.Where(c => c is not null).ToList()!;
    }

    /// <summary>
    /// Processes a single matched pair, returning a Change if modified or null if identical.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<Change?> ProcessSingleMatchAsync(
        SyntaxNode oldNode,
        SyntaxNode newNode,
        DiffOptions options,
        CancellationToken cancellationToken)
    {
        // CRITICAL OPTIMIZATION: Skip identical subtrees entirely
        if (AreSubtreesEquivalent(oldNode, newNode, options))
            return null;

        // Recursively compare children
        var nestedChanges = await CompareNodesAsync(oldNode, newNode, options, cancellationToken)
            .ConfigureAwait(false);

        // Create modification change
        var change = new Change
        {
            Type = ChangeType.Modified,
            Kind = NodeMatcher.GetChangeKind(newNode),
            Name = NodeMatcher.GetNodeName(newNode),
            OldLocation = NodeMatcher.CreateLocation(oldNode, options.OldPath),
            NewLocation = NodeMatcher.CreateLocation(newNode, options.NewPath),
            OldContent = oldNode.NormalizeWhitespace().ToString(),
            NewContent = newNode.NormalizeWhitespace().ToString(),
            Children = nestedChanges.Count > 0 ? nestedChanges : null
        };

        return change;
    }

    /// <summary>
    /// Matches siblings at a single tree level using O(n) hash-based lookup.
    /// </summary>
    private MatchResult MatchSiblings(
        IReadOnlyList<NodeMatcher.NodeInfo> oldChildren,
        IReadOnlyList<NodeMatcher.NodeInfo> newChildren)
    {
        // Build hash lookup for new children: (name, kind, signature) → index
        var newLookup = new Dictionary<(string?, ChangeKind, string?), int>(newChildren.Count);
        for (var i = 0; i < newChildren.Count; i++)
        {
            var key = (newChildren[i].Name, newChildren[i].Kind, newChildren[i].Signature);
            newLookup.TryAdd(key, i); // First wins for same-signature overloads
        }

        var matchedPairs = new List<(SyntaxNode, SyntaxNode)>(Math.Min(oldChildren.Count, newChildren.Count));
        var matchedNewIndices = new HashSet<int>();
        var unmatchedOld = new List<SyntaxNode>();

        // Match old children to new children
        foreach (var oldChild in oldChildren)
        {
            var key = (oldChild.Name, oldChild.Kind, oldChild.Signature);
            if (newLookup.TryGetValue(key, out var newIndex) && !matchedNewIndices.Contains(newIndex))
            {
                matchedPairs.Add((oldChild.Node, newChildren[newIndex].Node));
                matchedNewIndices.Add(newIndex);
            }
            else
            {
                unmatchedOld.Add(oldChild.Node);
            }
        }

        // Collect unmatched new children
        var unmatchedNew = new List<SyntaxNode>();
        for (var i = 0; i < newChildren.Count; i++)
        {
            if (!matchedNewIndices.Contains(i))
                unmatchedNew.Add(newChildren[i].Node);
        }

        return new MatchResult(matchedPairs, unmatchedOld, unmatchedNew);
    }

    /// <summary>
    /// Determines if two subtrees are structurally equivalent.
    /// Uses Roslyn's built-in equivalence check with optional whitespace normalization.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AreSubtreesEquivalent(SyntaxNode oldNode, SyntaxNode newNode, DiffOptions options)
    {
        // Fast path: Use Roslyn's built-in structural equivalence
        if (SyntaxFactory.AreEquivalent(oldNode, newNode, topLevel: false))
            return true;

        // Slow path: Normalize whitespace and compare
        if (options.IgnoreWhitespace)
        {
            var oldNormalized = oldNode.NormalizeWhitespace();
            var newNormalized = newNode.NormalizeWhitespace();
            return SyntaxFactory.AreEquivalent(oldNormalized, newNormalized, topLevel: false);
        }

        return false;
    }

    private static Change CreateRemovalChange(SyntaxNode node, DiffOptions options)
    {
        return new Change
        {
            Type = ChangeType.Removed,
            Kind = NodeMatcher.GetChangeKind(node),
            Name = NodeMatcher.GetNodeName(node),
            OldLocation = NodeMatcher.CreateLocation(node, options.OldPath),
            OldContent = node.NormalizeWhitespace().ToString()
        };
    }

    private static Change CreateAdditionChange(SyntaxNode node, DiffOptions options)
    {
        return new Change
        {
            Type = ChangeType.Added,
            Kind = NodeMatcher.GetChangeKind(node),
            Name = NodeMatcher.GetNodeName(node),
            NewLocation = NodeMatcher.CreateLocation(node, options.NewPath),
            NewContent = node.NormalizeWhitespace().ToString()
        };
    }

    private static void SortByLocation(List<Change> changes)
    {
        changes.Sort((a, b) =>
        {
            var aLine = a.NewLocation?.StartLine ?? a.OldLocation?.StartLine ?? 0;
            var bLine = b.NewLocation?.StartLine ?? b.OldLocation?.StartLine ?? 0;
            return aLine.CompareTo(bLine);
        });
    }

    /// <summary>
    /// Represents the result of matching nodes during recursive tree comparison.
    /// </summary>
    /// <param name="MatchedPairs">Pairs of nodes that were matched between old and new trees.</param>
    /// <param name="UnmatchedOld">Nodes from the old tree that have no match in the new tree (removals).</param>
    /// <param name="UnmatchedNew">Nodes from the new tree that have no match in the old tree (additions).</param>
    private record MatchResult(
        IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> MatchedPairs,
        IReadOnlyList<SyntaxNode> UnmatchedOld,
        IReadOnlyList<SyntaxNode> UnmatchedNew);
}
