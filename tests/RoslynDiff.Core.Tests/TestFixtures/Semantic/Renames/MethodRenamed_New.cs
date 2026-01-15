// Test fixture: Method rename detection (semantic analysis)
// Expected: "Calculate" renamed to "ComputeResult" should be detected as Renamed
namespace TestFixtures;

/// <summary>
/// A processor class for testing method rename detection.
/// </summary>
public class DataProcessor
{
    private readonly int _multiplier;

    public DataProcessor(int multiplier)
    {
        _multiplier = multiplier;
    }

    /// <summary>
    /// Computes a result based on input value.
    /// </summary>
    public int ComputeResult(int value)
    {
        var intermediate = value * _multiplier;
        var result = intermediate + 10;
        return result;
    }
}
