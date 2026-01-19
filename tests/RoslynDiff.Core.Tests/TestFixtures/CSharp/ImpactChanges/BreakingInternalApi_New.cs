// Test fixture: Breaking internal API change
// Expected: Internal method change detected as BreakingInternalApi
namespace TestFixtures;

/// <summary>
/// A utility class with internal API.
/// </summary>
public class InternalUtility
{
    /// <summary>
    /// Internal method for validation with length check.
    /// </summary>
    internal bool ValidateInput(string input, int minLength) // Signature change
    {
        return !string.IsNullOrEmpty(input) && input.Length >= minLength;
    }

    /// <summary>
    /// Internal method for data transformation.
    /// </summary>
    internal string TransformInternal(string data) // Renamed from ProcessInternal
    {
        return data.Trim();
    }
}
