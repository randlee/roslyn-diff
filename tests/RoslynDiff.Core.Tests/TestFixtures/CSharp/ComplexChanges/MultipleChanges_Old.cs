// Test fixture: Multiple simultaneous changes detection
// Expected changes:
//   - Class "UserService" Modified (method body changed)
//   - Class "AuditLogger" Removed
//   - Method "GetUser" Modified (async added, return type changed)
//   - Method "DeleteUser" Removed
//   - Property "CacheTimeout" Removed
//   - Record "UserDto" should remain Unchanged
namespace TestFixtures;

/// <summary>
/// A user data transfer object.
/// </summary>
public record UserDto(int Id, string Name, string Email);

/// <summary>
/// A service class for user operations with multiple elements.
/// </summary>
public class UserService
{
    private readonly Dictionary<int, UserDto> _users = new();

    /// <summary>
    /// Gets or sets the cache timeout in seconds.
    /// </summary>
    public int CacheTimeout { get; set; } = 300;

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    public UserDto? GetUser(int id)
    {
        return _users.TryGetValue(id, out var user) ? user : null;
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    public void CreateUser(UserDto user)
    {
        _users[user.Id] = user;
    }

    /// <summary>
    /// Deletes a user by ID.
    /// </summary>
    public bool DeleteUser(int id)
    {
        return _users.Remove(id);
    }
}

/// <summary>
/// An audit logger that will be removed.
/// </summary>
public class AuditLogger
{
    public void Log(string action, int userId)
    {
        Console.WriteLine($"[AUDIT] {action} for user {userId}");
    }
}
