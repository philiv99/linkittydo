using System.Text.Json;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Data;

/// <summary>
/// JSON file-based implementation of the GamePhrase repository.
/// Stores phrase data in individual JSON files within a data directory.
/// </summary>
public class JsonGamePhraseRepository : IGamePhraseRepository
{
    private readonly string _dataDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<JsonGamePhraseRepository> _logger;

    public JsonGamePhraseRepository(IConfiguration configuration, ILogger<JsonGamePhraseRepository> logger)
    {
        _logger = logger;
        
        var baseDirectory = configuration["DataDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Data");
        _dataDirectory = Path.Combine(baseDirectory, "Phrases");
        
        // Ensure the data directory exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
            _logger.LogInformation("Created phrases data directory: {Directory}", _dataDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private string GetPhraseFilePath(string uniqueId) => Path.Combine(_dataDirectory, $"{uniqueId}.json");

    public async Task<IEnumerable<GamePhrase>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var phrases = new List<GamePhrase>();
            var files = Directory.GetFiles(_dataDirectory, "*.json");

            foreach (var file in files)
            {
                var phrase = await ReadPhraseFromFileAsync(file);
                if (phrase != null)
                {
                    phrases.Add(phrase);
                }
            }

            return phrases;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<GamePhrase?> GetByIdAsync(string uniqueId)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetPhraseFilePath(uniqueId);
            return await ReadPhraseFromFileAsync(filePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<GamePhrase?> GetByTextAsync(string text)
    {
        var normalizedText = NormalizeText(text);
        var phrases = await GetAllAsync();
        return phrases.FirstOrDefault(p => 
            NormalizeText(p.Text).Equals(normalizedText, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<GamePhrase> CreateAsync(GamePhrase phrase)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetPhraseFilePath(phrase.UniqueId);
            await WritePhraseToFileAsync(filePath, phrase);
            _logger.LogInformation("Created game phrase: {UniqueId} - {Text}", phrase.UniqueId, phrase.Text);
            return phrase;
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
            var filePath = GetPhraseFilePath(uniqueId);
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            _logger.LogInformation("Deleted game phrase: {UniqueId}", uniqueId);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ExistsByTextAsync(string text)
    {
        var phrase = await GetByTextAsync(text);
        return phrase != null;
    }

    public async Task<int> GetCountAsync()
    {
        var phrases = await GetAllAsync();
        return phrases.Count();
    }

    private async Task<GamePhrase?> ReadPhraseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<GamePhrase>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read phrase from file: {FilePath}", filePath);
            return null;
        }
    }

    private async Task WritePhraseToFileAsync(string filePath, GamePhrase phrase)
    {
        var json = JsonSerializer.Serialize(phrase, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Normalizes text for comparison by removing extra whitespace and punctuation variations
    /// </summary>
    private static string NormalizeText(string text)
    {
        return text
            .Trim()
            .ToLowerInvariant()
            .Replace("  ", " ");
    }
}
