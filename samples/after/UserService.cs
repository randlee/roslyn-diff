namespace Samples;

/// <summary>
/// Service for managing user operations with async support.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="repository">The user repository.</param>
    /// <param name="logger">The logger instance.</param>
    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a user by their ID asynchronously.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    public async Task<User?> GetUserAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user with ID {UserId}", id);
        return await _repository.FindByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Creates a new user asynchronously.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user.</returns>
    public async Task<User> CreateUserAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        _logger.LogInformation("Creating user with name {Name}", name);

        var user = new User
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        return await _repository.AddAsync(user, cancellationToken);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user.</returns>
    public async Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        _logger.LogInformation("Updating user with ID {UserId}", user.Id);
        user.UpdatedAt = DateTime.UtcNow;
        return await _repository.UpdateAsync(user, cancellationToken);
    }

    /// <summary>
    /// Deletes a user by ID asynchronously.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user with ID {UserId}", id);
        await _repository.DeleteAsync(id, cancellationToken);
    }
}

public interface IUserService
{
    Task<User?> GetUserAsync(int id, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(string name, string email, CancellationToken cancellationToken = default);
    Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ILogger<T>
{
    void LogInformation(string message, params object[] args);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
