using LinkittyDo.Api.Controllers;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LinkittyDo.Api.Tests;

public class ClueControllerTests
{
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<IClueService> _clueServiceMock;
    private readonly ClueController _controller;

    public ClueControllerTests()
    {
        _gameServiceMock = new Mock<IGameService>();
        _clueServiceMock = new Mock<IClueService>();
        _controller = new ClueController(_gameServiceMock.Object, _clueServiceMock.Object);
    }

    private static GameSession CreateTestSession()
    {
        return new GameSession
        {
            SessionId = Guid.NewGuid(),
            Phrase = new Phrase
            {
                Id = 1,
                FullText = "the quick brown fox",
                Words = new List<PhraseWord>
                {
                    new() { Index = 0, Text = "the", IsHidden = false },
                    new() { Index = 1, Text = "quick", IsHidden = true, ClueSearchTerm = "fast" },
                    new() { Index = 2, Text = "brown", IsHidden = true },
                    new() { Index = 3, Text = "fox", IsHidden = true }
                }
            },
            UsedClueUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    [Fact]
    public async Task GetClue_ReturnsOk_WithWrappedClueResponse()
    {
        var session = CreateTestSession();
        var clue = new ClueResponse { Url = "https://example.com/fast", SearchTerm = "fast" };

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _clueServiceMock.Setup(s => s.GetClueAsync(session, 1)).ReturnsAsync(clue);

        var result = await _controller.GetClue(session.SessionId, 1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ClueResponse>>(okResult.Value);
        Assert.Equal("https://example.com/fast", response.Data!.Url);
        Assert.Equal("fast", response.Data.SearchTerm);
    }

    [Fact]
    public async Task GetClue_ReturnsNotFound_WhenSessionMissing()
    {
        var sessionId = Guid.NewGuid();
        _gameServiceMock.Setup(s => s.GetGame(sessionId)).Returns((GameSession?)null);

        var result = await _controller.GetClue(sessionId, 0);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal("GAME_NOT_FOUND", error.Error.Code);
    }

    [Fact]
    public async Task GetClue_ReturnsNotFound_WhenWordIndexInvalid()
    {
        var session = CreateTestSession();
        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);

        var result = await _controller.GetClue(session.SessionId, 99);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal("WORD_NOT_FOUND", error.Error.Code);
    }

    [Fact]
    public async Task GetClue_ReturnsBadRequest_WhenWordNotHidden()
    {
        var session = CreateTestSession();
        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);

        var result = await _controller.GetClue(session.SessionId, 0); // "the" is not hidden

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
    }

    [Fact]
    public async Task GetClue_RecordsClueEvent_WhenUrlReturned()
    {
        var session = CreateTestSession();
        var clue = new ClueResponse { Url = "https://example.com/clue", SearchTerm = "fast" };

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _clueServiceMock.Setup(s => s.GetClueAsync(session, 1)).ReturnsAsync(clue);

        await _controller.GetClue(session.SessionId, 1);

        _gameServiceMock.Verify(s => s.RecordClueEventAsync(session.SessionId, 1, "fast", "https://example.com/clue"), Times.Once);
    }

    [Fact]
    public async Task GetClue_DoesNotRecordEvent_WhenUrlEmpty()
    {
        var session = CreateTestSession();
        var clue = new ClueResponse { Url = "", SearchTerm = "fast" };

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _clueServiceMock.Setup(s => s.GetClueAsync(session, 1)).ReturnsAsync(clue);

        await _controller.GetClue(session.SessionId, 1);

        _gameServiceMock.Verify(s => s.RecordClueEventAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetClue_AddsExcludedUrlsToSession()
    {
        var session = CreateTestSession();
        var clue = new ClueResponse { Url = "https://new.com", SearchTerm = "fast" };
        var excludeUrls = new List<string> { "https://already-seen.com" };

        _gameServiceMock.Setup(s => s.GetGame(session.SessionId)).Returns(session);
        _clueServiceMock.Setup(s => s.GetClueAsync(session, 1)).ReturnsAsync(clue);

        await _controller.GetClue(session.SessionId, 1, excludeUrls);

        Assert.Contains("https://already-seen.com", session.UsedClueUrls);
    }
}
