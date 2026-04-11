using LinkittyDo.Api.Data;
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

    /// <summary>
    /// Load active sessions from the database into the memory cache.
    /// Called on startup to recover sessions that survived a server restart.
    /// </summary>
    public async Task LoadSessionsAsync(TimeSpan maxAge)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LinkittyDoDbContext>();

            var cutoff = DateTime.UtcNow - maxAge;

            var recovered = 0;
            var staleRemoved = 0;

            // Remove stale sessions (older than maxAge) first
            var staleRecords = await dbContext.GameSessions
                .Where(s => s.LastActivityAt < cutoff)
                .ToListAsync();
            if (staleRecords.Count > 0)
            {
                dbContext.GameSessions.RemoveRange(staleRecords);
                await dbContext.SaveChangesAsync();
                staleRemoved = staleRecords.Count;
            }

            var records = await dbContext.GameSessions
                .Where(s => s.LastActivityAt >= cutoff)
                .ToListAsync();

            if (records.Count == 0)
            {
                _logger.LogInformation("No active sessions to recover from database (removed {StaleCount} stale)", staleRemoved);
                return;
            }

            var gameRecordRepo = scope.ServiceProvider.GetRequiredService<IGameRecordRepository>();

            foreach (var record in records)
            {
                try
                {
                    var sessionId = Guid.Parse(record.SessionId);

                    // Deserialize session state
                    var state = JsonSerializer.Deserialize<SessionState>(record.StateJson) ?? new SessionState();

                    // Reconstruct Phrase from stored word data
                    Phrase phrase;
                    if (state.PhraseWords.Count > 0)
                    {
                        // Reconstruct from stored word data (preferred — self-contained)
                        phrase = new Phrase
                        {
                            Id = state.PhraseId,
                            UniqueId = record.PhraseUniqueId,
                            FullText = state.PhraseFullText,
                            Difficulty = record.Difficulty,
                            Words = state.PhraseWords.Select(w => new PhraseWord
                            {
                                Index = w.Index,
                                Text = w.Text,
                                IsHidden = w.IsHidden,
                                ClueSearchTerm = w.IsHidden ? w.Text : null
                            }).ToList()
                        };
                    }
                    else
                    {
                        // Fallback: discard session if phrase words not stored
                        _logger.LogWarning("Cannot recover session {SessionId}: no phrase word data in state",
                            record.SessionId);
                        dbContext.GameSessions.Remove(record);
                        continue;
                    }

                    var session = new GameSession
                    {
                        SessionId = sessionId,
                        PhraseId = phrase.Id,
                        Phrase = phrase,
                        RevealedWords = state.RevealedWords,
                        Score = record.Score,
                        Difficulty = record.Difficulty,
                        StartedAt = record.StartedAt,
                        LastActivityAt = record.LastActivityAt,
                        UserId = record.UserId,
                        UsedClueTerms = state.UsedClueTerms.ToDictionary(
                            k => k.Key,
                            v => new HashSet<string>(v.Value)),
                        UsedClueUrls = new HashSet<string>(state.UsedClueUrls, StringComparer.OrdinalIgnoreCase),
                        ClueCountPerWord = state.ClueCountPerWord,
                        GuessCountPerWord = state.GuessCountPerWord
                    };

                    // Reload the GameRecord if it exists
                    if (!string.IsNullOrEmpty(record.GameRecordId))
                    {
                        var gameRecord = await gameRecordRepo.GetByGameIdWithEventsAsync(record.GameRecordId);
                        if (gameRecord != null)
                        {
                            session.GameRecord = gameRecord;
                        }
                    }

                    _memoryCache.Set(sessionId, session);
                    recovered++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to recover session {SessionId}, discarding", record.SessionId);
                    dbContext.GameSessions.Remove(record);
                }
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Session recovery complete: {Recovered} recovered, {Stale} stale removed",
                recovered, staleRemoved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions from database");
        }
    }

    private async Task PersistSessionAsync(Guid sessionId, GameSession session)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LinkittyDoDbContext>();

            var stateJson = JsonSerializer.Serialize(new SessionState
            {
                RevealedWords = session.RevealedWords,
                UsedClueTerms = session.UsedClueTerms.ToDictionary(k => k.Key, v => v.Value.ToList()),
                UsedClueUrls = session.UsedClueUrls.ToList(),
                ClueCountPerWord = session.ClueCountPerWord,
                GuessCountPerWord = session.GuessCountPerWord,
                PhraseWords = session.Phrase?.Words.Select(w => new SessionPhraseWord
                {
                    Index = w.Index,
                    Text = w.Text,
                    IsHidden = w.IsHidden
                }).ToList() ?? new(),
                PhraseFullText = session.Phrase?.FullText ?? string.Empty,
                PhraseId = session.PhraseId
            });

            var existing = await dbContext.GameSessions.FindAsync(sessionId.ToString());
            if (existing != null)
            {
                existing.Score = session.Score;
                existing.StateJson = stateJson;
                existing.LastActivityAt = session.LastActivityAt;
                existing.GameRecordId = session.GameRecord?.GameId;
            }
            else
            {
                dbContext.GameSessions.Add(new GameSessionRecord
                {
                    SessionId = sessionId.ToString(),
                    UserId = session.UserId,
                    PhraseUniqueId = session.Phrase?.UniqueId ?? "",
                    GameRecordId = session.GameRecord?.GameId,
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
            var dbContext = scope.ServiceProvider.GetRequiredService<LinkittyDoDbContext>();

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
    internal class SessionState
    {
        public Dictionary<int, bool> RevealedWords { get; set; } = new();
        public Dictionary<int, List<string>> UsedClueTerms { get; set; } = new();
        public List<string> UsedClueUrls { get; set; } = new();
        public Dictionary<int, int> ClueCountPerWord { get; set; } = new();
        public Dictionary<int, int> GuessCountPerWord { get; set; } = new();
        public List<SessionPhraseWord> PhraseWords { get; set; } = new();
        public string PhraseFullText { get; set; } = string.Empty;
        public int PhraseId { get; set; }
    }

    internal class SessionPhraseWord
    {
        public int Index { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
    }
}
