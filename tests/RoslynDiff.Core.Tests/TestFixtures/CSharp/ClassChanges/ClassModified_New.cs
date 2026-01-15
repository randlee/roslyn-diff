// Test fixture: Class modification detection
// Expected: Class "DataProcessor" should be detected as Modified
//           (base class added, interface implemented)
namespace TestFixtures;

/// <summary>
/// A data processor class with modified implementation.
/// Now inherits from BaseProcessor and implements IProcessor.
/// </summary>
public class DataProcessor : BaseProcessor, IProcessor
{
    private readonly string _name;
    private readonly ILogger _logger;

    public DataProcessor(string name, ILogger logger)
    {
        _name = name;
        _logger = logger;
    }

    public override string Process(string input)
    {
        _logger.Log($"Processing: {input}");
        return input.ToUpper().Trim();
    }
}

public abstract class BaseProcessor
{
    public abstract string Process(string input);
}

public interface IProcessor
{
    string Process(string input);
}

public interface ILogger
{
    void Log(string message);
}
