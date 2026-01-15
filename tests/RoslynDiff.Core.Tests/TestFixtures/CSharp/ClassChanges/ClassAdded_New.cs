// Test fixture: Class addition detection
// Expected: New class "Calculator" should be detected as Added
namespace TestFixtures;

/// <summary>
/// A simple math helper class.
/// </summary>
public class MathHelper
{
    public int Add(int a, int b) => a + b;
}

/// <summary>
/// A calculator class that was added in this version.
/// </summary>
public class Calculator
{
    public int Multiply(int a, int b) => a * b;
}
