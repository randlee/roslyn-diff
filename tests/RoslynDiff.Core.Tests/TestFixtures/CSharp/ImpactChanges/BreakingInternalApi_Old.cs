// Test fixture: Breaking internal API change
// Expected: Internal method change detected as BreakingInternalApi
namespace TestFixtures;

/// <summary>
/// A utility class with internal API.
/// </summary>
public class InternalUtility
{
    /// <summary>
    /// Internal method for validation.
    /// </summary>
    internal bool ValidateInput(string input)
    {
        return !string.IsNullOrEmpty(input);
    }

    /// <summary>
    /// Internal method for processing.
    /// </summary>
    internal string ProcessInternal(string data)
    {
        return data.Trim();
    }
}
