using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IUserService
{
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task<User?> GetUserByIdAsync(string uniqueId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByNameAsync(string name);
    Task<User?> UpdateUserAsync(string uniqueId, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(string uniqueId);
    Task<bool> IsNameAvailableAsync(string name, string? excludeUserId = null);
    Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> UpdateDifficultyAsync(string uniqueId, int difficulty);
    Task<User?> AddPointsAsync(string uniqueId, int points);
    Task<User?> AddGameRecordAsync(string uniqueId, GameRecord gameRecord);
    Task<IEnumerable<GameRecord>> GetUserGamesAsync(string uniqueId);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Generates a unique user ID following the format: USR-{timestamp}-{random}
    /// </summary>
    private static string GenerateUniqueId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        return $"USR-{timestamp}-{random}";
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        // Validation is handled by the controller, but double-check uniqueness
        if (!await _repository.IsNameAvailableAsync(request.Name))
        {
            throw new InvalidOperationException("NAME_TAKEN");
        }

        if (!await _repository.IsEmailAvailableAsync(request.Email))
        {
            throw new InvalidOperationException("EMAIL_TAKEN");
        }

        var user = new User
        {
            UniqueId = GenerateUniqueId(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            LifetimePoints = 0,
            PreferredDifficulty = 10,
            Games = new List<GameRecord>(),
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(user);
    }

    public async Task<User?> GetUserByIdAsync(string uniqueId)
    {
        return await _repository.GetByIdAsync(uniqueId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _repository.GetByEmailAsync(email);
    }

    public async Task<User?> GetUserByNameAsync(string name)
    {
        return await _repository.GetByNameAsync(name);
    }

    public async Task<User?> UpdateUserAsync(string uniqueId, UpdateUserRequest request)
    {
        var existingUser = await _repository.GetByIdAsync(uniqueId);
        if (existingUser == null)
        {
            return null;
        }

        // Check if new name is available (excluding current user)
        if (!await _repository.IsNameAvailableAsync(request.Name, uniqueId))
        {
            throw new InvalidOperationException("NAME_TAKEN");
        }

        // Check if new email is available (excluding current user)
        if (!await _repository.IsEmailAvailableAsync(request.Email, uniqueId))
        {
            throw new InvalidOperationException("EMAIL_TAKEN");
        }

        existingUser.Name = request.Name.Trim();
        existingUser.Email = request.Email.Trim().ToLowerInvariant();
        existingUser.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(existingUser);
    }

    public async Task<bool> DeleteUserAsync(string uniqueId)
    {
        return await _repository.DeleteAsync(uniqueId);
    }

    public async Task<bool> IsNameAvailableAsync(string name, string? excludeUserId = null)
    {
        return await _repository.IsNameAvailableAsync(name, excludeUserId);
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null)
    {
        return await _repository.IsEmailAvailableAsync(email, excludeUserId);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<User?> UpdateDifficultyAsync(string uniqueId, int difficulty)
    {
        var user = await _repository.GetByIdAsync(uniqueId);
        if (user == null)
        {
            return null;
        }

        // Validate difficulty range
        if (difficulty < 0 || difficulty > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty), "Difficulty must be between 0 and 100");
        }

        user.PreferredDifficulty = difficulty;
        user.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(user);
    }

    public async Task<User?> AddPointsAsync(string uniqueId, int points)
    {
        var user = await _repository.GetByIdAsync(uniqueId);
        if (user == null)
        {
            return null;
        }

        // Validate points is non-negative
        if (points < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be a non-negative value");
        }

        user.LifetimePoints += points;
        user.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(user);
    }

    public async Task<User?> AddGameRecordAsync(string uniqueId, GameRecord gameRecord)
    {
        var user = await _repository.GetByIdAsync(uniqueId);
        if (user == null)
        {
            return null;
        }

        user.Games.Add(gameRecord);
        user.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(user);
    }

    public async Task<IEnumerable<GameRecord>> GetUserGamesAsync(string uniqueId)
    {
        var user = await _repository.GetByIdAsync(uniqueId);
        if (user == null)
        {
            return Enumerable.Empty<GameRecord>();
        }

        return user.Games.OrderByDescending(g => g.PlayedAt);
    }
}
