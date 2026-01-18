# Design Document: Recursive Tree Diff Algorithm

**Document ID:** DESIGN-003
**Bug Reference:** BUG-003 (Duplicate Node Extraction)
**Author:** ARCH-DIFF
**Date:** 2026-01-17
**Status:** PROPOSED
**Branch:** feature/sample-data-validation-tests

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Problem Statement](#2-problem-statement)
3. [Current Architecture Analysis](#3-current-architecture-analysis)
4. [Proposed Architecture](#4-proposed-architecture)
5. [Detailed Implementation Design](#5-detailed-implementation-design)
6. [Parallel Execution with ValueTask](#6-parallel-execution-with-valuetask)
7. [Cancellation Support](#7-cancellation-support)
8. [Performance Analysis](#8-performance-analysis)
9. [API Changes](#9-api-changes)
10. [Test Strategy](#10-test-strategy)
11. [Migration Guide](#11-migration-guide)
12. [Implementation Checklist](#12-implementation-checklist)
13. [Risk Assessment](#13-risk-assessment)
14. [Appendix](#14-appendix)

---

## 1. Executive Summary

### Problem
BUG-003 causes roslyn-diff to report duplicate structural nodes, creating overlapping line numbers in output. The root cause is a **fundamental architectural flaw**: two competing algorithms (flat extraction + hierarchical child comparison) process the same nodes multiple times.

### Solution
Replace the current flat extraction algorithm with a **unified recursive tree diff** that:
- Processes each node exactly once at its natural tree level
- Produces hierarchical output matching code structure
- Enables parallel subtree comparison via `ValueTask<T>`
- Skips identical subtrees for O(1) early termination

### Impact
- **Correctness:** Eliminates all duplicate reporting
- **Performance:** O(n) with early exit; 10-100x faster on large files with few changes
- **Scalability:** Handles 5000+ line classes efficiently
- **Parallelization:** Architecture supports concurrent subtree comparison

---

## 2. Problem Statement

### 2.1 Symptom

When comparing Calculator.cs (before/after adding methods), roslyn-diff reports:

```
Change 1: Namespace "Samples" (lines 1-56)
Change 2: Namespace "Samples" (lines 1-56)  ← DUPLICATE
Change 3: Class "Calculator" (lines 6-56)
Change 4: Class "Calculator" (lines 6-56)   ← DUPLICATE
Change 5: Method "Multiply" (lines 36-39)
Change 6: Method "Divide" (lines 48-54)
Change 7: Method "Multiply" (lines 36-39)   ← DUPLICATE
```

**Expected:** 4 changes (Namespace modified, Class modified, 2 methods added)
**Actual:** 7 changes with duplicates and overlapping line ranges

### 2.2 User Impact

- **Validation tests fail:** 27 of 34 integration tests fail due to line overlaps
- **Incorrect statistics:** Change counts are inflated
- **Confusing output:** Same element reported multiple times
- **AI/tooling confusion:** Downstream consumers receive inconsistent data

### 2.3 Root Cause

The bug stems from **two independent code paths** that both produce changes for the same nodes:

1. **Path 1 (Flat Extraction):** `ExtractStructuralNodes()` recursively extracts ALL nodes into a flat list
2. **Path 2 (Child Comparison):** `CompareChildren()` creates additional changes for children of matched pairs

When a modified Namespace contains a modified Class, both paths create a change for the Class.

---

## 3. Current Architecture Analysis

### 3.1 Code Flow

```
Compare(oldTree, newTree)
│
├── ExtractStructuralNodes(oldRoot)  ──► Flat list: [NS, Class, Method1, Method2, ...]
├── ExtractStructuralNodes(newRoot)  ──► Flat list: [NS, Class, Method1, Method2, Method3, ...]
│
├── MatchNodes(oldList, newList)     ──► O(n × m) global matching
│
├── ProcessMatchedPairs(pairs)
│   └── For each matched pair:
│       ├── CreateChange(...)        ──► Change for this node
│       └── CompareChildren(...)     ──► ALSO create changes for children ← DUPLICATES
│
├── ProcessRemovals(unmatchedOld)
└── ProcessAdditions(unmatchedNew)
```

### 3.2 The Duplication Mechanism

```
Step 1: Flat lists contain:
  Old: [Namespace, Class, Method1, Method2]
  New: [Namespace, Class, Method1, Method2, Method3, Method4]

Step 2: Global matching produces:
  Matched: [(NS,NS), (Class,Class), (M1,M1), (M2,M2)]
  Added: [Method3, Method4]

Step 3: ProcessMatchedPairs for (Namespace, Namespace):
  - Creates Change(Namespace)
  - Calls CompareChildren() → Creates Change(Class) as child ← FIRST

Step 4: ProcessMatchedPairs for (Class, Class):
  - Creates Change(Class) ← DUPLICATE (already created as child of Namespace)
  - Calls CompareChildren() → Creates Changes for Methods as children ← FIRST

Step 5: ProcessAdditions for Method3, Method4:
  - Creates Change(Method3) ← DUPLICATE (already child of Class)
  - Creates Change(Method4) ← DUPLICATE (already child of Class)
```

### 3.3 Complexity Analysis

| Operation | Current Complexity | Notes |
|-----------|-------------------|-------|
| Extract nodes | O(n) × 2 | Full tree traversal twice |
| Match nodes | O(n × m) | For each old, scan all new |
| Process pairs | O(matched) | Plus recursive children |
| Total | O(n × m) | Quadratic worst case |

### 3.4 Identified Flaws

1. **Flat extraction loses structure:** We know Method1 is inside Class, yet we search globally
2. **Redundant processing:** Nodes processed both globally AND as children
3. **No early termination:** Identical subtrees still fully processed
4. **No parallelization:** Sequential processing of all nodes
5. **Memory overhead:** Two complete flat lists stored

---

## 4. Proposed Architecture

### 4.1 Core Principle

> **Compare trees level-by-level, recursively. Each node is processed exactly once, at its natural level in the hierarchy.**

### 4.2 Algorithm Overview

```
CompareNodesAsync(oldParent, newParent, options, cancellationToken)
│
├── 1. Extract IMMEDIATE structural children (not recursive)
│       oldChildren = GetImmediateStructuralChildren(oldParent)
│       newChildren = GetImmediateStructuralChildren(newParent)
│
├── 2. Match siblings at THIS level only
│       matchResult = MatchSiblings(oldChildren, newChildren)  // O(n) hash-based
│
├── 3. Process matched pairs (PARALLELIZABLE):
│       For each (oldChild, newChild) in matched:
│           if (AreSubtreesEquivalent(oldChild, newChild)):
│               continue  // SKIP ENTIRE SUBTREE ← Key optimization
│
│           // Recursive comparison (can run in parallel)
│           nestedChanges = await CompareNodesAsync(oldChild, newChild, ...)
│           changes.Add(Change with Children = nestedChanges)
│
├── 4. Process removals (unmatched old children)
│
└── 5. Process additions (unmatched new children)
```

### 4.3 Key Differences

| Aspect | Current | Proposed |
|--------|---------|----------|
| Node extraction | Recursive flatten | Immediate children only |
| Matching scope | Global (all nodes) | Local (siblings at each level) |
| Recursion | Two competing paths | Single unified path |
| Subtree skip | Never | O(1) for identical subtrees |
| Output structure | Flat with duplicates | Hierarchical, no duplicates |
| Parallelization | Not possible | Natural per-subtree |
| Memory | O(n) flat lists | O(depth) stack only |

### 4.4 Output Format

**Hierarchical (Default):**
```json
{
  "changes": [
    {
      "type": "Modified",
      "kind": "Namespace",
      "name": "Samples",
      "newLocation": { "startLine": 1, "endLine": 56 },
      "children": [
        {
          "type": "Modified",
          "kind": "Class",
          "name": "Calculator",
          "newLocation": { "startLine": 6, "endLine": 56 },
          "children": [
            { "type": "Added", "kind": "Method", "name": "Multiply", "newLocation": { "startLine": 36, "endLine": 39 } },
            { "type": "Added", "kind": "Method", "name": "Divide", "newLocation": { "startLine": 48, "endLine": 54 } }
          ]
        }
      ]
    }
  ]
}
```

**Flat (Via Extension Method):**
```csharp
var flatChanges = hierarchicalChanges.Flatten().ToList();
// Returns: [Namespace, Class, Multiply, Divide] - all at top level
```

---

## 5. Detailed Implementation Design

### 5.1 New Interface: `ITreeComparer`

```csharp
namespace RoslynDiff.Core.Comparison;

/// <summary>
/// Defines the contract for recursive tree comparison with async support.
/// </summary>
public interface ITreeComparer
{
    /// <summary>
    /// Compares two syntax trees asynchronously using recursive tree diff.
    /// </summary>
    /// <param name="oldTree">The original syntax tree.</param>
    /// <param name="newTree">The modified syntax tree.</param>
    /// <param name="options">Comparison options.</param>
    /// <param name="cancellationToken">Cancellation token for early termination.</param>
    /// <returns>A list of hierarchical changes.</returns>
    ValueTask<IReadOnlyList<Change>> CompareAsync(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronous comparison for backward compatibility.
    /// </summary>
    IReadOnlyList<Change> Compare(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options);
}
```

### 5.2 New Class: `RecursiveTreeComparer`

```csharp
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
    /// Initializes a new instance with default settings.
    /// </summary>
    public RecursiveTreeComparer()
        : this(new NodeMatcher(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount })
    {
    }

    /// <summary>
    /// Initializes a new instance with custom dependencies.
    /// </summary>
    public RecursiveTreeComparer(NodeMatcher matcher, ParallelOptions parallelOptions)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _parallelOptions = parallelOptions ?? throw new ArgumentNullException(nameof(parallelOptions));
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
    /// Result of matching siblings at one tree level.
    /// </summary>
    private record MatchResult(
        IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> MatchedPairs,
        IReadOnlyList<SyntaxNode> UnmatchedOld,
        IReadOnlyList<SyntaxNode> UnmatchedNew);
}
```

### 5.3 Updates to `NodeMatcher`

```csharp
// Add to NodeMatcher.cs

/// <summary>
/// Extracts only IMMEDIATE structural children of a parent node.
/// This is O(children) not O(all descendants).
/// </summary>
/// <remarks>
/// Unlike <see cref="ExtractStructuralNodes"/> which recursively flattens the entire tree,
/// this method only returns direct children, enabling level-by-level comparison.
/// </remarks>
public IReadOnlyList<NodeInfo> ExtractImmediateStructuralChildren(SyntaxNode parent)
{
    var children = new List<NodeInfo>();

    foreach (var child in parent.ChildNodes())
    {
        if (IsStructuralNode(child))
        {
            children.Add(new NodeInfo(
                child,
                GetNodeName(child),
                GetChangeKind(child),
                GetSignature(child)));
        }
    }

    return children;
}

/// <summary>
/// [DEPRECATED] Extracts all structural nodes recursively into a flat list.
/// </summary>
/// <remarks>
/// This method is deprecated in favor of <see cref="ExtractImmediateStructuralChildren"/>
/// which supports the new recursive tree diff algorithm.
/// </remarks>
[Obsolete("Use ExtractImmediateStructuralChildren for hierarchical comparison. " +
          "This method will be removed in a future version.")]
public IReadOnlyList<NodeInfo> ExtractStructuralNodes(SyntaxNode root)
{
    var nodes = new List<NodeInfo>();
    ExtractNodesRecursive(root, nodes);
    return nodes;
}
```

### 5.4 Extension Methods for Backward Compatibility

```csharp
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
```

---

## 6. Parallel Execution with ValueTask

### 6.1 Why ValueTask?

`ValueTask<T>` is preferred over `Task<T>` for this use case because:

1. **Frequent synchronous completion:** Many subtrees are identical and return immediately
2. **Reduced allocations:** `ValueTask` can wrap a result directly without heap allocation
3. **Efficient async paths:** When async is needed, `ValueTask` wraps a `Task` seamlessly

### 6.2 Parallel Execution Strategy

```
Level 0 (CompilationUnit):
  └─ Namespace1, Namespace2  ← Compare in parallel if > threshold

Level 1 (Namespace):
  └─ Class1, Class2, Class3  ← Compare in parallel if > threshold

Level 2 (Class):
  └─ Method1..Method50  ← Compare in parallel (50 > threshold)
```

**Threshold Logic:**
```csharp
const int ParallelThreshold = 4;

if (matchedPairs.Count < ParallelThreshold)
    // Sequential: Overhead not worth it
    ProcessSequential();
else
    // Parallel: Task.WhenAll on subtrees
    ProcessParallel();
```

### 6.3 Parallel Processing Implementation

```csharp
private async ValueTask<List<Change>> ProcessMatchedPairsParallelAsync(
    IReadOnlyList<(SyntaxNode Old, SyntaxNode New)> matchedPairs,
    DiffOptions options,
    CancellationToken cancellationToken)
{
    // Partition work based on subtree complexity (optional enhancement)
    var tasks = matchedPairs.Select(pair =>
        ProcessSingleMatchAsync(pair.Old, pair.New, options, cancellationToken).AsTask());

    // Execute in parallel with degree of parallelism limit
    var results = await Task.WhenAll(tasks).ConfigureAwait(false);

    // Filter nulls (unchanged subtrees) and return
    return results.Where(c => c is not null).ToList()!;
}
```

### 6.4 ConfigureAwait(false) Usage

All async methods use `ConfigureAwait(false)` because:
- roslyn-diff is a library/CLI tool, not a UI application
- No synchronization context needs to be preserved
- Reduces overhead of context capture

---

## 7. Cancellation Support

### 7.1 Cancellation Points

Cancellation is checked at:
1. Entry to `CompareNodesAsync`
2. Before processing each matched pair
3. Before processing each removal
4. Before processing each addition

### 7.2 Implementation

```csharp
private async ValueTask<List<Change>> CompareNodesAsync(
    SyntaxNode oldParent,
    SyntaxNode newParent,
    DiffOptions options,
    CancellationToken cancellationToken)
{
    // Check cancellation at method entry
    cancellationToken.ThrowIfCancellationRequested();

    // ... processing ...

    foreach (var (oldNode, newNode) in matchResult.MatchedPairs)
    {
        // Check before each potentially expensive operation
        cancellationToken.ThrowIfCancellationRequested();
        // ... recursive call ...
    }
}
```

### 7.3 CLI Integration

```csharp
// In CLI command handler
using var cts = new CancellationTokenSource();

// Handle Ctrl+C
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("Cancelling comparison...");
};

try
{
    var changes = await comparer.CompareAsync(oldTree, newTree, options, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Comparison cancelled by user.");
    return ExitCodes.Cancelled;
}
```

---

## 8. Performance Analysis

### 8.1 Complexity Comparison

| Scenario | Current Algorithm | Proposed Algorithm |
|----------|-------------------|-------------------|
| Identical files | O(n × m) matching | O(n) equivalence checks |
| 1 method changed in 5000-line file | O(n × m) full scan | O(log n) path to change |
| All methods changed | O(n × m) + O(children²) | O(n) single pass |
| Deep nesting (5 levels) | O(n × m) × 5 levels | O(n) total |

### 8.2 Expected Performance Improvements

| Test Case | Current (est.) | Proposed (est.) | Speedup |
|-----------|----------------|-----------------|---------|
| Identical 100-line file | 10ms | 1ms | 10x |
| Identical 5000-line file | 500ms | 5ms | 100x |
| 1 change in 5000-line file | 500ms | 10ms | 50x |
| 50% changes in 5000-line file | 500ms | 300ms | 1.7x |
| 100% changes | 500ms | 400ms | 1.25x |

### 8.3 Memory Usage

| Aspect | Current | Proposed |
|--------|---------|----------|
| Flat lists | 2 × O(n) nodes | None |
| Match tracking | O(n) indices | O(siblings) per level |
| Call stack | O(1) | O(depth) |
| Total | O(n) | O(depth + max siblings) |

### 8.4 Benchmark Plan

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class TreeComparerBenchmarks
{
    private SyntaxTree _identicalOld;
    private SyntaxTree _identicalNew;
    private SyntaxTree _oneChangeOld;
    private SyntaxTree _oneChangeNew;
    private SyntaxTree _manyChangesOld;
    private SyntaxTree _manyChangesNew;

    [GlobalSetup]
    public void Setup()
    {
        // Load test files of various sizes
    }

    [Benchmark(Baseline = true)]
    public List<Change> Current_IdenticalFile() =>
        new SyntaxComparer().Compare(_identicalOld, _identicalNew, new DiffOptions());

    [Benchmark]
    public IReadOnlyList<Change> Proposed_IdenticalFile() =>
        new RecursiveTreeComparer().Compare(_identicalOld, _identicalNew, new DiffOptions());

    [Benchmark]
    public List<Change> Current_OneChange() =>
        new SyntaxComparer().Compare(_oneChangeOld, _oneChangeNew, new DiffOptions());

    [Benchmark]
    public IReadOnlyList<Change> Proposed_OneChange() =>
        new RecursiveTreeComparer().Compare(_oneChangeOld, _oneChangeNew, new DiffOptions());

    // ... more benchmarks ...
}
```

---

## 9. API Changes

### 9.1 New Public APIs

| Type | Name | Description |
|------|------|-------------|
| Interface | `ITreeComparer` | Contract for tree comparison with async support |
| Class | `RecursiveTreeComparer` | New implementation using recursive algorithm |
| Extension | `ChangeExtensions.Flatten()` | Flatten hierarchical changes |
| Extension | `ChangeExtensions.CountAll()` | Count changes including children |
| Extension | `ChangeExtensions.FindByName()` | Find change by name at any level |
| Extension | `ChangeExtensions.OfKind()` | Filter by change kind |

### 9.2 Modified APIs

| Type | Change | Migration |
|------|--------|-----------|
| `NodeMatcher.ExtractStructuralNodes` | Marked `[Obsolete]` | Use `ExtractImmediateStructuralChildren` |
| `SyntaxComparer.Compare` | Unchanged signature | Internally uses new algorithm |

### 9.3 Breaking Changes

| Change | Impact | Migration |
|--------|--------|-----------|
| Output structure is hierarchical | Tests checking flat structure | Use `Flatten()` extension |
| Changes have `Children` property | New property to handle | Check for null before accessing |

---

## 10. Test Strategy

### 10.1 Unit Tests to Update

Tests that check flat structure need updating:

```csharp
// BEFORE: Expects Class at top level
changes.Should().Contain(c => c.Kind == ChangeKind.Class && c.Name == "Bar");

// AFTER: Option 1 - Use Flatten()
changes.Flatten().Should().Contain(c => c.Kind == ChangeKind.Class && c.Name == "Bar");

// AFTER: Option 2 - Check hierarchy
changes.Should().ContainSingle(c => c.Kind == ChangeKind.Namespace)
    .Which.Children.Should().Contain(c => c.Kind == ChangeKind.Class && c.Name == "Bar");
```

### 10.2 New Unit Tests

```csharp
public class RecursiveTreeComparerTests
{
    [Fact]
    public void Compare_IdenticalTrees_ReturnsNoChanges()

    [Fact]
    public void Compare_AddedClass_ReturnsHierarchicalChange()

    [Fact]
    public void Compare_ModifiedMethod_NestsUnderClass()

    [Fact]
    public void Compare_NodesProcessedExactlyOnce_NoDuplicates()

    [Fact]
    public async Task CompareAsync_LargeFile_CompletesWithinTimeout()

    [Fact]
    public async Task CompareAsync_Cancellation_ThrowsOperationCancelled()

    [Fact]
    public void Flatten_HierarchicalChanges_ReturnsAllAtTopLevel()

    [Fact]
    public void Compare_DeepNesting_HandlesCorrectly()

    [Fact]
    public void MatchSiblings_UsesHashLookup_IsOrderN()
}
```

### 10.3 Integration Tests

The 27 failing validation tests from Sprint 4 should pass after this fix:
- No more overlapping line ranges
- No duplicate nodes
- Correct change counts

### 10.4 Performance Tests

```csharp
public class PerformanceTests
{
    [Fact]
    public void Compare_5000LineFile_CompletesUnder100ms()

    [Fact]
    public void Compare_IdenticalFile_SkipsProcessingQuickly()

    [Fact]
    public void Compare_ParallelExecution_FasterThanSequential()
}
```

---

## 11. Migration Guide

### 11.1 For Library Users

**Minimal change required:**
```csharp
// Before
var comparer = new SyntaxComparer();
var changes = comparer.Compare(oldTree, newTree, options);

// After (same API, hierarchical output)
var comparer = new SyntaxComparer();  // Now uses RecursiveTreeComparer internally
var changes = comparer.Compare(oldTree, newTree, options);

// To get flat list like before:
var flatChanges = changes.Flatten().ToList();
```

**For async support:**
```csharp
// New async API
var comparer = new RecursiveTreeComparer();
var changes = await comparer.CompareAsync(oldTree, newTree, options, cancellationToken);
```

### 11.2 For Test Authors

```csharp
// Update assertions to use Flatten() or check hierarchy
// See Test Strategy section for examples
```

### 11.3 For Output Consumers (JSON, HTML, etc.)

The JSON output will now be hierarchical with nested `children` arrays. Consumers that expect flat lists should:
1. Use the new `flatten` option in CLI (if added)
2. Post-process to flatten the hierarchy
3. Update to handle hierarchical structure

---

## 12. Implementation Checklist

### Phase 1: Core Implementation (Priority: P0)

- [ ] **1.1** Create `ITreeComparer` interface in `src/RoslynDiff.Core/Comparison/`
- [ ] **1.2** Create `RecursiveTreeComparer` class with synchronous `Compare()` method
- [ ] **1.3** Implement `CompareNodesAsync()` recursive algorithm
- [ ] **1.4** Implement `MatchSiblings()` with O(n) hash-based matching
- [ ] **1.5** Implement `AreSubtreesEquivalent()` with early termination
- [ ] **1.6** Add `ExtractImmediateStructuralChildren()` to `NodeMatcher`
- [ ] **1.7** Mark `ExtractStructuralNodes()` as `[Obsolete]`
- [ ] **1.8** Create `ChangeExtensions` with `Flatten()`, `CountAll()`, `FindByName()`, `OfKind()`

### Phase 2: Async & Parallel Support (Priority: P0)

- [ ] **2.1** Implement `CompareAsync()` with `ValueTask<T>` return type
- [ ] **2.2** Implement `ProcessMatchedPairsParallelAsync()` with `Task.WhenAll`
- [ ] **2.3** Add `CancellationToken` support throughout
- [ ] **2.4** Add `ConfigureAwait(false)` to all async methods
- [ ] **2.5** Implement parallel threshold logic (4+ items)

### Phase 3: Integration (Priority: P0)

- [ ] **3.1** Update `SyntaxComparer` to use `RecursiveTreeComparer` internally
- [ ] **3.2** Update `CSharpDiffer` to use new comparer
- [ ] **3.3** Update `VisualBasicDiffer` to use new comparer (if applicable)
- [ ] **3.4** Update output formatters to handle hierarchical changes

### Phase 4: Test Updates (Priority: P0)

- [ ] **4.1** Update `CSharpDifferTests` assertions for hierarchical output
- [ ] **4.2** Update `SyntaxComparerTests` (if exists)
- [ ] **4.3** Update edge case tests (whitespace, encoding, etc.)
- [ ] **4.4** Create new `RecursiveTreeComparerTests` class
- [ ] **4.5** Create performance tests for large files
- [ ] **4.6** Verify 27 validation tests now pass

### Phase 5: Documentation (Priority: P1)

- [ ] **5.1** Update `docs/architecture.md` with new algorithm description
- [ ] **5.2** Update `docs/api.md` with new interfaces and methods
- [ ] **5.3** Add migration notes to README
- [ ] **5.4** Update code comments and XML documentation

### Phase 6: Performance Validation (Priority: P1)

- [ ] **6.1** Create benchmark project with BenchmarkDotNet
- [ ] **6.2** Run benchmarks comparing old vs new algorithm
- [ ] **6.3** Document performance improvements
- [ ] **6.4** Tune parallel threshold based on benchmarks

### Phase 7: Cleanup (Priority: P2)

- [ ] **7.1** Remove deprecated code after verification
- [ ] **7.2** Remove old `CompareChildren()` method from `SyntaxComparer`
- [ ] **7.3** Clean up any unused helper methods
- [ ] **7.4** Final code review and refactoring

---

## 13. Risk Assessment

### 13.1 Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing consumers | Medium | High | Provide `Flatten()` extension; gradual deprecation |
| Performance regression in edge cases | Low | Medium | Benchmark extensively; keep old code path available |
| Parallel execution bugs | Medium | High | Extensive testing; configurable parallelism |
| Stack overflow on deep nesting | Low | High | Add depth limit; convert to iterative if needed |
| Cancellation race conditions | Low | Medium | Careful testing; use established patterns |

### 13.2 Rollback Plan

If critical issues discovered:
1. Revert `SyntaxComparer` to use old algorithm
2. Keep `RecursiveTreeComparer` as opt-in alternative
3. Fix issues and re-deploy

---

## 14. Appendix

### 14.1 Glossary

| Term | Definition |
|------|------------|
| **Structural Node** | A syntax node representing a named code element (class, method, property, field) |
| **Sibling Matching** | Matching nodes at the same tree level (children of the same parent) |
| **Subtree Equivalence** | Two nodes and all their descendants are structurally identical |
| **Hierarchical Output** | Changes nested to mirror code structure (Namespace > Class > Method) |
| **Flat Output** | All changes at top level, no nesting |

### 14.2 References

- [Roslyn SyntaxTree API](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis)
- [ValueTask Best Practices](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)
- [Tree Diff Algorithms](https://en.wikipedia.org/wiki/Tree_diff)
- [BUG-003 Discovery Report](../FINAL_VALIDATION_RESULTS.md)

### 14.3 Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-17 | ARCH-DIFF | Initial design document |

---

**End of Design Document**
