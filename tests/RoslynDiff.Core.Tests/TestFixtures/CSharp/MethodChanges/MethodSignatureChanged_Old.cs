// Test fixture: Method signature change detection
// Expected: Method "Calculate" should be detected as Modified (signature changed)
//           Parameters changed from (int, int) to (double, double, bool)
namespace TestFixtures;

/// <summary>
/// A calculator class for testing method signature changes.
/// </summary>
public class AdvancedCalculator
{
    /// <summary>
    /// Performs a calculation with original signature.
    /// </summary>
    public int Calculate(int a, int b)
    {
        return a + b;
    }
}
