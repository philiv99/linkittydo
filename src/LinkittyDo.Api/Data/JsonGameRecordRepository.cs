using System.Text.Json;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Data;

/// <summary>
/// JSON file-based implementation of IGameRecordRepository.
/// Stores game records as individual JSON files in Data/GameRecords/.
/// </summary>
public class JsonGameRecordRepository : IGameRecordRepository
{
    private readonly string _dataDirectory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonGameRecordRepository(IConfiguration configuration)
    {
        var baseDir = configuration.GetValue<string>("DataDirectory") ?? "Data";
        _dataDirectory = Path.Combine(baseDir, "GameRecords");
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<GameRecord?> GetByGameIdAsync(string gameId)
    {
        var filePath = GetFilePath(gameId);
        if (!File.Exists(filePath)) return null;

        await _semaphore.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<GameRecord>(json, JsonOptions);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<GameRecord>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        await _semaphore.WaitAsync();
        try
        {
            var records = new List<GameRecord>();
            if (!Directory.Exists(_dataDirectory)) return records;

            foreach (var file in Directory.GetFiles(_dataDirectory, "*.json"))
            {
                var json = await File.ReadAllTextAsync(file);
                var record = JsonSerializer.Deserialize<GameRecord>(json, JsonOptions);
                if (record != null && record.UserId == userId)
                    records.Add(record);
            }

            return records
                .OrderByDescending(r => r.PlayedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<GameRecord> CreateAsync(GameRecord record)
    {
        await _semaphore.WaitAsync();
        try
        {
            var filePath = GetFilePath(record.GameId);
            var json = JsonSerializer.Serialize(record, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            return record;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<GameRecord?> UpdateAsync(GameRecord record)
    {
        var filePath = GetFilePath(record.GameId);
        if (!File.Exists(filePath)) return null;

        await _semaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(record, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            return record;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetCountByUserIdAsync(string userId)
    {
        var records = await GetByUserIdAsync(userId, 1, int.MaxValue);
        return records.Count();
    }

    private string GetFilePath(string gameId)
    {
        return Path.Combine(_dataDirectory, $"{gameId}.json");
    }
}
