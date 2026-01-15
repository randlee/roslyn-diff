// Test fixture: Method addition detection
// Expected: Method "Divide" should be detected as Added
namespace TestFixtures;

/// <summary>
/// A calculator class for testing method addition.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Multiplies two numbers.
    /// </summary>
    public int Multiply(int a, int b)
    {
        return a * b;
    }

    /// <summary>
    /// Divides two numbers with zero-check.
    /// </summary>
    public double Divide(double a, double b)
    {
        if (b == 0)
            throw new DivideByZeroException();
        return a / b;
    }
}
