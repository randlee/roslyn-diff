// Test fixture: Class removal detection
// Expected: Class "DeprecatedHelper" should be detected as Removed
namespace TestFixtures;

/// <summary>
/// The main service class that will remain.
/// </summary>
public class MainService
{
    public void Execute() { }
}

/// <summary>
/// A deprecated helper class that will be removed.
/// </summary>
public class DeprecatedHelper
{
    public void OldMethod() { }
}
