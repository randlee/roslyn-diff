namespace RoslynDiff.Core.Differ;

using RoslynDiff.Core.Models;

/// <summary>
/// Classifies file types by their whitespace sensitivity.
/// </summary>
public static class LanguageClassifier
{
    private static readonly HashSet<string> SignificantExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".py",      // Python
        ".pyw",     // Python Windows
        ".yaml",    // YAML
        ".yml",     // YAML
        ".fs",      // F#
        ".fsx",     // F# Script
        ".fsi",     // F# Signature
        ".nim",     // Nim
        ".haml",    // Haml
        ".pug",     // Pug/Jade
        ".jade",    // Jade
        ".coffee",  // CoffeeScript
        ".slim",    // Slim
        ".sass",    // Sass (indented syntax)
        ".styl",    // Stylus
    };

    private static readonly HashSet<string> SignificantFilenames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Makefile",
        "GNUmakefile",
        "makefile",
    };

    private static readonly HashSet<string> InsignificantExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",      // C#
        ".vb",      // VB.NET
        ".java",    // Java
        ".js",      // JavaScript
        ".jsx",     // JSX
        ".ts",      // TypeScript
        ".tsx",     // TSX
        ".go",      // Go
        ".rs",      // Rust
        ".c",       // C
        ".cpp",     // C++
        ".h",       // C Header
        ".hpp",     // C++ Header
        ".json",    // JSON
        ".xml",     // XML
        ".html",    // HTML
        ".htm",     // HTML
        ".css",     // CSS
        ".scss",    // SCSS (not indented syntax)
        ".less",    // LESS
        ".php",     // PHP
        ".rb",      // Ruby (has significant whitespace in some cases but generally brace-like)
        ".swift",   // Swift
        ".kt",      // Kotlin
        ".scala",   // Scala
        ".sql",     // SQL
        ".sh",      // Shell
        ".bash",    // Bash
        ".zsh",     // Zsh
        ".ps1",     // PowerShell
        ".psm1",    // PowerShell Module
    };

    /// <summary>
    /// Gets the whitespace sensitivity classification for a file based on its path.
    /// </summary>
    /// <param name="filePath">The file path to classify.</param>
    /// <returns>The whitespace sensitivity classification.</returns>
    public static WhitespaceSensitivity GetSensitivity(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return WhitespaceSensitivity.Unknown;

        var fileName = Path.GetFileName(filePath);
        if (SignificantFilenames.Contains(fileName))
            return WhitespaceSensitivity.Significant;

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
            return WhitespaceSensitivity.Unknown;

        if (SignificantExtensions.Contains(extension))
            return WhitespaceSensitivity.Significant;

        if (InsignificantExtensions.Contains(extension))
            return WhitespaceSensitivity.Insignificant;

        // Unknown extensions - return Unknown for safety
        return WhitespaceSensitivity.Unknown;
    }

    /// <summary>
    /// Determines if a file path represents a whitespace-significant language.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the language is whitespace-significant; otherwise, false.</returns>
    public static bool IsWhitespaceSignificant(string? filePath)
    {
        return GetSensitivity(filePath) == WhitespaceSensitivity.Significant;
    }
}
