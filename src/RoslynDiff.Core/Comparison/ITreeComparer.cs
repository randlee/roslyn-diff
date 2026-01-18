namespace RoslynDiff.Core.Comparison;

using Microsoft.CodeAnalysis;
using RoslynDiff.Core.Models;

/// <summary>
/// Defines the contract for recursive tree comparison with async support.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides both synchronous and asynchronous methods for comparing
/// syntax trees using recursive tree diff algorithms. Implementations may leverage
/// parallel processing for improved performance on large syntax trees.
/// </para>
/// <para>
/// The comparison algorithm produces a list of <see cref="Change"/> objects that
/// describe structural differences between the old and new trees.
/// </para>
/// </remarks>
public interface ITreeComparer
{
    /// <summary>
    /// Compares two syntax trees asynchronously using recursive tree diff.
    /// </summary>
    /// <param name="oldTree">The original syntax tree.</param>
    /// <param name="newTree">The new syntax tree to compare against the original.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a read-only list of <see cref="Change"/>
    /// objects representing all detected differences.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="oldTree"/>, <paramref name="newTree"/>, or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    ValueTask<IReadOnlyList<Change>> CompareAsync(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two syntax trees synchronously using recursive tree diff.
    /// </summary>
    /// <param name="oldTree">The original syntax tree.</param>
    /// <param name="newTree">The new syntax tree to compare against the original.</param>
    /// <param name="options">Options controlling the comparison behavior.</param>
    /// <returns>
    /// A read-only list of <see cref="Change"/> objects representing all detected differences.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="oldTree"/>, <paramref name="newTree"/>, or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// This synchronous overload is provided for backward compatibility and simpler use cases.
    /// For large syntax trees or when responsiveness is important, prefer <see cref="CompareAsync"/>.
    /// </remarks>
    IReadOnlyList<Change> Compare(
        SyntaxTree oldTree,
        SyntaxTree newTree,
        DiffOptions options);
}
