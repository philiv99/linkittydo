using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IGameService
{
    Task<GameSession> StartNewGameAsync(string? userId = null, int difficulty = 10);
    GameSession? GetGame(Guid sessionId);
    GuessResponse SubmitGuess(Guid sessionId, GuessRequest request);
    GameState GetGameState(Guid sessionId);
    GameState GiveUp(Guid sessionId);
    void RecordClueEvent(Guid sessionId, int wordIndex, string searchTerm, string url);
    int RemoveExpiredSessions(TimeSpan maxAge);
    int ActiveSessionCount { get; }
    Task<GameRecord?> GetGameRecordAsync(Guid sessionId);
}

public class GameService : IGameService
{
    private readonly ISessionStore _sessionStore;
    private readonly IGamePhraseService _phraseService;
    private readonly IGameRecordRepository _gameRecordRepository;
    private readonly IUserService _userService;
    private readonly ILogger<GameService> _logger;

    public GameService(
        ISessionStore sessionStore,
        IGamePhraseService phraseService,
        IGameRecordRepository gameRecordRepository,
        IUserService userService,
        ILogger<GameService> logger)
    {
        _sessionStore = sessionStore;
        _phraseService = phraseService;
        _gameRecordRepository = gameRecordRepository;
        _userService = userService;
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

        // Create game record for non-guest users
        if (!session.IsGuestSession)
        {
            session.GameRecord = new GameRecord
            {
                GameId = GenerateGameId(),
                UserId = userId!,
                PlayedAt = now,
                PhraseId = phrase.Id,
                PhraseText = phrase.FullText,
                Difficulty = difficulty,
                Score = 0,
                Result = GameResult.InProgress,
                Events = new List<GameEvent>()
            };
        }

        _sessionStore.Set(session.SessionId, session);
        return session;
    }

    public GameSession? GetGame(Guid sessionId)
    {
        return _sessionStore.Get(sessionId);
    }

    public GuessResponse SubmitGuess(Guid sessionId, GuessRequest request)
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
            session.GameRecord.Events.Add(new GuessEvent
            {
                GameId = session.GameRecord.GameId,
                SequenceNumber = session.GameRecord.Events.Count,
                WordIndex = request.WordIndex,
                GuessText = request.Guess,
                IsCorrect = isCorrect,
                PointsAwarded = pointsAwarded,
                Timestamp = DateTime.UtcNow
            });
            session.GameRecord.Score = session.Score;
        }

        var isComplete = session.RevealedWords.All(kv => kv.Value);
        
        // If phrase is complete, mark game as solved and persist
        if (isComplete && !session.IsGuestSession && session.GameRecord != null)
        {
            session.GameRecord.Events.Add(new GameEndEvent
            {
                GameId = session.GameRecord.GameId,
                SequenceNumber = session.GameRecord.Events.Count,
                Reason = "solved",
                Timestamp = DateTime.UtcNow
            });
            session.GameRecord.Result = GameResult.Solved;
            session.GameRecord.CompletedAt = DateTime.UtcNow;

            _ = PersistGameRecordAsync(session);
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

    public GameState GiveUp(Guid sessionId)
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
            session.GameRecord.Events.Add(new GameEndEvent
            {
                GameId = session.GameRecord.GameId,
                SequenceNumber = session.GameRecord.Events.Count,
                Reason = "gaveup",
                Timestamp = DateTime.UtcNow
            });
            session.GameRecord.Result = GameResult.GaveUp;
            session.GameRecord.Score = 0;
            session.GameRecord.CompletedAt = DateTime.UtcNow;

            _ = PersistGameRecordAsync(session);
        }

        return GetGameState(sessionId);
    }

    public void RecordClueEvent(Guid sessionId, int wordIndex, string searchTerm, string url)
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

        session.GameRecord.Events.Add(new ClueEvent
        {
            GameId = session.GameRecord.GameId,
            SequenceNumber = session.GameRecord.Events.Count,
            WordIndex = wordIndex,
            SearchTerm = searchTerm,
            Url = url,
            Timestamp = DateTime.UtcNow
        });
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

    public int RemoveExpiredSessions(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var expired = _sessionStore.GetAll()
            .Where(kv => kv.Value.LastActivityAt < cutoff)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expired)
        {
            _sessionStore.Remove(key);
        }

        if (expired.Count > 0)
        {
            _logger.LogInformation("Removed {Count} expired sessions (older than {MaxAge})", expired.Count, maxAge);
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

    private async Task PersistGameRecordAsync(GameSession session)
    {
        if (session.GameRecord == null || session.IsGuestSession) return;

        try
        {
            await _gameRecordRepository.CreateAsync(session.GameRecord);
            
            // Also update user's lifetime points
            if (session.UserId != null && session.GameRecord.Score > 0)
            {
                await _userService.AddPointsAsync(session.UserId, session.GameRecord.Score);
            }

            _logger.LogInformation("Persisted game record {GameId} for user {UserId}",
                session.GameRecord.GameId, session.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist game record {GameId}", session.GameRecord.GameId);
        }
    }
}
