// Test fixture: Property modification detection (auto-property to backing field)
// Expected: Property "Value" should be detected as Modified
//           Changed from auto-property to property with backing field and validation
namespace TestFixtures;

/// <summary>
/// A configuration class for testing property modification.
/// </summary>
public class Configuration
{
    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the configuration name.
    /// </summary>
    public string Name { get; set; } = "Default";
}
