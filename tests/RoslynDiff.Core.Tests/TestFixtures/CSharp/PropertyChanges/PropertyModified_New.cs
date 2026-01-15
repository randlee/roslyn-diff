// Test fixture: Property modification detection (auto-property to backing field)
// Expected: Property "Value" should be detected as Modified
//           Changed from auto-property to property with backing field and validation
namespace TestFixtures;

/// <summary>
/// A configuration class for testing property modification.
/// </summary>
public class Configuration
{
    private int _value;

    /// <summary>
    /// Gets or sets the configuration value with validation.
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative");
            _value = value;
        }
    }

    /// <summary>
    /// Gets or sets the configuration name.
    /// </summary>
    public string Name { get; set; } = "Default";
}
