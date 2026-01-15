using System.Text.Json;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Data;

/// <summary>
/// JSON file-based implementation of the User repository.
/// Stores user data in individual JSON files within a data directory.
/// </summary>
public class JsonUserRepository : IUserRepository
{
    private readonly string _dataDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonUserRepository(IConfiguration configuration)
    {
        // Use AppContext.BaseDirectory for Azure App Service compatibility
        var baseDir = configuration["DataDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "Data");
        _dataDirectory = Path.Combine(baseDir, "Users");
        
        // Ensure the data directory exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private string GetUserFilePath(string uniqueId) => Path.Combine(_dataDirectory, $"{uniqueId}.json");

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var users = new List<User>();
            var files = Directory.GetFiles(_dataDirectory, "*.json");

            foreach (var file in files)
            {
                var user = await ReadUserFromFileAsync(file);
                if (user != null)
                {
                    users.Add(user);
                }
            }

            return users;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<User?> GetByIdAsync(string uniqueId)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetUserFilePath(uniqueId);
            return await ReadUserFromFileAsync(filePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var users = await GetAllAsync();
        return users.FirstOrDefault(u => 
            u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> GetByNameAsync(string name)
    {
        var normalizedName = name.Trim();
        var users = await GetAllAsync();
        return users.FirstOrDefault(u => 
            u.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User> CreateAsync(User user)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetUserFilePath(user.UniqueId);
            await WriteUserToFileAsync(filePath, user);
            return user;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<User?> UpdateAsync(User user)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetUserFilePath(user.UniqueId);
            if (!File.Exists(filePath))
            {
                return null;
            }

            await WriteUserToFileAsync(filePath, user);
            return user;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(string uniqueId)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetUserFilePath(uniqueId);
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> IsNameAvailableAsync(string name, string? excludeUserId = null)
    {
        var normalizedName = name.Trim();
        var users = await GetAllAsync();
        return !users.Any(u => 
            u.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase) &&
            (excludeUserId == null || !u.UniqueId.Equals(excludeUserId, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var users = await GetAllAsync();
        return !users.Any(u => 
            u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
            (excludeUserId == null || !u.UniqueId.Equals(excludeUserId, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<User?> ReadUserFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<User>(json, _jsonOptions);
    }

    private async Task WriteUserToFileAsync(string filePath, User user)
    {
        var json = JsonSerializer.Serialize(user, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
}
