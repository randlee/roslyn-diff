namespace Samples;

/// <summary>
/// A simple calculator class for basic arithmetic operations.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two numbers together.
    /// </summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The sum of a and b.</returns>
    public int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts the second number from the first.
    /// </summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The difference of a and b.</returns>
    public int Subtract(int a, int b)
    {
        return a - b;
    }

    /// <summary>
    /// Multiplies two numbers.
    /// </summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The product of a and b.</returns>
    public int Multiply(int a, int b)
    {
        return a * b;
    }

    /// <summary>
    /// Divides the first number by the second.
    /// </summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The quotient of a divided by b.</returns>
    /// <exception cref="DivideByZeroException">Thrown when b is zero.</exception>
    public int Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }
        return a / b;
    }
}
