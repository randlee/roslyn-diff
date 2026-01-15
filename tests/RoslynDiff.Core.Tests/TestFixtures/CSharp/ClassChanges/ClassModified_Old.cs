// Test fixture: Class modification detection
// Expected: Class "DataProcessor" should be detected as Modified
//           (base class added, interface implemented)
namespace TestFixtures;

/// <summary>
/// A data processor class with original implementation.
/// </summary>
public class DataProcessor
{
    private readonly string _name;

    public DataProcessor(string name)
    {
        _name = name;
    }

    public string Process(string input)
    {
        return input.ToUpper();
    }
}
