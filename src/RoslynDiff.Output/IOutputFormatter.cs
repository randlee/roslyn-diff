namespace RoslynDiff.Output;

using RoslynDiff.Core.Models;

/// <summary>
/// Defines the contract for formatting diff results into various output formats.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Gets the name of the output format (e.g., "json", "unified", "html").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Formats the diff result into a string representation.
    /// </summary>
    /// <param name="result">The diff result to format.</param>
    /// <param name="options">Options controlling the output format.</param>
    /// <returns>A formatted string representation of the diff result.</returns>
    string Format(DiffResult result, OutputOptions? options = null);

    /// <summary>
    /// Formats the diff result and writes it to the specified writer.
    /// </summary>
    /// <param name="result">The diff result to format.</param>
    /// <param name="writer">The text writer to write the output to.</param>
    /// <param name="options">Options controlling the output format.</param>
    Task FormatAsync(DiffResult result, TextWriter writer, OutputOptions? options = null);
}
