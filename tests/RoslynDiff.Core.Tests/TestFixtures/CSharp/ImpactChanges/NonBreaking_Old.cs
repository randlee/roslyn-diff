// Test fixture: Non-breaking changes
// Expected: Private member renames detected as NonBreaking
namespace TestFixtures;

/// <summary>
/// A class with private members only.
/// </summary>
public class PrivateService
{
    private int _count;
    private string _name;

    /// <summary>
    /// Public constructor.
    /// </summary>
    public PrivateService(string name)
    {
        _name = name;
        _count = 0;
    }

    /// <summary>
    /// Public method that uses private members.
    /// </summary>
    public string GetInfo()
    {
        return $"{_name}: {_count}";
    }

    private void IncrementCounter()
    {
        _count++;
    }

    private void ResetCounter()
    {
        _count = 0;
    }
}
