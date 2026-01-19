namespace RoslynDiff.Core.Models;

/// <summary>
/// Specifies how whitespace differences should be handled during diff comparison.
/// </summary>
public enum WhitespaceMode
{
    /// <summary>
    /// Exact character-by-character comparison. Matches standard 'diff' command behavior.
    /// This is the default to ensure compatibility with existing workflows.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Ignore leading and trailing whitespace on each line.
    /// Equivalent to current DiffPlex behavior with ignoreWhitespace=true.
    /// Similar to 'diff -b' (ignore changes in amount of whitespace).
    /// </summary>
    IgnoreLeadingTrailing = 1,

    /// <summary>
    /// Collapse all whitespace to single spaces and trim.
    /// Multiple spaces/tabs become single space, leading/trailing removed.
    /// Similar to 'diff -w' (ignore all whitespace).
    /// </summary>
    IgnoreAll = 2,

    /// <summary>
    /// Language-aware whitespace handling.
    /// - Whitespace-significant languages (Python, YAML, Makefile): Preserve exact
    ///   whitespace and flag indentation changes as potentially breaking.
    /// - Brace languages (C#, Java, JavaScript): Safe to normalize formatting.
    /// </summary>
    LanguageAware = 3
}
