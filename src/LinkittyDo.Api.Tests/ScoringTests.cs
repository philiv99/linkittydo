using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LinkittyDo.Api.Tests;

public class ScoringTests
{
    private readonly Mock<IGamePhraseService> _phraseServiceMock;
    private readonly GameService _service;

    public ScoringTests()
    {
        _phraseServiceMock = new Mock<IGamePhraseService>();
        var loggerMock = new Mock<ILogger<GameService>>();
        var sessionStore = new InMemorySessionStore();
        var gameRecordRepoMock = new Mock<IGameRecordRepository>();
        var userServiceMock = new Mock<IUserService>();
        var analyticsServiceMock = new Mock<IAnalyticsService>();
        var dbContextOptions = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: $"ScoringTests_{Guid.NewGuid()}")
            .Options;
        var dbContext = new LinkittyDoDbContext(dbContextOptions);
        _service = new GameService(
            sessionStore,
            _phraseServiceMock.Object,
            gameRecordRepoMock.Object,
            userServiceMock.Object,
            analyticsServiceMock.Object,
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

    // --- GetBasePoints tests ---

    [Theory]
    [InlineData(0, 100)]
    [InlineData(10, 100)]
    [InlineData(20, 100)]
    [InlineData(21, 150)]
    [InlineData(50, 150)]
    [InlineData(51, 200)]
    [InlineData(80, 200)]
    [InlineData(81, 300)]
    [InlineData(100, 300)]
    public void GetBasePoints_ReturnsTieredValues(int difficulty, int expectedBasePoints)
    {
        Assert.Equal(expectedBasePoints, GameService.GetBasePoints(difficulty));
    }

    // --- CalculateWordScore tests ---

    [Fact]
    public void CalculateWordScore_FirstGuessNoClues_Awards2xBonus()
    {
        var session = new GameSession { Difficulty = 10 };
        session.GuessCountPerWord[1] = 1;
        // No clue count entry means 0 clues

        var score = GameService.CalculateWordScore(session, 1);

        // Easy base=100, 100/1 * 2x = 200
        Assert.Equal(200, score);
    }

    [Fact]
    public void CalculateWordScore_WithOneClue_NoBonus()
    {
        var session = new GameSession { Difficulty = 10 };
        session.ClueCountPerWord[1] = 1;
        session.GuessCountPerWord[1] = 1;

        var score = GameService.CalculateWordScore(session, 1);

        // Easy base=100, 100/(1*1) = 100, no bonus (had 1 clue)
        Assert.Equal(100, score);
    }

    [Fact]
    public void CalculateWordScore_WithMultipleCluesAndGuesses_ReducesScore()
    {
        var session = new GameSession { Difficulty = 10 };
        session.ClueCountPerWord[1] = 3;
        session.GuessCountPerWord[1] = 2;

        var score = GameService.CalculateWordScore(session, 1);

        // Easy base=100, 100/(3*2) = 16.67 -> 17
        Assert.Equal(17, score);
    }

    [Fact]
    public void CalculateWordScore_HardDifficulty_HigherBasePoints()
    {
        var session = new GameSession { Difficulty = 60 };
        session.ClueCountPerWord[1] = 1;
        session.GuessCountPerWord[1] = 1;

        var score = GameService.CalculateWordScore(session, 1);

        // Hard base=200, 200/(1*1) = 200
        Assert.Equal(200, score);
    }

    [Fact]
    public void CalculateWordScore_ExpertDifficulty_HighestBasePoints()
    {
        var session = new GameSession { Difficulty = 90 };
        session.GuessCountPerWord[1] = 1;

        var score = GameService.CalculateWordScore(session, 1);

        // Expert base=300, no clues, first guess: 300/1 * 2x = 600
        Assert.Equal(600, score);
    }

    [Fact]
    public void CalculateWordScore_ManyAttempts_MinimumScore()
    {
        var session = new GameSession { Difficulty = 10 };
        session.ClueCountPerWord[1] = 5;
        session.GuessCountPerWord[1] = 10;

        var score = GameService.CalculateWordScore(session, 1);

        // 100/(5*10) = 2
        Assert.Equal(2, score);
    }

    // --- Integration: scoring through SubmitGuess ---

    [Fact]
    public async Task SubmitGuess_FirstGuessNoClues_Awards2xBonus()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        Assert.True(result.IsCorrect);
        Assert.Equal(200, result.CurrentScore);
    }

    [Fact]
    public async Task SubmitGuess_AfterClue_NoBonus()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        // Record a clue for word 1
        _service.RecordClueEvent(session.SessionId, 1, "fast", "https://example.com");

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        // 1 clue, 1 guess, no bonus: 100/(1*1) = 100
        Assert.Equal(100, result.CurrentScore);
    }

    [Fact]
    public async Task SubmitGuess_MultipleWrongGuesses_ReducesScore()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        // Two wrong guesses first
        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong1" });
        _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "wrong2" });
        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        // 0 clues (effective 1), 3 guesses, no first-guess bonus: 100/(1*3) = 33
        Assert.Equal(33, result.CurrentScore);
    }

    [Fact]
    public async Task SubmitGuess_HighDifficulty_HigherPoints()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 60)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync(null, 60);

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        // Hard base=200, first guess no clues: 200 * 2 = 400
        Assert.Equal(400, result.CurrentScore);
    }

    [Fact]
    public async Task SubmitGuess_ClueCountTracked_AcrossMultipleClues()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync("USR-1", 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync("USR-1");

        // Record 2 clues for word 1
        _service.RecordClueEvent(session.SessionId, 1, "fast", "https://example1.com");
        _service.RecordClueEvent(session.SessionId, 1, "rapid", "https://example2.com");

        var result = _service.SubmitGuess(session.SessionId, new GuessRequest { WordIndex = 1, Guess = "quick" });

        // 2 clues, 1 guess: 100/(2*1) = 50
        Assert.Equal(50, result.CurrentScore);
    }

    [Fact]
    public async Task RecordClueEvent_TracksClueCount_ForGuestSessions()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 10)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync();

        // Record clue for guest (no GameRecord but clue count should still be tracked)
        _service.RecordClueEvent(session.SessionId, 1, "fast", "https://example.com");

        // Verify the clue count is tracked on the session
        Assert.Equal(1, session.ClueCountPerWord[1]);
    }

    [Fact]
    public async Task GameSession_StoresDifficulty()
    {
        _phraseServiceMock.Setup(s => s.GetPhraseForUserAsync(null, 75)).ReturnsAsync(CreateTestPhrase());
        var session = await _service.StartNewGameAsync(null, 75);

        Assert.Equal(75, session.Difficulty);
    }
}
