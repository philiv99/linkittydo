using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class AdminDashboardTests
{
    private static LinkittyDoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LinkittyDoDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedUsers(LinkittyDoDbContext context)
    {
        context.Users.AddRange(
            new User { UniqueId = "USR-001", Name = "Alice", Email = "alice@test.com", CreatedAt = DateTime.UtcNow },
            new User { UniqueId = "USR-002", Name = "Bob", Email = "bob@test.com", CreatedAt = DateTime.UtcNow },
            new User { UniqueId = "SIM-USR-001", Name = "SimBot1", Email = "sim1@simulated.local", IsSimulated = true, CreatedAt = DateTime.UtcNow }
        );
        context.GameRecords.AddRange(
            new GameRecord { GameId = "GAME-001", UserId = "USR-001", PhraseText = "p1", Result = GameResult.Solved, Score = 300, PlayedAt = DateTime.UtcNow },
            new GameRecord { GameId = "GAME-002", UserId = "USR-002", PhraseText = "p1", Result = GameResult.GaveUp, Score = 0, PlayedAt = DateTime.UtcNow },
            new GameRecord { GameId = "SIM-GAME-001", UserId = "SIM-USR-001", PhraseText = "p1", IsSimulated = true, Result = GameResult.Solved, Score = 200, PlayedAt = DateTime.UtcNow }
        );
        context.GamePhrases.Add(new GamePhrase { UniqueId = "PHR-001", Text = "test phrase", WordCount = 2, Difficulty = 30, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();
    }

    [Fact]
    public async Task GetDashboardStats_ReturnsCorrectCounts()
    {
        using var context = CreateContext();
        SeedUsers(context);
        var service = new AdminService(context);

        var stats = await service.GetDashboardStatsAsync();

        Assert.Equal(2, stats.TotalUsers); // excludes simulated
        Assert.Equal(2, stats.TotalGamesPlayed); // excludes simulated
        Assert.Equal(1, stats.TotalGamesSolved);
        Assert.Equal(1, stats.TotalGamesGaveUp);
        Assert.Equal(1, stats.SimulatedUsers);
        Assert.Equal(1, stats.SimulatedGames);
        Assert.Equal(1, stats.TotalPhrases);
    }

    [Fact]
    public async Task GetUsers_ReturnsPagedResults()
    {
        using var context = CreateContext();
        SeedUsers(context);
        var service = new AdminService(context);

        var users = await service.GetUsersAsync(1, 2);
        Assert.Equal(2, users.Count);

        var page2 = await service.GetUsersAsync(2, 2);
        Assert.Equal(1, page2.Count);
    }

    [Fact]
    public async Task GetUsers_FilterBySimulated()
    {
        using var context = CreateContext();
        SeedUsers(context);
        var service = new AdminService(context);

        var simUsers = await service.GetUsersAsync(1, 20, isSimulated: true);
        Assert.Single(simUsers);
        Assert.True(simUsers[0].IsSimulated);

        var realUsers = await service.GetUsersAsync(1, 20, isSimulated: false);
        Assert.Equal(2, realUsers.Count);
    }

    [Fact]
    public async Task GetUserCount_ReturnsCorrectCount()
    {
        using var context = CreateContext();
        SeedUsers(context);
        var service = new AdminService(context);

        Assert.Equal(3, await service.GetUserCountAsync());
        Assert.Equal(1, await service.GetUserCountAsync(true));
        Assert.Equal(2, await service.GetUserCountAsync(false));
    }

    [Fact]
    public async Task SetUserActiveStatus_DeactivatesUser()
    {
        using var context = CreateContext();
        SeedUsers(context);
        var service = new AdminService(context);

        var result = await service.SetUserActiveStatusAsync("USR-001", false);
        Assert.True(result);

        var user = await context.Users.FindAsync("USR-001");
        Assert.False(user!.IsActive);
    }

    [Fact]
    public async Task SetUserActiveStatus_ReturnsFalseForMissing()
    {
        using var context = CreateContext();
        var service = new AdminService(context);

        var result = await service.SetUserActiveStatusAsync("NONEXISTENT", false);
        Assert.False(result);
    }

    [Fact]
    public async Task GetPlayerAnalytics_ReturnsNullIfNoStats()
    {
        using var context = CreateContext();
        var service = new AdminService(context);

        var stats = await service.GetPlayerAnalyticsAsync("USR-001");
        Assert.Null(stats);
    }

    [Fact]
    public async Task GetPlayerAnalytics_ReturnsExistingStats()
    {
        using var context = CreateContext();
        context.PlayerStats.Add(new PlayerStats
        {
            UserId = "USR-001",
            GamesPlayed = 10,
            GamesSolved = 8,
            AvgScore = 250m,
            ComputedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new AdminService(context);

        var stats = await service.GetPlayerAnalyticsAsync("USR-001");
        Assert.NotNull(stats);
        Assert.Equal(10, stats.GamesPlayed);
        Assert.Equal(250m, stats.AvgScore);
    }

    [Fact]
    public async Task DashboardStats_SolveRate()
    {
        using var context = CreateContext();
        SeedUsers(context);
        var service = new AdminService(context);

        var stats = await service.GetDashboardStatsAsync();
        // 1 solved + 1 gave up + 1 sim solved = 3 total, 2 solved / 3 total
        Assert.True(stats.OverallSolveRate > 0);
    }

    [Fact]
    public async Task DashboardStats_EmptyDatabase()
    {
        using var context = CreateContext();
        var service = new AdminService(context);

        var stats = await service.GetDashboardStatsAsync();
        Assert.Equal(0, stats.TotalUsers);
        Assert.Equal(0, stats.TotalGamesPlayed);
        Assert.Equal(0, stats.OverallSolveRate);
    }
}
