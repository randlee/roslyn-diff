// Test fixture: Method removal detection
// Expected: Method "DeprecatedCalculation" should be detected as Removed
namespace TestFixtures;

/// <summary>
/// A service class for testing method removal.
/// </summary>
public class DataService
{
    /// <summary>
    /// Fetches data from the source.
    /// </summary>
    public string FetchData(string source)
    {
        return $"Data from {source}";
    }
}
