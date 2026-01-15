namespace RoslynDiff.Cli;

/// <summary>
/// Parses class specification in format "file.cs:ClassName" or "file.cs".
/// </summary>
public static class ClassSpecParser
{
    /// <summary>
    /// Represents the result of parsing a class specification.
    /// </summary>
    /// <param name="FilePath">The file path from the specification.</param>
    /// <param name="ClassName">The class name from the specification, or null if not specified.</param>
    public record ClassSpec(string FilePath, string? ClassName);

    /// <summary>
    /// Parses a class specification string.
    /// </summary>
    /// <param name="spec">The specification string in format "file.cs:ClassName" or "file.cs".</param>
    /// <returns>A tuple containing the file path and optional class name.</returns>
    /// <exception cref="ArgumentException">Thrown when the specification is null, empty, or invalid.</exception>
    public static (string FilePath, string? ClassName) Parse(string spec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spec);

        var result = ParseToClassSpec(spec);
        return (result.FilePath, result.ClassName);
    }

    /// <summary>
    /// Parses a class specification string into a <see cref="ClassSpec"/> record.
    /// </summary>
    /// <param name="spec">The specification string in format "file.cs:ClassName" or "file.cs".</param>
    /// <returns>A <see cref="ClassSpec"/> containing the parsed file path and optional class name.</returns>
    /// <exception cref="ArgumentException">Thrown when the specification is null, empty, or invalid.</exception>
    public static ClassSpec ParseToClassSpec(string spec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spec);

        // Handle Windows-style paths with drive letters (e.g., C:\path\file.cs:ClassName)
        // We need to find a colon that's not part of a drive letter specification
        var colonIndex = FindClassNameSeparator(spec);

        if (colonIndex == -1)
        {
            // No class name specified
            return new ClassSpec(spec.Trim(), null);
        }

        var filePath = spec[..colonIndex].Trim();
        var className = spec[(colonIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty in class specification.", nameof(spec));
        }

        if (string.IsNullOrWhiteSpace(className))
        {
            throw new ArgumentException("Class name cannot be empty when specified with colon separator.", nameof(spec));
        }

        // Validate class name is a valid C# identifier
        if (!IsValidClassName(className))
        {
            throw new ArgumentException($"Invalid class name: '{className}'. Must be a valid C# identifier.", nameof(spec));
        }

        return new ClassSpec(filePath, className);
    }

    /// <summary>
    /// Finds the index of the colon that separates the file path from the class name.
    /// Handles Windows drive letter colons (e.g., C:) correctly.
    /// </summary>
    private static int FindClassNameSeparator(string spec)
    {
        // Start searching after potential drive letter (index 2 onwards for "C:\...")
        var startIndex = 0;

        // If it looks like a Windows path with a drive letter, skip past it
        if (spec.Length >= 2 && char.IsLetter(spec[0]) && spec[1] == ':')
        {
            startIndex = 2;
        }

        // Find the last colon (class name separator should be the last one)
        var lastColonIndex = spec.LastIndexOf(':');

        if (lastColonIndex < startIndex)
        {
            return -1;
        }

        return lastColonIndex;
    }

    /// <summary>
    /// Validates that a string is a valid C# class name (identifier).
    /// </summary>
    private static bool IsValidClassName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        // First character must be a letter or underscore
        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            return false;
        }

        // Remaining characters must be letters, digits, or underscores
        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }
}
