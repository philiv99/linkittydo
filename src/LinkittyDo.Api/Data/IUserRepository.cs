using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Data;

/// <summary>
/// Repository interface for User data access.
/// Implementations can be swapped between JSON file storage, SQL database, etc.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get all users
    /// </summary>
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>
    /// Get a user by their unique ID
    /// </summary>
    Task<User?> GetByIdAsync(string uniqueId);

    /// <summary>
    /// Get a user by their email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Get a user by their name
    /// </summary>
    Task<User?> GetByNameAsync(string name);

    /// <summary>
    /// Create a new user
    /// </summary>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Update an existing user
    /// </summary>
    Task<User?> UpdateAsync(User user);

    /// <summary>
    /// Delete a user by their unique ID
    /// </summary>
    Task<bool> DeleteAsync(string uniqueId);

    /// <summary>
    /// Check if a name is available (optionally excluding a specific user)
    /// </summary>
    Task<bool> IsNameAvailableAsync(string name, string? excludeUserId = null);

    /// <summary>
    /// Check if an email is available (optionally excluding a specific user)
    /// </summary>
    Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null);
}
