using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LinkittyDo.Api.Tests;

public class GameServiceTests
{
    private readonly Mock<IGamePhraseService> _phraseServiceMock;
    private readonly GameService _service;

    public GameServiceTests()
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
                new() { Index = 2, Text = "brown", IsHidden = true },
                new() { Index = 3, Text = "fox", IsHidden = true }
            }
        };
    }

    [Fact]
    public async Task StartNewGameAsync_CreatesGuestSession_WhenNoUserId()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());

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
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123")).ReturnsAsync(CreateTestPhrase());

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
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
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
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        Assert.True(result.IsCorrect);
        Assert.Equal(100, result.CurrentScore);
        Assert.Equal("quick", result.RevealedWord);
        Assert.False(result.IsPhraseComplete);
    }

    [Fact]
    public async Task SubmitGuess_CaseInsensitive()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "QUICK" });

        Assert.True(result.IsCorrect);
    }

    [Fact]
    public async Task SubmitGuess_IncorrectGuess_ReturnsFalse()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong" });

        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.CurrentScore);
        Assert.Null(result.RevealedWord);
    }

    [Fact]
    public async Task SubmitGuess_AllWordsRevealed_MarksComplete()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        Assert.True(result.IsPhraseComplete);
        Assert.Equal(300, result.CurrentScore);
    }

    [Fact]
    public async Task SubmitGuess_RecordsEvents_ForRegisteredUser()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123")).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-123");

        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong" });

        Assert.Equal(2, session.GameRecord!.Events.Count(e => e is GuessEvent));
        var correctGuess = session.GameRecord.Events.OfType<GuessEvent>().First(e => e.IsCorrect);
        Assert.Equal(100, correctGuess.PointsAwarded);
    }

    [Fact]
    public async Task SubmitGuess_RecordsSolvedEvent_WhenComplete()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123")).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-123");

        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });
        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 2, Guess = "brown" });
        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 3, Guess = "fox" });

        Assert.Equal(GameResult.Solved, session.GameRecord!.Result);
        Assert.NotNull(session.GameRecord.CompletedAt);
        var endEvent = session.GameRecord.Events.OfType<GameEndEvent>().Single();
        Assert.Equal("solved", endEvent.Reason);
    }

    [Fact]
    public async Task GetGameState_ReturnsCorrectState()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
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
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123")).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-123");

        var state = _service.GiveUp(session.SessionId);

        Assert.True(state.IsComplete);
        // All words should now have display text
        Assert.All(state.Words, w => Assert.NotNull(w.DisplayText));
        Assert.Equal(GameResult.GaveUp, session.GameRecord!.Result);
    }

    [Fact]
    public async Task RecordClueEvent_RecordsEvent_ForRegisteredUser()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-123")).ReturnsAsync(CreateTestPhrase());
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
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 99, Guess = "test" });

        Assert.False(result.IsCorrect);
    }

    [Fact]
    public async Task SubmitGuess_NonHiddenWord_ReturnsFalse()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 0, Guess = "the" });

        Assert.False(result.IsCorrect);
    }
}
