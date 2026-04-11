using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LinkittyDo.Api.Tests;

public class GamePersistenceTests
{
    private readonly Mock<IGamePhraseService> _phraseServiceMock;
    private readonly Mock<IGameRecordRepository> _gameRecordRepoMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAnalyticsService> _analyticsServiceMock;
    private readonly LinkittyDoDbContext _dbContext;
    private readonly GameService _service;

    public GamePersistenceTests()
    {
        _phraseServiceMock = new Mock<IGamePhraseService>();
        _gameRecordRepoMock = new Mock<IGameRecordRepository>();
        _userServiceMock = new Mock<IUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _analyticsServiceMock = new Mock<IAnalyticsService>();
        var loggerMock = new Mock<ILogger<GameService>>();
        var sessionStore = new InMemorySessionStore();
        var dbContextOptions = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: $"GamePersistenceTests_{Guid.NewGuid()}")
            .Options;
        _dbContext = new LinkittyDoDbContext(dbContextOptions);
        _service = new GameService(
            sessionStore,
            _phraseServiceMock.Object,
            _gameRecordRepoMock.Object,
            _userServiceMock.Object,
            _analyticsServiceMock.Object,
            _unitOfWorkMock.Object,
            _dbContext,
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

    // --- Awaited persistence tests ---

    [Fact]
    public async Task SubmitGuessAsync_WhenAllWordsSolved_CallsPersistWithTransaction()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        // Solve all hidden words
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        // Verify transaction was used
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(default), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task GiveUpAsync_CallsPersistWithTransaction()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.GiveUpAsync(session.SessionId);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task SubmitGuessAsync_PersistenceError_PropagatesException()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        _gameRecordRepoMock.Setup(r => r.CreateAsync(It.IsAny<GameRecord>()))
            .ThrowsAsync(new InvalidOperationException("DB write failed"));
        var session = await _service.StartNewGameAsync("USR-1");

        // Solve all words to trigger persistence
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" }));

        // Verify rollback was called
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task GiveUpAsync_PersistenceError_PropagatesException()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        _gameRecordRepoMock.Setup(r => r.CreateAsync(It.IsAny<GameRecord>()))
            .ThrowsAsync(new InvalidOperationException("DB write failed"));
        var session = await _service.StartNewGameAsync("USR-1");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.GiveUpAsync(session.SessionId));

        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(default), Times.Once);
    }

    // --- Guest session tests ---

    [Fact]
    public async Task SubmitGuessAsync_GuestSession_SkipsPersistence()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync(); // No userId = guest

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        // No persistence calls for guest
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(default), Times.Never);
        _gameRecordRepoMock.Verify(r => r.CreateAsync(It.IsAny<GameRecord>()), Times.Never);
    }

    [Fact]
    public async Task GiveUpAsync_GuestSession_SkipsPersistence()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync(); // guest

        await _service.GiveUpAsync(session.SessionId);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(default), Times.Never);
        _gameRecordRepoMock.Verify(r => r.CreateAsync(It.IsAny<GameRecord>()), Times.Never);
    }

    // --- Event recording tests ---

    [Fact]
    public async Task SubmitGuessAsync_RecordsGuessEvents_ForRegisteredUser()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        var guessEvents = session.GameRecord!.Events.OfType<GuessEvent>().ToList();
        Assert.Equal(2, guessEvents.Count);
        Assert.False(guessEvents[0].IsCorrect);
        Assert.True(guessEvents[1].IsCorrect);
        Assert.Equal(0, guessEvents[0].SequenceNumber);
        Assert.Equal(1, guessEvents[1].SequenceNumber);
    }

    [Fact]
    public async Task SubmitGuessAsync_WhenSolved_RecordsGameEndEvent()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        var endEvent = session.GameRecord!.Events.OfType<GameEndEvent>().SingleOrDefault();
        Assert.NotNull(endEvent);
        Assert.Equal("solved", endEvent.Reason);
        Assert.Equal(GameResult.Solved, session.GameRecord.Result);
        Assert.NotNull(session.GameRecord.CompletedAt);
    }

    [Fact]
    public async Task GiveUpAsync_RecordsGameEndEventWithGaveUpReason()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.GiveUpAsync(session.SessionId);

        var endEvent = session.GameRecord!.Events.OfType<GameEndEvent>().SingleOrDefault();
        Assert.NotNull(endEvent);
        Assert.Equal("gaveup", endEvent.Reason);
        Assert.Equal(GameResult.GaveUp, session.GameRecord.Result);
    }

    // --- Analytics isolation tests ---

    [Fact]
    public async Task SubmitGuessAsync_AnalyticsFailure_DoesNotRollbackGameData()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        _analyticsServiceMock.Setup(a => a.RecomputePlayerStatsAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Analytics failed"));
        var session = await _service.StartNewGameAsync("USR-1");

        // Should not throw even though analytics fails
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        // Transaction committed successfully despite analytics failure
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(default), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(default), Times.Never);
    }

    // --- GameRecord events saved to DB ---

    [Fact]
    public async Task PersistGameRecord_SavesEventsToDbContext()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        // Record a clue and a guess, then solve
        _service.RecordClueEvent(session.SessionId, 1, "fast", "https://example.com");
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        // Events should have been added to DbContext (via AddRange)
        var savedEvents = _dbContext.GameEvents.ToList();
        Assert.True(savedEvents.Count >= 4); // 1 clue + 3 guesses + 1 game end = 5
    }
}
