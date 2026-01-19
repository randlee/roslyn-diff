// Test fixture: Formatting-only changes
// Expected: Whitespace changes detected as FormattingOnly
namespace TestFixtures;

/// <summary>
/// A simple class for formatting tests.
/// </summary>
public class FormattingTest
{
    /// <summary>
    /// A simple method.
    /// </summary>
    public int Calculate(int a,int b)
    {
        return a+b;
    }

    /// <summary>
    /// Another method.
    /// </summary>
    public string Format(string   value)
    {
        return   value.Trim();
    }
}
