// Test fixture: Property removal detection
// Expected: Property "LegacyId" should be detected as Removed
namespace TestFixtures;

/// <summary>
/// An entity class for testing property removal.
/// </summary>
public class Entity
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
