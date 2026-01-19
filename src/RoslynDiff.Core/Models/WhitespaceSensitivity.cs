namespace RoslynDiff.Core.Models;

/// <summary>
/// Classifies languages by their whitespace sensitivity for diff purposes.
/// </summary>
public enum WhitespaceSensitivity
{
    /// <summary>
    /// Unknown or unclassified language. Uses exact comparison for safety.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Whitespace is semantically significant (Python, YAML, Makefile, F#).
    /// Indentation changes may break program correctness.
    /// </summary>
    Significant = 1,

    /// <summary>
    /// Whitespace is generally insignificant (C#, Java, JavaScript, Go).
    /// Formatting changes don't affect program behavior.
    /// </summary>
    Insignificant = 2
}
