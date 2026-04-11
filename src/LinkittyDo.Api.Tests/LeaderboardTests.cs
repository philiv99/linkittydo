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
    public async Task GetLeaderboardAsync_ExcludesSimulatedUsers()
    {
        var users = new List<User>
        {
            new() { UniqueId = "USR-1", Name = "Alice", LifetimePoints = 1000 },
            new() { UniqueId = "SIM-1", Name = "SimBot1", LifetimePoints = 5000, IsSimulated = true },
            new() { UniqueId = "USR-2", Name = "Bob", LifetimePoints = 500 },
            new() { UniqueId = "SIM-2", Name = "SimBot2", LifetimePoints = 3000, IsSimulated = true }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = (await _service.GetLeaderboardAsync(10)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
        Assert.DoesNotContain(result, u => u.IsSimulated);
    }

    [Fact]
    public async Task GetLeaderboardEntriesAsync_FallbackReturnsEntriesWithStats()
    {
        // Without DbContext (JSON provider fallback), uses N+1 game count queries
        var users = new List<User>
        {
            new() { UniqueId = "USR-1", Name = "Alice", LifetimePoints = 1000 },
            new() { UniqueId = "USR-2", Name = "Bob", LifetimePoints = 500 }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _gameRecordRepoMock.Setup(r => r.GetCountByUserIdAsync("USR-1")).ReturnsAsync(5);
        _gameRecordRepoMock.Setup(r => r.GetCountByUserIdAsync("USR-2")).ReturnsAsync(3);

        var result = (await _service.GetLeaderboardEntriesAsync(10)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Rank);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(1000, result[0].LifetimePoints);
        Assert.Equal(5, result[0].GamesPlayed);
        Assert.Equal(2, result[1].Rank);
        Assert.Equal("Bob", result[1].Name);
        Assert.Equal(3, result[1].GamesPlayed);
    }

    [Fact]
    public async Task GetLeaderboardEntriesAsync_FallbackExcludesSimulatedUsers()
    {
        var users = new List<User>
        {
            new() { UniqueId = "USR-1", Name = "Alice", LifetimePoints = 1000 },
            new() { UniqueId = "SIM-1", Name = "SimBot", LifetimePoints = 9999, IsSimulated = true },
            new() { UniqueId = "USR-2", Name = "Bob", LifetimePoints = 500 }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _gameRecordRepoMock.Setup(r => r.GetCountByUserIdAsync(It.IsAny<string>())).ReturnsAsync(1);

        var result = (await _service.GetLeaderboardEntriesAsync(10)).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, e => e.Name == "SimBot");
    }

    [Fact]
    public async Task LeaderboardEndpoint_InvalidTop_ReturnsBadRequest()
    {
        var serviceMock = new Mock<IUserService>();
        var roleServiceMock = new Mock<IRoleService>();
        var controller = new UserController(serviceMock.Object, roleServiceMock.Object);

        var result = await controller.GetLeaderboard(0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task LeaderboardEndpoint_ValidTop_ReturnsRankedEntries()
    {
        var serviceMock = new Mock<IUserService>();
        var entries = new List<LeaderboardEntry>
        {
            new() { Rank = 1, Name = "Alice", LifetimePoints = 1000, GamesPlayed = 2, GamesSolved = 1, BestScore = 800, CurrentStreak = 1 },
            new() { Rank = 2, Name = "Bob", LifetimePoints = 500, GamesPlayed = 1, GamesSolved = 1, BestScore = 500, CurrentStreak = 1 }
        };
        serviceMock.Setup(s => s.GetLeaderboardEntriesAsync(10)).ReturnsAsync(entries);
        var roleServiceMock = new Mock<IRoleService>();
        var controller = new UserController(serviceMock.Object, roleServiceMock.Object);

        var result = await controller.GetLeaderboard(10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<LeaderboardEntry>>>(okResult.Value);
        var resultEntries = response.Data!.ToList();
        Assert.Equal(2, resultEntries.Count);
        Assert.Equal(1, resultEntries[0].Rank);
        Assert.Equal("Alice", resultEntries[0].Name);
        Assert.Equal(1000, resultEntries[0].LifetimePoints);
        Assert.Equal(2, resultEntries[0].GamesPlayed);
        Assert.Equal(1, resultEntries[0].GamesSolved);
        Assert.Equal(800, resultEntries[0].BestScore);
        Assert.Equal(2, resultEntries[1].Rank);
    }
}
