using LinkittyDo.Api.Controllers;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LinkittyDo.Api.Tests;

public class DailyChallengeControllerTests
{
    private readonly Mock<IDailyChallengeService> _dailyChallengeServiceMock;
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly DailyChallengeController _controller;

    public DailyChallengeControllerTests()
    {
        _dailyChallengeServiceMock = new Mock<IDailyChallengeService>();
        _gameServiceMock = new Mock<IGameService>();
        _controller = new DailyChallengeController(_dailyChallengeServiceMock.Object, _gameServiceMock.Object);
    }

    [Fact]
    public async Task GetTodaysChallenge_ReturnsOk_WithStatus()
    {
        var status = new DailyChallengeResponse
        {
            Date = DateTime.UtcNow.Date,
            PhraseUniqueId = "PH-123",
            AlreadyPlayed = false
        };
        _dailyChallengeServiceMock.Setup(s => s.GetChallengeStatusAsync(null)).ReturnsAsync(status);

        var result = await _controller.GetTodaysChallenge();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DailyChallengeResponse>>(okResult.Value);
        Assert.False(response.Data!.AlreadyPlayed);
    }

    [Fact]
    public async Task GetTodaysChallenge_ReturnsAlreadyPlayed_WhenUserCompleted()
    {
        var userId = "USR-123-ABC";
        var status = new DailyChallengeResponse
        {
            Date = DateTime.UtcNow.Date,
            PhraseUniqueId = "PH-123",
            AlreadyPlayed = true,
            PreviousResult = new DailyChallengeResultResponse
            {
                Score = 300,
                Result = "Solved",
                CompletedAt = DateTime.UtcNow
            }
        };
        _dailyChallengeServiceMock.Setup(s => s.GetChallengeStatusAsync(userId)).ReturnsAsync(status);

        var result = await _controller.GetTodaysChallenge(userId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DailyChallengeResponse>>(okResult.Value);
        Assert.True(response.Data!.AlreadyPlayed);
        Assert.Equal(300, response.Data.PreviousResult!.Score);
    }

    [Fact]
    public async Task StartDailyChallenge_ReturnsOk_WhenNotYetPlayed()
    {
        var sessionId = Guid.NewGuid();
        var request = new StartGameRequest { UserId = "USR-123-ABC", Difficulty = 20 };
        var status = new DailyChallengeResponse { AlreadyPlayed = false };
        var session = new GameSession { SessionId = sessionId };
        var gameState = new GameState
        {
            SessionId = sessionId,
            Words = new List<WordState>(),
            Score = 0,
            IsComplete = false
        };

        _dailyChallengeServiceMock.Setup(s => s.GetChallengeStatusAsync("USR-123-ABC")).ReturnsAsync(status);
        _gameServiceMock.Setup(s => s.StartDailyChallengeAsync("USR-123-ABC", 20)).ReturnsAsync(session);
        _gameServiceMock.Setup(s => s.GetGameState(sessionId)).Returns(gameState);

        var result = await _controller.StartDailyChallenge(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GameState>>(okResult.Value);
        Assert.Equal(sessionId, response.Data!.SessionId);
    }

    [Fact]
    public async Task StartDailyChallenge_ReturnsConflict_WhenAlreadyPlayed()
    {
        var request = new StartGameRequest { UserId = "USR-123-ABC" };
        var status = new DailyChallengeResponse { AlreadyPlayed = true };

        _dailyChallengeServiceMock.Setup(s => s.GetChallengeStatusAsync("USR-123-ABC")).ReturnsAsync(status);

        var result = await _controller.StartDailyChallenge(request);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal("ALREADY_PLAYED", error.Error.Code);
    }

    [Fact]
    public async Task StartDailyChallenge_SkipsCheck_ForGuestUser()
    {
        var sessionId = Guid.NewGuid();
        var request = new StartGameRequest { Difficulty = 10 };
        var session = new GameSession { SessionId = sessionId };
        var gameState = new GameState
        {
            SessionId = sessionId,
            Words = new List<WordState>(),
            Score = 0,
            IsComplete = false
        };

        _gameServiceMock.Setup(s => s.StartDailyChallengeAsync(null, 10)).ReturnsAsync(session);
        _gameServiceMock.Setup(s => s.GetGameState(sessionId)).Returns(gameState);

        var result = await _controller.StartDailyChallenge(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _dailyChallengeServiceMock.Verify(s => s.GetChallengeStatusAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsOk_WithEntries()
    {
        var date = DateTime.UtcNow.Date;
        var entries = new List<DailyChallengeLeaderboardEntry>
        {
            new() { Rank = 1, PlayerName = "Alice", Score = 500, Result = "Solved" },
            new() { Rank = 2, PlayerName = "Bob", Score = 300, Result = "Solved" }
        };

        _dailyChallengeServiceMock.Setup(s => s.GetDailyLeaderboardAsync(date, 10)).ReturnsAsync(entries);

        var result = await _controller.GetLeaderboard(date, 10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<DailyChallengeLeaderboardEntry>>>(okResult.Value);
        Assert.Equal(2, response.Data!.Count);
        Assert.Equal("Alice", response.Data[0].PlayerName);
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsEmpty_WhenNoResults()
    {
        var date = DateTime.UtcNow.Date;
        _dailyChallengeServiceMock.Setup(s => s.GetDailyLeaderboardAsync(date, 10))
            .ReturnsAsync(new List<DailyChallengeLeaderboardEntry>());

        var result = await _controller.GetLeaderboard(date, 10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<DailyChallengeLeaderboardEntry>>>(okResult.Value);
        Assert.Empty(response.Data!);
    }
}
