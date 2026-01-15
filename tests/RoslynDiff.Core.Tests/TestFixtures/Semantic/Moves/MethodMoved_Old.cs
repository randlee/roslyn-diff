// Test fixture: Method move detection (semantic analysis)
// Expected: "ProcessData" moved from "ClassA" to "ClassB" should be detected as Moved
namespace TestFixtures;

/// <summary>
/// First class containing the method to be moved.
/// </summary>
public class ClassA
{
    /// <summary>
    /// Processes data with validation and transformation.
    /// </summary>
    public string ProcessData(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim();
        var upper = trimmed.ToUpperInvariant();
        return upper;
    }

    public int GetValue()
    {
        return 42;
    }
}

/// <summary>
/// Second class that will receive the moved method.
/// </summary>
public class ClassB
{
    public void DoSomething()
    {
        Console.WriteLine("Doing something");
    }
}
