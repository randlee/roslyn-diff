// Test fixture: Multiple simultaneous changes detection
// Expected changes:
//   - Class "UserService" Modified (method body changed)
//   - Class "AuditLogger" Removed
//   - Class "UserValidator" Added
//   - Method "GetUser" Modified (async added, return type changed)
//   - Method "DeleteUser" Removed
//   - Method "UpdateUser" Added
//   - Property "CacheTimeout" Removed
//   - Property "MaxRetries" Added
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
    private readonly IUserValidator _validator;

    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    public UserService(IUserValidator validator)
    {
        _validator = validator;
    }

    /// <summary>
    /// Gets a user by ID asynchronously.
    /// </summary>
    public async Task<UserDto?> GetUserAsync(int id)
    {
        await Task.Delay(1); // Simulate async operation
        return _users.TryGetValue(id, out var user) ? user : null;
    }

    /// <summary>
    /// Creates a new user with validation.
    /// </summary>
    public void CreateUser(UserDto user)
    {
        if (!_validator.Validate(user))
            throw new ArgumentException("Invalid user data", nameof(user));
        _users[user.Id] = user;
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    public bool UpdateUser(UserDto user)
    {
        if (!_users.ContainsKey(user.Id))
            return false;

        if (!_validator.Validate(user))
            throw new ArgumentException("Invalid user data", nameof(user));

        _users[user.Id] = user;
        return true;
    }
}

/// <summary>
/// Interface for user validation.
/// </summary>
public interface IUserValidator
{
    bool Validate(UserDto user);
}

/// <summary>
/// A user validator that was added.
/// </summary>
public class UserValidator : IUserValidator
{
    /// <summary>
    /// Validates the user data.
    /// </summary>
    public bool Validate(UserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.Name))
            return false;

        if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
            return false;

        return true;
    }
}
