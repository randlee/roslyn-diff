// Test fixture: Class move detection (semantic analysis)
// Expected: "Helper" moved from "OldNamespace" to "NewNamespace" should be detected as Moved
namespace NewNamespace;

/// <summary>
/// A helper class that was moved to this namespace.
/// </summary>
public class Helper
{
    /// <summary>
    /// Formats a message with the given prefix.
    /// </summary>
    public string FormatMessage(string prefix, string message)
    {
        return $"[{prefix}] {message}";
    }

    /// <summary>
    /// Validates the input string.
    /// </summary>
    public bool ValidateInput(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.Length <= 100;
    }
}
