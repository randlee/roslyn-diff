namespace RoslynDiff.Core.Differ;

using RoslynDiff.Core.Models;

/// <summary>
/// Analyzes whitespace differences to detect potential issues in whitespace-significant languages.
/// </summary>
public static class WhitespaceAnalyzer
{
    /// <summary>
    /// Analyzes a pair of lines for whitespace issues.
    /// </summary>
    /// <param name="oldLine">The original line (null if added).</param>
    /// <param name="newLine">The new line (null if removed).</param>
    /// <returns>Detected whitespace issues as flags.</returns>
    public static WhitespaceIssue Analyze(string? oldLine, string? newLine)
    {
        var issues = WhitespaceIssue.None;

        // Cannot analyze if either line is null (pure addition or deletion)
        if (oldLine == null || newLine == null)
        {
            // For new lines only, we can still check for mixed tabs/spaces
            if (newLine != null)
            {
                var newIndent = GetIndentation(newLine);
                if (HasMixedTabsSpaces(newIndent))
                {
                    issues |= WhitespaceIssue.MixedTabsSpaces;
                }
            }
            return issues;
        }

        // Check indentation change
        var oldIndent = GetIndentation(oldLine);
        var newIndent2 = GetIndentation(newLine);

        if (oldIndent.Length != newIndent2.Length || oldIndent != newIndent2)
        {
            issues |= WhitespaceIssue.IndentationChanged;
        }

        // Check for mixed tabs/spaces in new line's indentation
        if (HasMixedTabsSpaces(newIndent2))
        {
            issues |= WhitespaceIssue.MixedTabsSpaces;
        }

        // Check trailing whitespace changes
        var oldTrailing = GetTrailingWhitespace(oldLine);
        var newTrailing = GetTrailingWhitespace(newLine);
        if (oldTrailing != newTrailing)
        {
            issues |= WhitespaceIssue.TrailingWhitespace;
        }

        return issues;
    }

    /// <summary>
    /// Gets the leading whitespace (indentation) from a line.
    /// </summary>
    /// <param name="line">The line to extract indentation from.</param>
    /// <returns>The leading whitespace string containing only spaces and tabs.</returns>
    public static string GetIndentation(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
        {
            i++;
        }
        return line[..i];
    }

    /// <summary>
    /// Gets the trailing whitespace from a line.
    /// </summary>
    /// <param name="line">The line to extract trailing whitespace from.</param>
    /// <returns>The trailing whitespace string.</returns>
    public static string GetTrailingWhitespace(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var i = line.Length;
        while (i > 0 && char.IsWhiteSpace(line[i - 1]))
        {
            i--;
        }
        return line[i..];
    }

    /// <summary>
    /// Checks if indentation contains mixed tabs and spaces.
    /// </summary>
    /// <param name="indent">The indentation string to check.</param>
    /// <returns>True if the indentation contains both tabs and spaces; otherwise, false.</returns>
    public static bool HasMixedTabsSpaces(string indent)
    {
        ArgumentNullException.ThrowIfNull(indent);

        return indent.Contains(' ') && indent.Contains('\t');
    }

    /// <summary>
    /// Calculates the visual width of indentation assuming a given tab width.
    /// </summary>
    /// <param name="indent">The indentation string to measure.</param>
    /// <param name="tabWidth">The number of spaces a tab represents (default: 4).</param>
    /// <returns>The visual width in columns.</returns>
    public static int CalculateIndentWidth(string indent, int tabWidth = 4)
    {
        ArgumentNullException.ThrowIfNull(indent);

        if (tabWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tabWidth), "Tab width must be positive.");
        }

        var width = 0;
        foreach (var c in indent)
        {
            if (c == '\t')
            {
                // Tab moves to the next tab stop
                width = ((width / tabWidth) + 1) * tabWidth;
            }
            else if (c == ' ')
            {
                width++;
            }
            // Other characters are not typically part of indentation,
            // but if present, treat them as single-width
            else
            {
                width++;
            }
        }
        return width;
    }

    /// <summary>
    /// Detects if line ending changed between old and new content.
    /// </summary>
    /// <param name="oldContent">The original content.</param>
    /// <param name="newContent">The new content.</param>
    /// <returns>True if line ending style changed; otherwise, false.</returns>
    public static bool DetectLineEndingChange(string oldContent, string newContent)
    {
        ArgumentNullException.ThrowIfNull(oldContent);
        ArgumentNullException.ThrowIfNull(newContent);

        var oldEnding = DetectLineEnding(oldContent);
        var newEnding = DetectLineEnding(newContent);

        return oldEnding != newEnding;
    }

    /// <summary>
    /// Detects the dominant line ending style in content.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <returns>The detected line ending style.</returns>
    private static LineEndingStyle DetectLineEnding(string content)
    {
        var crlfCount = 0;
        var lfCount = 0;
        var crCount = 0;

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\r')
            {
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    crlfCount++;
                    i++; // Skip the \n
                }
                else
                {
                    crCount++;
                }
            }
            else if (content[i] == '\n')
            {
                lfCount++;
            }
        }

        // Return the dominant line ending style
        if (crlfCount >= lfCount && crlfCount >= crCount)
        {
            return crlfCount > 0 ? LineEndingStyle.CRLF : LineEndingStyle.None;
        }

        if (lfCount >= crCount)
        {
            return lfCount > 0 ? LineEndingStyle.LF : LineEndingStyle.None;
        }

        return crCount > 0 ? LineEndingStyle.CR : LineEndingStyle.None;
    }

    /// <summary>
    /// Represents different line ending styles.
    /// </summary>
    private enum LineEndingStyle
    {
        /// <summary>No line endings detected.</summary>
        None,

        /// <summary>Unix-style line endings (LF).</summary>
        LF,

        /// <summary>Windows-style line endings (CRLF).</summary>
        CRLF,

        /// <summary>Old Mac-style line endings (CR only).</summary>
        CR
    }
}
