// Test fixture: Breaking public API change
// Expected: Public method signature change detected as BreakingPublicApi
namespace TestFixtures;

/// <summary>
/// A calculator class with public API.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two integers with optional precision.
    /// </summary>
    public int Add(int a, int b, int precision = 0) // Signature change - added parameter
    {
        return a + b;
    }

    /// <summary>
    /// Computes difference of two integers.
    /// </summary>
    public int Difference(int a, int b) // Renamed from Subtract (breaking change)
    {
        return a - b;
    }
}
