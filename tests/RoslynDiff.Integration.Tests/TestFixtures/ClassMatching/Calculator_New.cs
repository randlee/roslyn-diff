namespace TestFixtures;

/// <summary>
/// A simple calculator class for testing class matching - updated version.
/// </summary>
public class Calculator : ICalculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
}

public interface ICalculator
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    int Multiply(int a, int b);
}
