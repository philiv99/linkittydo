using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LinkittyDo.Api.Tests;

public class GameServiceTests
{
    private readonly Mock<IGamePhraseService> _phraseServiceMock;
    private readonly Mock<IGameRecordRepository> _gameRecordRepoMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAnalyticsService> _analyticsServiceMock;
    private readonly GameService _service;

    public GameServiceTests()
    {
        _phraseServiceMock = new Mock<IGamePhraseService>();
        _gameRecordRepoMock = new Mock<IGameRecordRepository>();
        _userServiceMock = new Mock<IUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _analyticsServiceMock = new Mock<IAnalyticsService>();
        var loggerMock = new Mock<ILogger<GameService>>();
        var sessionStore = new InMemorySessionStore();
        var dbContextOptions = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: $"GameServiceTests_{Guid.NewGuid()}")
            .Options;
        var dbContext = new LinkittyDoDbContext(dbContextOptions);
        _service = new GameService(
            sessionStore,
            _phraseServiceMock.Object,
            _gameRecordRepoMock.Object,
            _userServiceMock.Object,
            _analyticsServiceMock.Object,
            _unitOfWorkMock.Object,
            dbContext,
            loggerMock.Object);
    }

    private static Phrase CreateTestPhrase()
    {
        return new Phrase
        {
            Id = 1,
            FullText = "the quick brown fox",
            Words = new List<PhraseWord>
            {
                new() { Index = 0, Text = "the", IsHidden = false },
                new() { Index = 1, Text = "quick", IsHidden = true },
                new() { Index = 2, Text = "brown", IsHidden = true },
                new() { Index = 3, Text = "fox", IsHidden = true }
            }
        };
    }

    [Fact]
    public async Task StartNewGameAsync_CreatesGuestSession_WhenNoUserId()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());

        var session = await _service.StartNewGameAsync();

        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.True(session.IsGuestSession);
        Assert.Null(session.GameRecord);
        Assert.Equal(3, session.RevealedWords.Count);
        Assert.All(session.RevealedWords, kv => Assert.False(kv.Value));
    }

    [Fact]
    public async Task StartNewGameAsync_CreatesRegisteredSession_WhenUserIdProvided()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123", 50)).ReturnsAsync(CreateTestPhrase());

        var session = await _service.StartNewGameAsync("USR-123", 50);

        Assert.False(session.IsGuestSession);
        Assert.Equal("USR-123", session.UserId);
        Assert.NotNull(session.GameRecord);
        Assert.StartsWith("GAME-", session.GameRecord!.GameId);
        Assert.Equal(50, session.GameRecord.Difficulty);
        Assert.Equal(GameResult.InProgress, session.GameRecord.Result);
    }

    [Fact]
    public async Task GetGame_ReturnsSession_WhenExists()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = _service.GetGame(session.SessionId);

        Assert.NotNull(result);
        Assert.Equal(session.SessionId, result!.SessionId);
    }

    [Fact]
    public void GetGame_ReturnsNull_WhenNotExists()
    {
        var result = _service.GetGame(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SubmitGuess_CorrectGuess_ReturnsCorrectAndAwardsPoints()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        Assert.True(result.IsCorrect);
        // First guess, no clues, easy difficulty (base=100): 100/1 * 2x bonus = 200
        Assert.Equal(200, result.CurrentScore);
        Assert.Equal("quick", result.RevealedWord);
        Assert.False(result.IsPhraseComplete);
    }

    [Fact]
    public async Task SubmitGuess_CaseInsensitive()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "QUICK" });

        Assert.True(result.IsCorrect);
    }

    [Fact]
    public async Task SubmitGuess_IncorrectGuess_ReturnsFalse()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong" });

        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.CurrentScore);
        Assert.Null(result.RevealedWord);
    }

    [Fact]
    public async Task SubmitGuess_AllWordsRevealed_MarksComplete()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        Assert.True(result.IsPhraseComplete);
        // 3 words × 200 each (first guess, no clues, 2x bonus) = 600
        Assert.Equal(600, result.CurrentScore);
    }

    [Fact]
    public async Task SubmitGuess_RecordsEvents_ForRegisteredUser()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-123");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong" });

        Assert.Equal(2, session.GameRecord!.Events.Count(e => e is GuessEvent));
        var correctGuess = session.GameRecord.Events.OfType<GuessEvent>().First(e => e.IsCorrect);
        // First guess, no clues, easy difficulty: 200
        Assert.Equal(200, correctGuess.PointsAwarded);
    }

    [Fact]
    public async Task SubmitGuess_RecordsSolvedEvent_WhenComplete()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-123");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        Assert.Equal(GameResult.Solved, session.GameRecord!.Result);
        Assert.NotNull(session.GameRecord.CompletedAt);
        var endEvent = session.GameRecord.Events.OfType<GameEndEvent>().Single();
        Assert.Equal("solved", endEvent.Reason);
    }

    [Fact]
    public async Task GetGameState_ReturnsCorrectState()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var state = _service.GetGameState(session.SessionId);

        Assert.Equal(session.SessionId, state.SessionId);
        Assert.Equal(4, state.Words.Count);
        Assert.False(state.IsComplete);
        Assert.Equal(0, state.Score);

        // Non-hidden words show text
        Assert.Equal("the", state.Words[0].DisplayText);
        Assert.False(state.Words[0].IsHidden);

        // Hidden words show null
        Assert.Null(state.Words[1].DisplayText);
        Assert.True(state.Words[1].IsHidden);
    }

    [Fact]
    public async Task GiveUp_RevealsAllWords_AndMarksComplete()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-123");

        var state = await _service.GiveUpAsync(session.SessionId);

        Assert.True(state.IsComplete);
        // All words should now have display text
        Assert.All(state.Words, w => Assert.NotNull(w.DisplayText));
        Assert.Equal(GameResult.GaveUp, session.GameRecord!.Result);
    }

    [Fact]
    public async Task RecordClueEvent_RecordsEvent_ForRegisteredUser()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-123");

        _service.RecordClueEvent(session.SessionId, 1, "fast", "https://example.com");

        var clueEvent = session.GameRecord!.Events.OfType<ClueEvent>().Single();
        Assert.Equal(1, clueEvent.WordIndex);
        Assert.Equal("fast", clueEvent.SearchTerm);
        Assert.Equal("https://example.com", clueEvent.Url);
    }

    [Fact]
    public async Task SubmitGuess_InvalidWordIndex_ReturnsFalse()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 99, Guess = "test" });

        Assert.False(result.IsCorrect);
    }

    [Fact]
    public async Task SubmitGuess_NonHiddenWord_ReturnsFalse()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 0, Guess = "the" });

        Assert.False(result.IsCorrect);
    }
}
