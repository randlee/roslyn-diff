// Test fixture: Property addition detection
// Expected: Property "Description" should be detected as Added
namespace TestFixtures;

/// <summary>
/// A product class for testing property addition.
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
