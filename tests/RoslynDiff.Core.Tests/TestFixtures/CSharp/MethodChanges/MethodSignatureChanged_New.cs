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
    /// Performs a calculation with modified signature.
    /// Added precision parameter and changed to double.
    /// </summary>
    public double Calculate(double a, double b, bool roundResult = false)
    {
        var result = a + b;
        return roundResult ? Math.Round(result) : result;
    }
}
