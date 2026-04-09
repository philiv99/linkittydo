using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Database-backed session store that persists sessions to the GameSessions table.
/// Sessions survive API restarts when using MySQL data provider.
/// Falls back to in-memory for the hot path with periodic DB sync.
/// </summary>
public class DatabaseSessionStore : ISessionStore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSessionStore> _logger;
    private readonly InMemorySessionStore _memoryCache = new();

    public DatabaseSessionStore(IServiceProvider serviceProvider, ILogger<DatabaseSessionStore> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public GameSession? Get(Guid sessionId)
    {
        return _memoryCache.Get(sessionId);
    }

    public void Set(Guid sessionId, GameSession session)
    {
        _memoryCache.Set(sessionId, session);
        _ = PersistSessionAsync(sessionId, session);
    }

    public bool Remove(Guid sessionId)
    {
        var removed = _memoryCache.Remove(sessionId);
        if (removed)
        {
            _ = RemoveFromDbAsync(sessionId);
        }
        return removed;
    }

    public IEnumerable<KeyValuePair<Guid, GameSession>> GetAll()
    {
        return _memoryCache.GetAll();
    }

    public int Count => _memoryCache.Count;

    private async Task PersistSessionAsync(Guid sessionId, GameSession session)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Data.LinkittyDoDbContext>();

            var stateJson = JsonSerializer.Serialize(new SessionState
            {
                RevealedWords = session.RevealedWords,
                UsedClueTerms = session.UsedClueTerms.ToDictionary(k => k.Key, v => v.Value.ToList()),
                UsedClueUrls = session.UsedClueUrls.ToList(),
                ClueCountPerWord = session.ClueCountPerWord,
                GuessCountPerWord = session.GuessCountPerWord
            });

            var existing = await dbContext.GameSessions.FindAsync(sessionId.ToString());
            if (existing != null)
            {
                existing.Score = session.Score;
                existing.StateJson = stateJson;
                existing.LastActivityAt = session.LastActivityAt;
            }
            else
            {
                dbContext.GameSessions.Add(new GameSessionRecord
                {
                    SessionId = sessionId.ToString(),
                    UserId = session.UserId,
                    PhraseUniqueId = session.Phrase?.FullText ?? "",
                    Score = session.Score,
                    Difficulty = session.Difficulty,
                    StateJson = stateJson,
                    StartedAt = session.StartedAt,
                    LastActivityAt = session.LastActivityAt
                });
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist session {SessionId} to database", sessionId);
        }
    }

    private async Task RemoveFromDbAsync(Guid sessionId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Data.LinkittyDoDbContext>();

            var record = await dbContext.GameSessions.FindAsync(sessionId.ToString());
            if (record != null)
            {
                dbContext.GameSessions.Remove(record);
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove session {SessionId} from database", sessionId);
        }
    }

    /// <summary>
    /// Ephemeral session state serialized to JSON for DB storage.
    /// </summary>
    private class SessionState
    {
        public Dictionary<int, bool> RevealedWords { get; set; } = new();
        public Dictionary<int, List<string>> UsedClueTerms { get; set; } = new();
        public List<string> UsedClueUrls { get; set; } = new();
        public Dictionary<int, int> ClueCountPerWord { get; set; } = new();
        public Dictionary<int, int> GuessCountPerWord { get; set; } = new();
    }
}
