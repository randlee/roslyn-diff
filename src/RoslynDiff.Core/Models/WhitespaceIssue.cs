namespace RoslynDiff.Core.Models;

/// <summary>
/// Types of whitespace issues that can be detected in language-aware mode.
/// </summary>
[Flags]
public enum WhitespaceIssue
{
    /// <summary>
    /// No whitespace issues detected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indentation level changed (significant for Python/YAML).
    /// </summary>
    IndentationChanged = 1 << 0,

    /// <summary>
    /// Mixed tabs and spaces in indentation.
    /// </summary>
    MixedTabsSpaces = 1 << 1,

    /// <summary>
    /// Trailing whitespace added or changed.
    /// </summary>
    TrailingWhitespace = 1 << 2,

    /// <summary>
    /// Line ending style changed (CRLF vs LF).
    /// </summary>
    LineEndingChanged = 1 << 3,

    /// <summary>
    /// Tab width assumption may be incorrect.
    /// </summary>
    AmbiguousTabWidth = 1 << 4
}
