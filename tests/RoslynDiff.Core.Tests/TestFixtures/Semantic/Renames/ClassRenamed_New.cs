// Test fixture: Class rename detection (semantic analysis)
// Expected: "OldCalculator" renamed to "NewCalculator" should be detected as Renamed
namespace TestFixtures;

/// <summary>
/// A calculator class that was renamed.
/// </summary>
public class NewCalculator
{
    /// <summary>
    /// Adds two numbers.
    /// </summary>
    public int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts the second number from the first.
    /// </summary>
    public int Subtract(int a, int b)
    {
        return a - b;
    }
}
