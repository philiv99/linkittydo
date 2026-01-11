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
}

public class GameService : IGameService
{
    private readonly Dictionary<Guid, GameSession> _sessions = new();
    private readonly IGamePhraseService _phraseService;
    private readonly ILogger<GameService> _logger;

    public GameService(IGamePhraseService phraseService, ILogger<GameService> logger)
    {
        _phraseService = phraseService;
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
        
        var phrase = await _phraseService.GetPhraseForUserAsync(userId);
        var now = DateTime.UtcNow;
        
        var session = new GameSession
        {
            SessionId = Guid.NewGuid(),
            PhraseId = phrase.Id,
            Phrase = phrase,
            RevealedWords = new Dictionary<int, bool>(),
            Score = 0,
            StartedAt = now,
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
                PlayedAt = now,
                PhraseId = phrase.Id,
                PhraseText = phrase.FullText,
                Difficulty = difficulty,
                Score = 0,
                Result = GameResult.InProgress,
                Events = new List<GameEvent>()
            };
        }

        _sessions[session.SessionId] = session;
        return session;
    }

    public GameSession? GetGame(Guid sessionId)
    {
        return _sessions.GetValueOrDefault(sessionId);
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

        // Case-insensitive comparison
        var isCorrect = string.Equals(word.Text, request.Guess, StringComparison.OrdinalIgnoreCase);
        var pointsAwarded = isCorrect ? 100 : 0;

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
                WordIndex = request.WordIndex,
                GuessText = request.Guess,
                IsCorrect = isCorrect,
                PointsAwarded = pointsAwarded,
                Timestamp = DateTime.UtcNow
            });
            session.GameRecord.Score = session.Score;
        }

        var isComplete = session.RevealedWords.All(kv => kv.Value);
        
        // If phrase is complete, mark game as solved
        if (isComplete && !session.IsGuestSession && session.GameRecord != null)
        {
            session.GameRecord.Events.Add(new GameEndEvent
            {
                Reason = "solved",
                Timestamp = DateTime.UtcNow
            });
            session.GameRecord.Result = GameResult.Solved;
            session.GameRecord.CompletedAt = DateTime.UtcNow;
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
                Reason = "gaveup",
                Timestamp = DateTime.UtcNow
            });
            session.GameRecord.Result = GameResult.GaveUp;
            session.GameRecord.Score = 0;
            session.GameRecord.CompletedAt = DateTime.UtcNow;
        }

        return GetGameState(sessionId);
    }

    public void RecordClueEvent(Guid sessionId, int wordIndex, string searchTerm, string url)
    {
        var session = GetGame(sessionId);
        if (session == null || session.IsGuestSession || session.GameRecord == null)
        {
            return;
        }

        session.GameRecord.Events.Add(new ClueEvent
        {
            WordIndex = wordIndex,
            SearchTerm = searchTerm,
            Url = url,
            Timestamp = DateTime.UtcNow
        });
    }
}
