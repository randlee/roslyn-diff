// Test fixture: Mixed impact level changes
// Expected: Contains examples of all impact levels
namespace TestFixtures;

/// <summary>
/// A service class demonstrating various impact levels.
/// </summary>
public class DataService
{
    private int _counter; // Renamed from _internalCounter (NonBreaking - private member)

    /// <summary>
    /// Public API: Processes data with the given count and validation flag.
    /// </summary>
    public string ProcessData(int count, bool validate) // Signature change (BreakingPublicApi)
    {
        if (validate && count < 0)
            throw new ArgumentException("Count must be non-negative");
        return $"Processed {count} items";
    }

    /// <summary>
    /// Internal API: Gets the internal state value.
    /// </summary>
    internal int GetStateValue() // Renamed from GetInternalState (BreakingInternalApi)
    {
        return _counter;
    }

    /// <summary>
    /// Private method: Helper for calculations.
    /// </summary>
    private int ComputeValue(int value) // Renamed from CalculateValue (NonBreaking - private method)
    {
        return value * 2;
    }
}
