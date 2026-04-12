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
        var dailyChallengeServiceMock = new Mock<IDailyChallengeService>();
        _service = new GameService(
            sessionStore,
            _phraseServiceMock.Object,
            _gameRecordRepoMock.Object,
            _userServiceMock.Object,
            _analyticsServiceMock.Object,
            _unitOfWorkMock.Object,
            _dbContext,
            dailyChallengeServiceMock.Object,
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

    // --- Game start persistence tests ---

    [Fact]
    public async Task StartNewGameAsync_RegisteredUser_PersistsGameRecordImmediately()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());

        var session = await _service.StartNewGameAsync("USR-1");

        _gameRecordRepoMock.Verify(r => r.CreateAsync(It.Is<GameRecord>(
            gr => gr.UserId == "USR-1" && gr.Result == GameResult.InProgress)), Times.Once);
    }

    [Fact]
    public async Task StartNewGameAsync_DbFailure_ThrowsAndDoesNotCreateSession()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        _gameRecordRepoMock.Setup(r => r.CreateAsync(It.IsAny<GameRecord>()))
            .ThrowsAsync(new InvalidOperationException("DB write failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.StartNewGameAsync("USR-1"));
    }

    [Fact]
    public async Task StartNewGameAsync_GuestUser_NoPersistence()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());

        await _service.StartNewGameAsync();

        _gameRecordRepoMock.Verify(r => r.CreateAsync(It.IsAny<GameRecord>()), Times.Never);
    }

    // --- Game completion persistence tests ---

    [Fact]
    public async Task SubmitGuessAsync_WhenAllWordsSolved_UpdatesRecordWithTransaction()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        // Verify completion uses UpdateAsync (not CreateAsync, which was at start)
        _gameRecordRepoMock.Verify(r => r.UpdateAsync(It.Is<GameRecord>(
            gr => gr.Result == GameResult.Solved)), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task GiveUpAsync_UpdatesRecordWithTransaction()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.GiveUpAsync(session.SessionId);

        _gameRecordRepoMock.Verify(r => r.UpdateAsync(It.Is<GameRecord>(
            gr => gr.Result == GameResult.GaveUp)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task SubmitGuessAsync_CompletionUpdateError_ReturnsFailed()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        _gameRecordRepoMock.Setup(r => r.UpdateAsync(It.IsAny<GameRecord>()))
            .ThrowsAsync(new InvalidOperationException("DB update failed"));
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        Assert.Equal(PersistenceStatus.Failed, result.PersistenceStatus);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task GiveUpAsync_UpdateError_ReturnsFailed()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        _gameRecordRepoMock.Setup(r => r.UpdateAsync(It.IsAny<GameRecord>()))
            .ThrowsAsync(new InvalidOperationException("DB update failed"));
        var session = await _service.StartNewGameAsync("USR-1");

        var state = await _service.GiveUpAsync(session.SessionId);

        Assert.Equal(PersistenceStatus.Failed, state.PersistenceStatus);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(default), Times.Once);
    }

    // --- Guest session tests ---

    [Fact]
    public async Task SubmitGuessAsync_GuestSession_SkipsPersistence()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(default), Times.Never);
        _gameRecordRepoMock.Verify(r => r.UpdateAsync(It.IsAny<GameRecord>()), Times.Never);
    }

    [Fact]
    public async Task GiveUpAsync_GuestSession_SkipsPersistence()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        await _service.GiveUpAsync(session.SessionId);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(default), Times.Never);
        _gameRecordRepoMock.Verify(r => r.UpdateAsync(It.IsAny<GameRecord>()), Times.Never);
    }

    // --- Incremental event persistence tests ---

    [Fact]
    public async Task SubmitGuessAsync_PersistsGuessEventImmediately()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong" });

        var savedEvents = _dbContext.GameEvents.OfType<GuessEvent>().ToList();
        Assert.Single(savedEvents);
        Assert.Equal("wrong", savedEvents[0].GuessText);
    }

    [Fact]
    public async Task RecordClueEventAsync_PersistsClueEventImmediately()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.RecordClueEventAsync(session.SessionId, 1, "fast", "https://example.com");

        var savedEvents = _dbContext.GameEvents.OfType<ClueEvent>().ToList();
        Assert.Single(savedEvents);
        Assert.Equal("fast", savedEvents[0].SearchTerm);
    }

    // --- Event recording correctness ---

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

    // --- Analytics isolation ---

    [Fact]
    public async Task SubmitGuessAsync_AnalyticsFailure_DoesNotRollbackGameData()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        _analyticsServiceMock.Setup(a => a.RecomputePlayerStatsAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Analytics failed"));
        var session = await _service.StartNewGameAsync("USR-1");

        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(default), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(default), Times.Never);
    }

    // --- Abandoned game tracking ---

    [Fact]
    public async Task RemoveExpiredSessionsAsync_MarksRegisteredGamesAsAbandoned()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");
        session.LastActivityAt = DateTime.UtcNow.AddHours(-25);

        var removed = await _service.RemoveExpiredSessionsAsync(TimeSpan.FromHours(24));

        Assert.Equal(1, removed);
        _gameRecordRepoMock.Verify(r => r.UpdateAsync(It.Is<GameRecord>(
            gr => gr.Result == GameResult.Abandoned && gr.CompletedAt != null)), Times.Once);
    }

    [Fact]
    public async Task RemoveExpiredSessionsAsync_GuestSession_NoAbandonedPersistence()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();
        session.LastActivityAt = DateTime.UtcNow.AddHours(-25);

        var removed = await _service.RemoveExpiredSessionsAsync(TimeSpan.FromHours(24));

        Assert.Equal(1, removed);
        _gameRecordRepoMock.Verify(r => r.UpdateAsync(It.IsAny<GameRecord>()), Times.Never);
    }

    // --- Persistence status tests (#125) ---

    [Fact]
    public async Task SubmitGuessAsync_RegisteredUser_ReturnsSavedStatus()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        Assert.Equal(PersistenceStatus.Saved, result.PersistenceStatus);
    }

    [Fact]
    public async Task SubmitGuessAsync_GuestSession_ReturnsNotApplicableStatus()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = await _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        Assert.Equal(PersistenceStatus.NotApplicable, result.PersistenceStatus);
    }

    [Fact]
    public async Task GiveUpAsync_RegisteredUser_ReturnsSavedStatus()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        var state = await _service.GiveUpAsync(session.SessionId);

        Assert.Equal(PersistenceStatus.Saved, state.PersistenceStatus);
    }

    [Fact]
    public async Task GiveUpAsync_GuestSession_ReturnsNotApplicableStatus()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var state = await _service.GiveUpAsync(session.SessionId);

        Assert.Equal(PersistenceStatus.NotApplicable, state.PersistenceStatus);
    }
}
