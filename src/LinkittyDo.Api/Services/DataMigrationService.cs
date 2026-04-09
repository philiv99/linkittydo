using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Migrates data from JSON file storage to MySQL database.
/// Reads all User, GamePhrase, and GameRecord JSON files and inserts into normalized tables.
/// Idempotent: skips existing records by primary key.
/// </summary>
public interface IDataMigrationService
{
    Task<DataMigrationResult> MigrateAsync(CancellationToken cancellationToken = default);
}

public class DataMigrationResult
{
    public int UsersImported { get; set; }
    public int UsersSkipped { get; set; }
    public int PhrasesImported { get; set; }
    public int PhrasesSkipped { get; set; }
    public int GameRecordsImported { get; set; }
    public int GameRecordsSkipped { get; set; }
    public int GameEventsImported { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0;
}

public class JsonToMySqlMigrationService : IDataMigrationService
{
    private readonly LinkittyDoDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JsonToMySqlMigrationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonToMySqlMigrationService(
        LinkittyDoDbContext dbContext,
        IConfiguration configuration,
        ILogger<JsonToMySqlMigrationService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DataMigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
    {
        var result = new DataMigrationResult();
        var dataDir = _configuration.GetValue<string>("DataDirectory") ?? "Data";

        _logger.LogInformation("Starting JSON → MySQL data migration from {DataDir}", dataDir);

        await MigrateUsersAsync(dataDir, result, cancellationToken);
        await MigratePhrasesAsync(dataDir, result, cancellationToken);
        await MigrateGameRecordsAsync(dataDir, result, cancellationToken);

        _logger.LogInformation(
            "Migration complete: {Users} users, {Phrases} phrases, {Records} game records, {Events} events imported. {Errors} errors.",
            result.UsersImported, result.PhrasesImported, result.GameRecordsImported,
            result.GameEventsImported, result.Errors.Count);

        return result;
    }

    private async Task MigrateUsersAsync(string dataDir, DataMigrationResult result, CancellationToken ct)
    {
        var usersDir = Path.Combine(dataDir, "Users");
        if (!Directory.Exists(usersDir))
        {
            _logger.LogInformation("No Users directory found at {Path}, skipping user migration", usersDir);
            return;
        }

        var files = Directory.GetFiles(usersDir, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var user = JsonSerializer.Deserialize<User>(json, JsonOptions);
                if (user == null) continue;

                var exists = await _dbContext.Users.AnyAsync(u => u.UniqueId == user.UniqueId, ct);
                if (exists)
                {
                    result.UsersSkipped++;
                    continue;
                }

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(ct);
                result.UsersImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"User file {Path.GetFileName(file)}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to import user from {File}", file);
            }
        }
    }

    private async Task MigratePhrasesAsync(string dataDir, DataMigrationResult result, CancellationToken ct)
    {
        var phrasesDir = Path.Combine(dataDir, "Phrases");
        if (!Directory.Exists(phrasesDir))
        {
            _logger.LogInformation("No Phrases directory found at {Path}, skipping phrase migration", phrasesDir);
            return;
        }

        var files = Directory.GetFiles(phrasesDir, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var phrase = JsonSerializer.Deserialize<GamePhrase>(json, JsonOptions);
                if (phrase == null) continue;

                var exists = await _dbContext.GamePhrases.AnyAsync(p => p.UniqueId == phrase.UniqueId, ct);
                if (exists)
                {
                    result.PhrasesSkipped++;
                    continue;
                }

                _dbContext.GamePhrases.Add(phrase);
                await _dbContext.SaveChangesAsync(ct);
                result.PhrasesImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Phrase file {Path.GetFileName(file)}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to import phrase from {File}", file);
            }
        }
    }

    private async Task MigrateGameRecordsAsync(string dataDir, DataMigrationResult result, CancellationToken ct)
    {
        var recordsDir = Path.Combine(dataDir, "GameRecords");
        if (!Directory.Exists(recordsDir))
        {
            _logger.LogInformation("No GameRecords directory found at {Path}, skipping game record migration", recordsDir);
            return;
        }

        var files = Directory.GetFiles(recordsDir, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var record = JsonSerializer.Deserialize<GameRecord>(json, JsonOptions);
                if (record == null) continue;

                var exists = await _dbContext.GameRecords.AnyAsync(r => r.GameId == record.GameId, ct);
                if (exists)
                {
                    result.GameRecordsSkipped++;
                    continue;
                }

                // Save game record without events first (events are in separate table)
                var events = record.Events.ToList();
                record.Events = new List<GameEvent>();
                _dbContext.GameRecords.Add(record);
                await _dbContext.SaveChangesAsync(ct);
                result.GameRecordsImported++;

                // Save events
                foreach (var evt in events)
                {
                    evt.GameId = record.GameId;
                    _dbContext.GameEvents.Add(evt);
                }
                await _dbContext.SaveChangesAsync(ct);
                result.GameEventsImported += events.Count;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"GameRecord file {Path.GetFileName(file)}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to import game record from {File}", file);
            }
        }
    }
}
