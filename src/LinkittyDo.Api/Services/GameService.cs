using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IGameService
{
    Task<GameSession> StartNewGameAsync(string? userId = null, int difficulty = 10);
    GameSession? GetGame(Guid sessionId);
    Task<GuessResponse> SubmitGuessAsync(Guid sessionId, GuessRequest request);
    GameState GetGameState(Guid sessionId);
    Task<GameState> GiveUpAsync(Guid sessionId);
    Task RecordClueEventAsync(Guid sessionId, int wordIndex, string searchTerm, string url);
    Task<int> RemoveExpiredSessionsAsync(TimeSpan maxAge);
    int ActiveSessionCount { get; }
    Task<GameRecord?> GetGameRecordAsync(Guid sessionId);
}

public class GameService : IGameService
{
    private readonly ISessionStore _sessionStore;
    private readonly IGamePhraseService _phraseService;
    private readonly IGameRecordRepository _gameRecordRepository;
    private readonly IUserService _userService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LinkittyDo.Api.Data.LinkittyDoDbContext _dbContext;
    private readonly ILogger<GameService> _logger;

    public GameService(
        ISessionStore sessionStore,
        IGamePhraseService phraseService,
        IGameRecordRepository gameRecordRepository,
        IUserService userService,
        IAnalyticsService analyticsService,
        IUnitOfWork unitOfWork,
        LinkittyDo.Api.Data.LinkittyDoDbContext dbContext,
        ILogger<GameService> logger)
    {
        _sessionStore = sessionStore;
        _phraseService = phraseService;
        _gameRecordRepository = gameRecordRepository;
        _userService = userService;
        _analyticsService = analyticsService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Generates a unique game ID following the format: GAME-{timestamp}-{random}
    /// </summary>
    private static string GenerateGameId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        return $"GAME-{timestamp}-{random}";
    }

    public async Task<GameSession> StartNewGameAsync(string? userId = null, int difficulty = 10)
    {
        _logger.LogInformation("Starting new game for user: {UserId}, difficulty: {Difficulty}", 
            userId ?? "guest", difficulty);
        
        var phrase = await _phraseService.GetPhraseForUserAsync(userId, difficulty);
        var now = DateTime.UtcNow;
        
        var session = new GameSession
        {
            SessionId = Guid.NewGuid(),
            PhraseId = phrase.Id,
            Phrase = phrase,
            RevealedWords = new Dictionary<int, bool>(),
            Score = 0,
            Difficulty = difficulty,
            StartedAt = now,
            LastActivityAt = now,
            UserId = userId
        };

        // Initialize all hidden words as not revealed
        foreach (var word in phrase.Words.Where(w => w.IsHidden))
        {
            session.RevealedWords[word.Index] = false;
        }

        // Create game record for non-guest users and persist to DB immediately
        if (!session.IsGuestSession)
        {
            session.GameRecord = new GameRecord
            {
                GameId = GenerateGameId(),
                UserId = userId!,
                PlayedAt = now,
                PhraseId = phrase.Id,
                PhraseUniqueId = phrase.UniqueId,
                PhraseText = phrase.FullText,
                Difficulty = difficulty,
                Score = 0,
                Result = GameResult.InProgress,
                Events = new List<GameEvent>()
            };

            try
            {
                await _gameRecordRepository.CreateAsync(session.GameRecord);
                _logger.LogInformation("Persisted InProgress GameRecord {GameId} for user {UserId}",
                    session.GameRecord.GameId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist initial GameRecord for user {UserId}", userId);
                throw;
            }
        }

        _sessionStore.Set(session.SessionId, session);
        return session;
    }

    public GameSession? GetGame(Guid sessionId)
    {
        return _sessionStore.Get(sessionId);
    }

    public async Task<GuessResponse> SubmitGuessAsync(Guid sessionId, GuessRequest request)
    {
        var session = GetGame(sessionId);
        if (session == null)
        {
            return new GuessResponse { IsCorrect = false };
        }

        var word = session.Phrase.Words.FirstOrDefault(w => w.Index == request.WordIndex);
        if (word == null || !word.IsHidden)
        {
            return new GuessResponse { IsCorrect = false, CurrentScore = session.Score };
        }

        session.LastActivityAt = DateTime.UtcNow;

        // Case-insensitive comparison
        var isCorrect = string.Equals(word.Text, request.Guess, StringComparison.OrdinalIgnoreCase);
        
        // Track guess count for this word
        if (!session.GuessCountPerWord.ContainsKey(request.WordIndex))
            session.GuessCountPerWord[request.WordIndex] = 0;
        session.GuessCountPerWord[request.WordIndex]++;
        
        // Calculate points using enhanced scoring formula
        var pointsAwarded = isCorrect ? CalculateWordScore(session, request.WordIndex) : 0;

        if (isCorrect)
        {
            session.RevealedWords[request.WordIndex] = true;
            session.Score += pointsAwarded;
        }

        // Record guess event for non-guest sessions
        if (!session.IsGuestSession && session.GameRecord != null)
        {
            var guessEvent = new GuessEvent
            {
                GameId = session.GameRecord.GameId,
                SequenceNumber = session.GameRecord.Events.Count,
                WordIndex = request.WordIndex,
                GuessText = request.Guess,
                IsCorrect = isCorrect,
                PointsAwarded = pointsAwarded,
                Timestamp = DateTime.UtcNow
            };
            session.GameRecord.Events.Add(guessEvent);
            session.GameRecord.Score = session.Score;

            // Persist event incrementally
            try
            {
                _dbContext.GameEvents.Add(guessEvent);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist guess event for game {GameId}", session.GameRecord.GameId);
            }
        }

        var isComplete = session.RevealedWords.All(kv => kv.Value);
        
        // If phrase is complete, mark game as solved and persist final state
        if (isComplete && !session.IsGuestSession && session.GameRecord != null)
        {
            var endEvent = new GameEndEvent
            {
                GameId = session.GameRecord.GameId,
                SequenceNumber = session.GameRecord.Events.Count,
                Reason = "solved",
                Timestamp = DateTime.UtcNow
            };
            session.GameRecord.Events.Add(endEvent);
            session.GameRecord.Result = GameResult.Solved;
            session.GameRecord.CompletedAt = DateTime.UtcNow;

            await PersistGameCompletionAsync(session, endEvent);
        }

        return new GuessResponse
        {
            IsCorrect = isCorrect,
            IsPhraseComplete = isComplete,
            CurrentScore = session.Score,
            RevealedWord = isCorrect ? word.Text : null
        };
    }

    public GameState GetGameState(Guid sessionId)
    {
        var session = GetGame(sessionId);
        if (session == null)
        {
            return new GameState();
        }

        var words = session.Phrase.Words.Select(w => new WordState
        {
            Index = w.Index,
            IsHidden = w.IsHidden,
            IsRevealed = !w.IsHidden || session.RevealedWords.GetValueOrDefault(w.Index, false),
            DisplayText = !w.IsHidden || session.RevealedWords.GetValueOrDefault(w.Index, false) 
                ? w.Text 
                : null
        }).ToList();

        return new GameState
        {
            SessionId = session.SessionId,
            Words = words,
            Score = session.Score,
            IsComplete = session.RevealedWords.All(kv => kv.Value)
        };
    }

    public async Task<GameState> GiveUpAsync(Guid sessionId)
    {
        var session = GetGame(sessionId);
        if (session == null)
        {
            return new GameState();
        }

        // Reveal all hidden words
        foreach (var word in session.Phrase.Words.Where(w => w.IsHidden))
        {
            session.RevealedWords[word.Index] = true;
        }

        // Set score to 0 for giving up
        session.Score = 0;
        
        // Record game end event for non-guest sessions
        if (!session.IsGuestSession && session.GameRecord != null)
        {
            var endEvent = new GameEndEvent
            {
                GameId = session.GameRecord.GameId,
                SequenceNumber = session.GameRecord.Events.Count,
                Reason = "gaveup",
                Timestamp = DateTime.UtcNow
            };
            session.GameRecord.Events.Add(endEvent);
            session.GameRecord.Result = GameResult.GaveUp;
            session.GameRecord.Score = 0;
            session.GameRecord.CompletedAt = DateTime.UtcNow;

            await PersistGameCompletionAsync(session, endEvent);
        }

        return GetGameState(sessionId);
    }

    public async Task RecordClueEventAsync(Guid sessionId, int wordIndex, string searchTerm, string url)
    {
        var session = GetGame(sessionId);
        if (session == null)
        {
            return;
        }
        
        session.LastActivityAt = DateTime.UtcNow;

        // Track clue count per word for scoring
        if (!session.ClueCountPerWord.ContainsKey(wordIndex))
            session.ClueCountPerWord[wordIndex] = 0;
        session.ClueCountPerWord[wordIndex]++;

        if (session.IsGuestSession || session.GameRecord == null)
        {
            return;
        }

        var clueEvent = new ClueEvent
        {
            GameId = session.GameRecord.GameId,
            SequenceNumber = session.GameRecord.Events.Count,
            WordIndex = wordIndex,
            SearchTerm = searchTerm,
            Url = url,
            Timestamp = DateTime.UtcNow
        };
        session.GameRecord.Events.Add(clueEvent);

        // Persist event incrementally
        try
        {
            _dbContext.GameEvents.Add(clueEvent);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist clue event for game {GameId}", session.GameRecord.GameId);
        }
    }

    /// <summary>
    /// Calculates the score for correctly guessing a word.
    /// Formula: BasePoints / (clueCount * guessCount), with first-guess-no-clue bonus.
    /// </summary>
    internal static int CalculateWordScore(GameSession session, int wordIndex)
    {
        var difficulty = session.Difficulty;
        var basePoints = GetBasePoints(difficulty);
        
        var clueCount = session.ClueCountPerWord.GetValueOrDefault(wordIndex, 0);
        var guessCount = session.GuessCountPerWord.GetValueOrDefault(wordIndex, 1);
        
        // Minimum 1 for divisor components
        var effectiveClueCount = Math.Max(1, clueCount);
        var effectiveGuessCount = Math.Max(1, guessCount);
        
        var score = (double)basePoints / (effectiveClueCount * effectiveGuessCount);
        
        // First-guess bonus: 2x if correct on first guess with no clues
        if (guessCount == 1 && clueCount == 0)
        {
            score *= 2;
        }
        
        return (int)Math.Round(score);
    }

    /// <summary>
    /// Returns base points scaled by difficulty tier.
    /// </summary>
    internal static int GetBasePoints(int difficulty)
    {
        return difficulty switch
        {
            <= 20 => 100,
            <= 50 => 150,
            <= 80 => 200,
            _ => 300
        };
    }

    public int ActiveSessionCount => _sessionStore.Count;

    public async Task<int> RemoveExpiredSessionsAsync(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var expired = _sessionStore.GetAll()
            .Where(kv => kv.Value.LastActivityAt < cutoff)
            .Select(kv => kv.Value)
            .ToList();

        var abandonedCount = 0;
        foreach (var session in expired)
        {
            // Mark registered user games as abandoned in DB
            if (!session.IsGuestSession && session.GameRecord != null)
            {
                try
                {
                    session.GameRecord.Result = GameResult.Abandoned;
                    session.GameRecord.CompletedAt = DateTime.UtcNow;
                    await _gameRecordRepository.UpdateAsync(session.GameRecord);
                    abandonedCount++;
                    _logger.LogInformation("Marked game {GameId} as Abandoned for user {UserId}",
                        session.GameRecord.GameId, session.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to mark game {GameId} as Abandoned",
                        session.GameRecord.GameId);
                }
            }
            _sessionStore.Remove(session.SessionId);
        }

        if (expired.Count > 0)
        {
            _logger.LogInformation("Removed {Count} expired sessions (older than {MaxAge}), {Abandoned} marked as abandoned",
                expired.Count, maxAge, abandonedCount);
        }

        return expired.Count;
    }

    public async Task<GameRecord?> GetGameRecordAsync(Guid sessionId)
    {
        var session = _sessionStore.Get(sessionId);
        if (session == null || session.IsGuestSession || session.GameRecord == null)
            return null;

        return session.GameRecord;
    }

    private async Task PersistGameCompletionAsync(GameSession session, GameEndEvent endEvent)
    {
        if (session.GameRecord == null || session.IsGuestSession) return;

        try
        {
            // Wrap GameRecord update + GameEndEvent in a transaction for atomicity
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Update the existing GameRecord (created at game start)
                await _gameRecordRepository.UpdateAsync(session.GameRecord);

                // Persist the GameEndEvent
                _dbContext.GameEvents.Add(endEvent);
                await _dbContext.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Analytics recompute outside transaction — failure should not roll back game data
            // Update user's lifetime points
            if (session.UserId != null && session.GameRecord.Score > 0)
            {
                await _userService.AddPointsAsync(session.UserId, session.GameRecord.Score);
            }

            try
            {
                if (session.UserId != null)
                {
                    await _analyticsService.RecomputePlayerStatsAsync(session.UserId);
                }
                if (!string.IsNullOrEmpty(session.GameRecord.PhraseUniqueId))
                {
                    await _analyticsService.RecomputePhrasePlayStatsAsync(session.GameRecord.PhraseUniqueId);
                }
                if (session.GameRecord.Events.Count > 0)
                {
                    await _analyticsService.RecomputeClueEffectivenessForGameAsync(
                        session.GameRecord.GameId, session.GameRecord.Events);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to recompute analytics for game {GameId}, data will be stale until next recompute",
                    session.GameRecord.GameId);
            }

            _logger.LogInformation("Persisted game record {GameId} for user {UserId}",
                session.GameRecord.GameId, session.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist game record {GameId}", session.GameRecord.GameId);
            throw;
        }
    }
}
