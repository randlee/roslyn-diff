namespace RoslynDiff.Core.Tfm;

/// <summary>
/// Provides fast detection of C# preprocessor directives in source code.
/// This is a performance-optimized component used to determine if multi-TFM analysis is needed.
/// </summary>
/// <remarks>
/// This detector performs a conservative text-based scan for preprocessor directives.
/// It intentionally allows false positives (e.g., directives in comments or strings) to maintain
/// simplicity and performance. False negatives are not acceptable as they would cause incorrect
/// analysis results.
/// </remarks>
public static class PreprocessorDirectiveDetector
{
    /// <summary>
    /// Determines whether the given source code content contains C# preprocessor directives.
    /// </summary>
    /// <param name="content">The source code content to analyze.</param>
    /// <returns>
    /// <c>true</c> if preprocessor directives are detected; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method scans for the following C# preprocessor directives:
    /// <list type="bullet">
    /// <item><description>#if - Conditional compilation start</description></item>
    /// <item><description>#elif - Else-if conditional compilation</description></item>
    /// <item><description>#else - Else conditional compilation</description></item>
    /// <item><description>#endif - Conditional compilation end</description></item>
    /// <item><description>#define - Symbol definition</description></item>
    /// <item><description>#undef - Symbol un-definition</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The detection is conservative - directives in comments or strings will still return <c>true</c>.
    /// This is intentional to maintain performance and simplicity. The goal is to quickly identify
    /// files that may require multi-TFM analysis, not to perform a complete syntactic analysis.
    /// </para>
    /// <para>
    /// Performance: This method uses efficient string scanning with minimal allocations,
    /// optimized for the hot path in multi-TFM analysis.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static bool HasPreprocessorDirectives(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Fast path: if there's no '#' character at all, no directives can exist
        if (!content.Contains('#'))
        {
            return false;
        }

        var length = content.Length;
        for (var i = 0; i < length; i++)
        {
            var c = content[i];

            // Skip until we find a '#' character
            if (c != '#')
            {
                continue;
            }

            // Move past the '#' and any whitespace
            var j = i + 1;
            while (j < length && (content[j] == ' ' || content[j] == '\t'))
            {
                j++;
            }

            // Check for the directive keywords
            // We need at least 2 characters for the shortest directive ('if')
            if (j + 2 > length)
            {
                continue;
            }

            // Check for common directives using efficient character-by-character comparison
            // Order by likelihood to optimize common cases
            var remaining = length - j;

            // Check for #if (2 chars) - case insensitive for VB.NET support
            if (remaining >= 2 &&
                (content[j] == 'i' || content[j] == 'I') &&
                (content[j + 1] == 'f' || content[j + 1] == 'F') &&
                (remaining == 2 || !IsIdentifierChar(content[j + 2])))
            {
                return true;
            }

            // Check for #else (4 chars) - case insensitive for VB.NET support
            if (remaining >= 4 &&
                (content[j] == 'e' || content[j] == 'E') &&
                (content[j + 1] == 'l' || content[j + 1] == 'L') &&
                (content[j + 2] == 's' || content[j + 2] == 'S') &&
                (content[j + 3] == 'e' || content[j + 3] == 'E'))
            {
                // Check if it's #else or #elif
                if (remaining == 4 || !IsIdentifierChar(content[j + 4]))
                {
                    return true; // #else
                }
                if (remaining >= 6 &&
                    (content[j + 4] == 'i' || content[j + 4] == 'I') &&
                    (content[j + 5] == 'f' || content[j + 5] == 'F') &&
                    (remaining == 6 || !IsIdentifierChar(content[j + 6])))
                {
                    return true; // #elif
                }
            }

            // Check for #endif (5 chars) - case insensitive for VB.NET support
            if (remaining >= 5 &&
                (content[j] == 'e' || content[j] == 'E') &&
                (content[j + 1] == 'n' || content[j + 1] == 'N') &&
                (content[j + 2] == 'd' || content[j + 2] == 'D') &&
                (content[j + 3] == 'i' || content[j + 3] == 'I') &&
                (content[j + 4] == 'f' || content[j + 4] == 'F') &&
                (remaining == 5 || !IsIdentifierChar(content[j + 5])))
            {
                return true;
            }

            // Check for #define (6 chars) - case insensitive for VB.NET support
            if (remaining >= 6 &&
                (content[j] == 'd' || content[j] == 'D') &&
                (content[j + 1] == 'e' || content[j + 1] == 'E') &&
                (content[j + 2] == 'f' || content[j + 2] == 'F') &&
                (content[j + 3] == 'i' || content[j + 3] == 'I') &&
                (content[j + 4] == 'n' || content[j + 4] == 'N') &&
                (content[j + 5] == 'e' || content[j + 5] == 'E') &&
                (remaining == 6 || !IsIdentifierChar(content[j + 6])))
            {
                return true;
            }

            // Check for #undef (5 chars) - case insensitive for VB.NET support
            if (remaining >= 5 &&
                (content[j] == 'u' || content[j] == 'U') &&
                (content[j + 1] == 'n' || content[j + 1] == 'N') &&
                (content[j + 2] == 'd' || content[j + 2] == 'D') &&
                (content[j + 3] == 'e' || content[j + 3] == 'E') &&
                (content[j + 4] == 'f' || content[j + 4] == 'F') &&
                (remaining == 5 || !IsIdentifierChar(content[j + 5])))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a character can be part of a C# identifier.
    /// Used to ensure we match complete directive keywords, not partial matches.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns><c>true</c> if the character can be part of an identifier; otherwise, <c>false</c>.</returns>
    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}
