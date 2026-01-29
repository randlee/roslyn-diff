namespace MathLib.Utilities;

/// <summary>
/// Helper methods for string manipulation
/// </summary>
public class StringHelper
{
    /// <summary>
    /// Converts string to uppercase
    /// </summary>
    public static string ToUpperCase(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        return input.ToUpper();
    }

    /// <summary>
    /// Converts string to lowercase
    /// </summary>
    public static string ToLowerCase(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        return input.ToLower();
    }

    /// <summary>
    /// Reverses a string
    /// </summary>
    public static string Reverse(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
