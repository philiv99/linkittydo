using LinkittyDo.Api.Controllers;
using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LinkittyDo.Api.Tests;

public class LeaderboardTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly Mock<IGameRecordRepository> _gameRecordRepoMock;
    private readonly UserService _service;

    public LeaderboardTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _gameRecordRepoMock = new Mock<IGameRecordRepository>();
        _service = new UserService(_repoMock.Object, _gameRecordRepoMock.Object);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsUsersOrderedByPoints()
    {
        var users = new List<User>
        {
            new() { UniqueId = "USR-1", Name = "Alice", LifetimePoints = 500 },
            new() { UniqueId = "USR-2", Name = "Bob", LifetimePoints = 1000 },
            new() { UniqueId = "USR-3", Name = "Charlie", LifetimePoints = 750 }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = (await _service.GetLeaderboardAsync(10)).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Bob", result[0].Name);
        Assert.Equal("Charlie", result[1].Name);
        Assert.Equal("Alice", result[2].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_RespectsTopLimit()
    {
        var users = Enumerable.Range(1, 20).Select(i => new User
        {
            UniqueId = $"USR-{i}",
            Name = $"Player{i}",
            LifetimePoints = i * 100
        }).ToList();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = (await _service.GetLeaderboardAsync(5)).ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal("Player20", result[0].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_TiedPointsOrderedByName()
    {
        var users = new List<User>
        {
            new() { UniqueId = "USR-1", Name = "Zara", LifetimePoints = 500 },
            new() { UniqueId = "USR-2", Name = "Alice", LifetimePoints = 500 }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = (await _service.GetLeaderboardAsync(10)).ToList();

        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Zara", result[1].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_EmptyUsers_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        var result = (await _service.GetLeaderboardAsync(10)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task LeaderboardEndpoint_InvalidTop_ReturnsBadRequest()
    {
        var serviceMock = new Mock<IUserService>();
        var controller = new UserController(serviceMock.Object);

        var result = await controller.GetLeaderboard(0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task LeaderboardEndpoint_ValidTop_ReturnsRankedEntries()
    {
        var serviceMock = new Mock<IUserService>();
        var users = new List<User>
        {
            new() { UniqueId = "USR-1", Name = "Alice", LifetimePoints = 1000 },
            new() { UniqueId = "USR-2", Name = "Bob", LifetimePoints = 500 }
        };
        serviceMock.Setup(s => s.GetLeaderboardAsync(10)).ReturnsAsync(users);
        serviceMock.Setup(s => s.GetGameCountAsync("USR-1")).ReturnsAsync(2);
        serviceMock.Setup(s => s.GetGameCountAsync("USR-2")).ReturnsAsync(1);
        var controller = new UserController(serviceMock.Object);

        var result = await controller.GetLeaderboard(10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<LeaderboardEntry>>>(okResult.Value);
        var entries = response.Data!.ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(1, entries[0].Rank);
        Assert.Equal("Alice", entries[0].Name);
        Assert.Equal(1000, entries[0].LifetimePoints);
        Assert.Equal(2, entries[0].GamesPlayed);
        Assert.Equal(2, entries[1].Rank);
    }
}
