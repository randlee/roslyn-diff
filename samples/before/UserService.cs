namespace Samples;

/// <summary>
/// Service for managing user operations.
/// </summary>
public class UserService
{
    private readonly IUserRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="repository">The user repository.</param>
    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    public User? GetUser(int id)
    {
        return _repository.FindById(id);
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <param name="email">The user's email.</param>
    /// <returns>The created user.</returns>
    public User CreateUser(string name, string email)
    {
        var user = new User
        {
            Name = name,
            Email = email
        };
        return _repository.Add(user);
    }

    /// <summary>
    /// Deletes a user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    public void DeleteUser(int id)
    {
        _repository.Delete(id);
    }
}

public interface IUserRepository
{
    User? FindById(int id);
    User Add(User user);
    void Delete(int id);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
