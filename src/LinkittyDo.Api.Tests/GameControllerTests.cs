using LinkittyDo.Api.Controllers;
using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LinkittyDo.Api.Tests;

public class GameControllerTests
{
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IGameRecordRepository> _gameRecordRepoMock;
    private readonly GameController _controller;

    public GameControllerTests()
    {
        _gameServiceMock = new Mock<IGameService>();
        _userServiceMock = new Mock<IUserService>();
        _gameRecordRepoMock = new Mock<IGameRecordRepository>();
        _controller = new GameController(_gameServiceMock.Object, _userServiceMock.Object, _gameRecordRepoMock.Object);
    }

    private static GameSession CreateTestSession(bool isGuest = true)
    {
        var sessionId = Guid.NewGuid();
        return new GameSession
        {
            SessionId = sessionId,
            PhraseId = 1,
            Phrase = new Phrase
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
            },
            RevealedWords = new Dictionary<int, bool> { { 1, false }, { 2, false }, { 3, false } },
            Score = 0,
            UserId = isGuest ? null : "USR-1234567890123-ABC123",
            GameRecord = isGuest ? null : new GameRecord
            {
                GameId = "GAME-123-ABC",
                PhraseText = "the quick brown fox",
                Events = new List<GameEvent>()
            }
        };
    }

    private static GameState CreateTestGameState(Guid sessionId)
    {
        return new GameState
        {
            SessionId = sessionId,
            Words = new List<WordState>
            {
                new() { Index = 0, DisplayText = "the", IsHidden = false, IsRevealed = false },
                new() { Index = 1, DisplayText = null, IsHidden = true, IsRevealed = false },
                new() { Index = 2, DisplayText = null, IsHidden = true, IsRevealed = false },
                new() { Index = 3, DisplayText = null, IsHidden = true, IsRevealed = false }
            },
            Score = 0,
            IsComplete = false
        };
    }

    [Fact]
    public async Task StartGame_ReturnsOk_WithWrappedGameState()
    {
        var session = CreateTestSession();
        var state = CreateTestGameState(session.SessionId);

        _gameServiceMock.Setup(s => s.StartNewGameAsync(null, 10)).ReturnsAsync(session);
        _gameServiceMock.Setup(s => s.GetGameState(session.SessionId)).Returns(state);

        var result = await _controller.StartGame(null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GameState>>(okResult.Value);
        Assert.Equal(session.SessionId, response.Data!.SessionId);
        Assert.Equal(4, response.Data.Words.Count);
    }

    [Fact]
    public async Task StartGame_WithRequest_UsesProvidedValues()
    {
        var request = new StartGameRequest { UserId = "USR-123", Difficulty = 50 };
        var session = CreateTestSession(isGuest: false);
        var state = CreateTestGameState(session.SessionId);

        _gameServiceMock.Setup(s => s.StartNewGameAsync("USR-123", 50)).ReturnsAsync(session);
        _gameServiceMock.Setup(s => s.GetGameState(session.SessionId)).Returns(state);

        var result = await _controller.StartGame(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<ApiResponse<GameState>>(okResult.Value);
        _gameServiceMock.Verify(s => s.StartNewGameAsync("USR-123", 50), Times.Once);
    }

    [Fact]
    public void GetGame_ReturnsOk_WhenFound()
    {
        var session = CreateTestSession();
        var state = CreateTestGameState(session.SessionId);

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _gameServiceMock.Setup(s => s.GetGameState(session.SessionId)).Returns(state);

        var result = _controller.GetGame(session.SessionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GameState>>(okResult.Value);
        Assert.Equal(session.SessionId, response.Data!.SessionId);
    }

    [Fact]
    public void GetGame_ReturnsNotFound_WhenMissing()
    {
        var sessionId = Guid.NewGuid();
        _gameServiceMock.Setup(s => s.GetGame(sessionId)).Returns((GameSession?)null);

        var result = _controller.GetGame(sessionId);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal("GAME_NOT_FOUND", error.Error.Code);
    }

    [Fact]
    public async Task SubmitGuess_ReturnsOk_WithWrappedResponse()
    {
        var session = CreateTestSession();
        var request = new GuessRequest { WordIndex = 1, Guess = "quick" };
        var guessResponse = new GuessResponse
        {
            IsCorrect = true,
            IsPhraseComplete = false,
            CurrentScore = 100,
            RevealedWord = "quick"
        };

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _gameServiceMock.Setup(s => s.SubmitGuessAsync(session.SessionId, request)).ReturnsAsync(guessResponse);

        var result = await _controller.SubmitGuess(session.SessionId, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GuessResponse>>(okResult.Value);
        Assert.True(response.Data!.IsCorrect);
        Assert.Equal(100, response.Data.CurrentScore);
    }

    [Fact]
    public async Task SubmitGuess_ReturnsNotFound_WhenSessionMissing()
    {
        var sessionId = Guid.NewGuid();
        _gameServiceMock.Setup(s => s.GetGame(sessionId)).Returns((GameSession?)null);

        var result = await _controller.SubmitGuess(sessionId, new GuessRequest { WordIndex = 1, Guess = "test" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitGuess_CompletesGame_WhenPhraseCompleteAndNotGuest()
    {
        var session = CreateTestSession(isGuest: false);
        var request = new GuessRequest { WordIndex = 1, Guess = "quick" };
        var guessResponse = new GuessResponse { IsCorrect = true, IsPhraseComplete = true, CurrentScore = 300 };

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _gameServiceMock.Setup(s => s.SubmitGuessAsync(session.SessionId, request)).ReturnsAsync(guessResponse);

        var result = await _controller.SubmitGuess(session.SessionId, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GuessResponse>>(okResult.Value);
        Assert.True(response.Data!.IsPhraseComplete);
    }

    [Fact]
    public async Task GiveUp_ReturnsOk_WithWrappedState()
    {
        var session = CreateTestSession();
        var state = CreateTestGameState(session.SessionId);
        state.IsComplete = true;

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _gameServiceMock.Setup(s => s.GiveUpAsync(session.SessionId)).ReturnsAsync(state);

        var result = await _controller.GiveUp(session.SessionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GameState>>(okResult.Value);
        Assert.True(response.Data!.IsComplete);
    }

    [Fact]
    public async Task GiveUp_ReturnsNotFound_WhenSessionMissing()
    {
        var sessionId = Guid.NewGuid();
        _gameServiceMock.Setup(s => s.GetGame(sessionId)).Returns((GameSession?)null);

        var result = await _controller.GiveUp(sessionId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void GetGameRecord_ReturnsOk_WithWrappedRecord()
    {
        var session = CreateTestSession(isGuest: false);
        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);

        var result = _controller.GetGameRecord(session.SessionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GameRecord>>(okResult.Value);
        Assert.Equal("GAME-123-ABC", response.Data!.GameId);
    }

    [Fact]
    public void GetGameRecord_ReturnsNotFound_ForGuestSession()
    {
        var session = CreateTestSession(isGuest: true);
        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);

        var result = _controller.GetGameRecord(session.SessionId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void GetGameRecord_ReturnsNotFound_WhenSessionMissing()
    {
        var sessionId = Guid.NewGuid();
        _gameServiceMock.Setup(s => s.GetGame(sessionId)).Returns((GameSession?)null);

        var result = _controller.GetGameRecord(sessionId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
