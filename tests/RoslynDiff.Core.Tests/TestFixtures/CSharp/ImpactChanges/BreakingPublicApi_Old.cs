// Test fixture: Breaking public API change
// Expected: Public method signature change detected as BreakingPublicApi
namespace TestFixtures;

/// <summary>
/// A calculator class with public API.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two integers.
    /// </summary>
    public int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts two integers.
    /// </summary>
    public int Subtract(int a, int b)
    {
        return a - b;
    }
}
