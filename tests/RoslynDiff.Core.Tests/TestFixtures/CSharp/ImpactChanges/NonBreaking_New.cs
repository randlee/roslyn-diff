// Test fixture: Non-breaking changes
// Expected: Private member renames detected as NonBreaking
namespace TestFixtures;

/// <summary>
/// A class with private members only.
/// </summary>
public class PrivateService
{
    private int _counter; // Renamed from _count
    private string _serviceName; // Renamed from _name

    /// <summary>
    /// Public constructor.
    /// </summary>
    public PrivateService(string name)
    {
        _serviceName = name;
        _counter = 0;
    }

    /// <summary>
    /// Public method that uses private members.
    /// </summary>
    public string GetInfo()
    {
        return $"{_serviceName}: {_counter}";
    }

    private void Increment() // Renamed from IncrementCounter
    {
        _counter++;
    }

    private void Reset() // Renamed from ResetCounter
    {
        _counter = 0;
    }
}
