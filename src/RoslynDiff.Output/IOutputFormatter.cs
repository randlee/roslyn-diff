namespace RoslynDiff.Output;

using RoslynDiff.Core.Models;

/// <summary>
/// Defines the contract for formatting diff results into various output formats.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Gets the format identifier (e.g., "json", "html", "text").
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets the MIME content type for this format (e.g., "application/json", "text/html", "text/plain").
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Formats the diff result into a string representation.
    /// </summary>
    /// <param name="result">The diff result to format.</param>
    /// <param name="options">Options controlling the output format.</param>
    /// <returns>A formatted string representation of the diff result.</returns>
    string FormatResult(DiffResult result, OutputOptions? options = null);

    /// <summary>
    /// Formats the diff result and writes it to the specified writer.
    /// </summary>
    /// <param name="result">The diff result to format.</param>
    /// <param name="writer">The text writer to write the output to.</param>
    /// <param name="options">Options controlling the output format.</param>
    Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null);
}
