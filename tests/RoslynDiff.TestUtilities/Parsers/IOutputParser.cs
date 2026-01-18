using RoslynDiff.TestUtilities.Comparers;

namespace RoslynDiff.TestUtilities.Parsers;

/// <summary>
/// Base interface for parsing different output formats.
/// </summary>
public interface IOutputParser
{
    /// <summary>
    /// Gets the output format this parser handles.
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Determines whether the parser can handle the given content.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns><c>true</c> if the parser can handle the content; otherwise, <c>false</c>.</returns>
    bool CanParse(string content);
}

/// <summary>
/// Interface for parsers that extract line numbers and ranges from output.
/// </summary>
public interface ILineNumberParser : IOutputParser
{
    /// <summary>
    /// Extracts all line numbers referenced in the content.
    /// </summary>
    /// <param name="content">The content to parse.</param>
    /// <returns>A collection of line numbers.</returns>
    IEnumerable<int> ExtractLineNumbers(string content);

    /// <summary>
    /// Extracts all line ranges referenced in the content.
    /// </summary>
    /// <param name="content">The content to parse.</param>
    /// <returns>A collection of line ranges.</returns>
    IEnumerable<LineRange> ExtractLineRanges(string content);
}
