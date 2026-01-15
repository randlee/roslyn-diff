// Test fixture: Method body modification detection
// Expected: Method "ProcessData" should be detected as Modified (body changed)
namespace TestFixtures;

/// <summary>
/// A processor class for testing method body modification.
/// </summary>
public class StringProcessor
{
    /// <summary>
    /// Processes the input string.
    /// </summary>
    public string ProcessData(string input)
    {
        // Original implementation: simple uppercase
        return input.ToUpper();
    }
}
