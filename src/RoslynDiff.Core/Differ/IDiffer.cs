namespace RoslynDiff.Core.Differ;

using RoslynDiff.Core.Models;

/// <summary>
/// Defines the contract for comparing code content and producing semantic or line-based diffs.
/// </summary>
public interface IDiffer
{
    /// <summary>
    /// Compares two versions of content and produces a diff result.
    /// </summary>
    /// <param name="oldContent">The original content to compare.</param>
    /// <param name="newContent">The new content to compare against the original.</param>
    /// <param name="options">Options controlling the diff behavior.</param>
    /// <returns>A <see cref="DiffResult"/> containing all detected changes.</returns>
    DiffResult Compare(string oldContent, string newContent, DiffOptions options);

    /// <summary>
    /// Determines whether this differ can handle the specified file.
    /// </summary>
    /// <param name="filePath">The path to the file to be diffed.</param>
    /// <param name="options">Options that may affect whether this differ can handle the file.</param>
    /// <returns><c>true</c> if this differ can process the file; otherwise, <c>false</c>.</returns>
    bool CanHandle(string filePath, DiffOptions options);
}
