// Test fixture: Mixed impact level changes
// Expected: Contains examples of all impact levels
namespace TestFixtures;

/// <summary>
/// A service class demonstrating various impact levels.
/// </summary>
public class DataService
{
    private int _internalCounter;

    /// <summary>
    /// Public API: Processes data with the given count.
    /// </summary>
    public string ProcessData(int count)
    {
        return $"Processed {count} items";
    }

    /// <summary>
    /// Internal API: Gets the internal state.
    /// </summary>
    internal int GetInternalState()
    {
        return _internalCounter;
    }

    /// <summary>
    /// Private method: Helper for calculations.
    /// </summary>
    private int CalculateValue(int input)
    {
        return input * 2;
    }
}
