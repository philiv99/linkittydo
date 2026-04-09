using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace LinkittyDo.Api.Services;

public interface ISiteConfigService
{
    Task<string?> GetValueAsync(string key);
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task SetValueAsync(string key, string value, string? updatedBy = null);
    Task<IList<SiteConfig>> GetAllAsync();
    void InvalidateCache();
}

public class SiteConfigService : ISiteConfigService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private bool _cacheLoaded;

    public SiteConfigService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        await EnsureCacheLoadedAsync();
        return _cache.GetValueOrDefault(key);
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var value = await GetValueAsync(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var value = await GetValueAsync(key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task SetValueAsync(string key, string value, string? updatedBy = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.LinkittyDoDbContext>();

        var config = await dbContext.SiteConfigs.FindAsync(key);
        if (config != null)
        {
            config.Value = value;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedBy = updatedBy;
        }
        else
        {
            dbContext.SiteConfigs.Add(new SiteConfig
            {
                Key = key,
                Value = value,
                ValueType = "string",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = updatedBy
            });
        }

        await dbContext.SaveChangesAsync();
        _cache[key] = value;
    }

    public async Task<IList<SiteConfig>> GetAllAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.LinkittyDoDbContext>();
        return await dbContext.SiteConfigs.OrderBy(c => c.Key).ToListAsync();
    }

    public void InvalidateCache()
    {
        _cache.Clear();
        _cacheLoaded = false;
    }

    private async Task EnsureCacheLoadedAsync()
    {
        if (_cacheLoaded) return;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.LinkittyDoDbContext>();
        var configs = await dbContext.SiteConfigs.ToListAsync();

        foreach (var config in configs)
        {
            _cache[config.Key] = config.Value;
        }
        _cacheLoaded = true;
    }
}

/// <summary>
/// In-memory site config service for JSON provider mode.
/// </summary>
public class InMemorySiteConfigService : ISiteConfigService
{
    private readonly ConcurrentDictionary<string, string> _configs = new(new Dictionary<string, string>
    {
        ["MaxSessionTtlHours"] = "24",
        ["DefaultDifficulty"] = "10",
        ["ClueRetryLimit"] = "5",
        ["MaintenanceMode"] = "false"
    });

    public Task<string?> GetValueAsync(string key) =>
        Task.FromResult(_configs.GetValueOrDefault(key));

    public Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var value = _configs.GetValueOrDefault(key);
        return Task.FromResult(int.TryParse(value, out var result) ? result : defaultValue);
    }

    public Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var value = _configs.GetValueOrDefault(key);
        return Task.FromResult(bool.TryParse(value, out var result) ? result : defaultValue);
    }

    public Task SetValueAsync(string key, string value, string? updatedBy = null)
    {
        _configs[key] = value;
        return Task.CompletedTask;
    }

    public Task<IList<SiteConfig>> GetAllAsync() =>
        Task.FromResult<IList<SiteConfig>>(_configs.Select(kv => new SiteConfig
        {
            Key = kv.Key,
            Value = kv.Value,
            ValueType = "string"
        }).ToList());

    public void InvalidateCache() { }
}
