using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
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
        _service = new GameService(_phraseServiceMock.Object, loggerMock.Object);
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

        var removed = _service.RemoveExpiredSessions(TimeSpan.FromHours(24));
        Assert.Equal(1, removed);
        Assert.Equal(0, _service.ActiveSessionCount);
    }

    [Fact]
    public async Task RemoveExpiredSessions_KeepsRecentSessions()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        await _service.StartNewGameAsync();

        var removed = _service.RemoveExpiredSessions(TimeSpan.FromHours(24));
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

        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        Assert.True(session.LastActivityAt > oldActivity);
    }

    [Fact]
    public async Task LastActivityAt_UpdatesOnClue()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        session.LastActivityAt = DateTime.UtcNow.AddHours(-1);
        var oldActivity = session.LastActivityAt;

        _service.RecordClueEvent(session.SessionId, 1, "fast", "http://example.com");

        Assert.True(session.LastActivityAt > oldActivity);
    }

    [Fact]
    public async Task RemoveExpiredSessions_ReturnsZeroWhenEmpty()
    {
        var removed = _service.RemoveExpiredSessions(TimeSpan.FromHours(24));
        Assert.Equal(0, removed);
    }
}
