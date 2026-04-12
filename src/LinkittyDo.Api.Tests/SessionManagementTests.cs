using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LinkittyDo.Api.Tests;

public class SessionManagementTests
{
    private readonly Mock<IGamePhraseService> _phraseServiceMock;
    private readonly GameService _service;

    public SessionManagementTests()
    {
        _phraseServiceMock = new Mock<IGamePhraseService>();
        var loggerMock = new Mock<ILogger<GameService>>();
        var sessionStore = new InMemorySessionStore();
        var gameRecordRepoMock = new Mock<IGameRecordRepository>();
        var userServiceMock = new Mock<IUserService>();
        var analyticsServiceMock = new Mock<IAnalyticsService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var dbContextOptions = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: $"SessionManagementTests_{Guid.NewGuid()}")
            .Options;
        var dbContext = new LinkittyDoDbContext(dbContextOptions);
        var dailyChallengeServiceMock = new Mock<IDailyChallengeService>();
        _service = new GameService(
            sessionStore,
            _phraseServiceMock.Object,
            gameRecordRepoMock.Object,
            userServiceMock.Object,
            analyticsServiceMock.Object,
            unitOfWorkMock.Object,
            dbContext,
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
            }
        };
    }

    [Fact]
    public void ActiveSessionCount_StartsAtZero()
    {
        Assert.Equal(0, _service.ActiveSessionCount);
    }

    [Fact]
    public async Task ActiveSessionCount_IncreasesOnNewGame()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        await _service.StartNewGameAsync();
        Assert.Equal(1, _service.ActiveSessionCount);
    }

    [Fact]
    public async Task RemoveExpiredSessions_RemovesOldSessions()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        // Manually set LastActivityAt to the past
        session.LastActivityAt = DateTime.UtcNow.AddHours(-25);

        var removed = await _service.RemoveExpiredSessionsAsync(TimeSpan.FromHours(24));
        Assert.Equal(1, removed);
        Assert.Equal(0, _service.ActiveSessionCount);
    }

    [Fact]
    public async Task RemoveExpiredSessions_KeepsRecentSessions()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        await _service.StartNewGameAsync();

        var removed = await _service.RemoveExpiredSessionsAsync(TimeSpan.FromHours(24));
        Assert.Equal(0, removed);
        Assert.Equal(1, _service.ActiveSessionCount);
    }

    [Fact]
    public async Task LastActivityAt_IsSetOnGameStart()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var before = DateTime.UtcNow;
        var session = await _service.StartNewGameAsync();
        var after = DateTime.UtcNow;

        Assert.InRange(session.LastActivityAt, before, after);
    }

    [Fact]
    public async Task LastActivityAt_UpdatesOnGuess()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        // Set LastActivityAt to past
        session.LastActivityAt = DateTime.UtcNow.AddHours(-1);
        var oldActivity = session.LastActivityAt;

        _service.SubmitGuessAsync(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" }).GetAwaiter().GetResult();

        Assert.True(session.LastActivityAt > oldActivity);
    }

    [Fact]
    public async Task LastActivityAt_UpdatesOnClue()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        session.LastActivityAt = DateTime.UtcNow.AddHours(-1);
        var oldActivity = session.LastActivityAt;

        await _service.RecordClueEventAsync(session.SessionId, 1, "fast", "http://example.com");

        Assert.True(session.LastActivityAt > oldActivity);
    }

    [Fact]
    public async Task RemoveExpiredSessions_ReturnsZeroWhenEmpty()
    {
        var removed = await _service.RemoveExpiredSessionsAsync(TimeSpan.FromHours(24));
        Assert.Equal(0, removed);
    }
}
