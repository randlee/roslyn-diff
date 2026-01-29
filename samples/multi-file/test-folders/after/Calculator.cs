namespace MathLib;

/// <summary>
/// Provides basic calculator operations
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two integers
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Sum of a and b</returns>
    public int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts one integer from another
    /// </summary>
    public int Subtract(int a, int b)
    {
        return a - b;
    }

    /// <summary>
    /// Multiplies two integers
    /// </summary>
    public int Multiply(int a, int b)
    {
        return a * b;
    }
}
