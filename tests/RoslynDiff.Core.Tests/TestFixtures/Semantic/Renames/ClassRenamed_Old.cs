// Test fixture: Class rename detection (semantic analysis)
// Expected: "OldCalculator" renamed to "NewCalculator" should be detected as Renamed
namespace TestFixtures;

/// <summary>
/// A calculator class that will be renamed.
/// </summary>
public class OldCalculator
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
