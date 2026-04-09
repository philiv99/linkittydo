using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class DataExplorerTests
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

    private static void SeedData(LinkittyDoDbContext context)
    {
        context.Users.AddRange(
            new User { UniqueId = "USR-001", Name = "Alice", Email = "alice@test.com", CreatedAt = DateTime.UtcNow },
            new User { UniqueId = "SIM-USR-001", Name = "SimBot_Beginner_1234", Email = "sim1@sim.local", IsSimulated = true, CreatedAt = DateTime.UtcNow },
            new User { UniqueId = "SIM-USR-002", Name = "SimBot_Expert_5678", Email = "sim2@sim.local", IsSimulated = true, CreatedAt = DateTime.UtcNow }
        );
        context.GamePhrases.Add(new GamePhrase { UniqueId = "PHR-001", Text = "test phrase one", WordCount = 3, Difficulty = 30, CreatedAt = DateTime.UtcNow });
        context.GameRecords.AddRange(
            new GameRecord { GameId = "GAME-001", UserId = "USR-001", PhraseText = "PHR-001", Result = GameResult.Solved, Score = 300, PlayedAt = DateTime.UtcNow.AddDays(-5) },
            new GameRecord { GameId = "GAME-002", UserId = "USR-001", PhraseText = "PHR-001", Result = GameResult.GaveUp, Score = 0, PlayedAt = DateTime.UtcNow },
            new GameRecord { GameId = "SIM-GAME-001", UserId = "SIM-USR-001", PhraseText = "PHR-001", IsSimulated = true, Result = GameResult.Solved, Score = 200, PlayedAt = DateTime.UtcNow.AddDays(-2) },
            new GameRecord { GameId = "SIM-GAME-002", UserId = "SIM-USR-002", PhraseText = "PHR-001", IsSimulated = true, Result = GameResult.GaveUp, Score = 0, PlayedAt = DateTime.UtcNow.AddDays(-1) }
        );
        context.GameEvents.AddRange(
            new GuessEvent { GameId = "GAME-001", SequenceNumber = 0, WordIndex = 0, GuessText = "test", IsCorrect = true, PointsAwarded = 100 },
            new GameEndEvent { GameId = "GAME-001", SequenceNumber = 1, Reason = "solved" }
        );
        context.PlayerStats.Add(new PlayerStats { UserId = "USR-001", GamesPlayed = 2, GamesSolved = 1, AvgScore = 150m, ComputedAt = DateTime.UtcNow });
        context.SaveChanges();
    }

    [Fact]
    public async Task GetSimulationSummary_ReturnsCorrectCounts()
    {
        using var context = CreateContext();
        SeedData(context);
        var service = new DataExplorerService(context);

        var summary = await service.GetSimulationSummaryAsync();
        Assert.Equal(2, summary.TotalSimUsers);
        Assert.Equal(2, summary.TotalSimGames);
        Assert.Equal(1, summary.SimSolved);
        Assert.Equal(1, summary.SimGaveUp);
        Assert.Equal(0.5, summary.SimSolveRate);
    }

    [Fact]
    public async Task GetSimulationSummary_GroupsByProfile()
    {
        using var context = CreateContext();
        SeedData(context);
        var service = new DataExplorerService(context);

        var summary = await service.GetSimulationSummaryAsync();
        Assert.Contains("Beginner", summary.GamesByProfile.Keys);
        Assert.Contains("Expert", summary.GamesByProfile.Keys);
        Assert.Equal(1, summary.GamesByProfile["Beginner"]);
        Assert.Equal(1, summary.GamesByProfile["Expert"]);
    }

    [Fact]
    public async Task GetPlayerDetail_ReturnsFullDetail()
    {
        using var context = CreateContext();
        SeedData(context);
        var service = new DataExplorerService(context);

        var detail = await service.GetPlayerDetailAsync("USR-001");
        Assert.NotNull(detail);
        Assert.Equal("Alice", detail.User!.Name);
        Assert.NotNull(detail.Stats);
        Assert.Equal(2, detail.Stats.GamesPlayed);
        Assert.Equal(2, detail.RecentGames.Count);
    }

    [Fact]
    public async Task GetPlayerDetail_ReturnsNullForMissing()
    {
        using var context = CreateContext();
        var service = new DataExplorerService(context);

        var detail = await service.GetPlayerDetailAsync("NONEXISTENT");
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetPlayerDetail_ReturnsNullStatsIfNotComputed()
    {
        using var context = CreateContext();
        context.Users.Add(new User { UniqueId = "USR-NEW", Name = "NoStats", Email = "nostats@test.com", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var service = new DataExplorerService(context);

        var detail = await service.GetPlayerDetailAsync("USR-NEW");
        Assert.NotNull(detail);
        Assert.Null(detail.Stats);
        Assert.Empty(detail.RecentGames);
    }

    [Fact]
    public async Task GetDataSummary_ReturnsAllCounts()
    {
        using var context = CreateContext();
        SeedData(context);
        var service = new DataExplorerService(context);

        var summary = await service.GetDataSummaryAsync();
        Assert.Equal(3, summary.TotalUsers);
        Assert.Equal(1, summary.TotalPhrases);
        Assert.Equal(4, summary.TotalGames);
        Assert.Equal(2, summary.TotalEvents);
        Assert.Equal(2, summary.TotalSimUsers);
        Assert.Equal(2, summary.TotalSimGames);
        Assert.True(summary.EstimatedStorageSizeBytes > 0);
    }

    [Fact]
    public async Task GetDataSummary_GameDateRange()
    {
        using var context = CreateContext();
        SeedData(context);
        var service = new DataExplorerService(context);

        var summary = await service.GetDataSummaryAsync();
        Assert.True(summary.OldestGame < summary.NewestGame);
    }

    [Fact]
    public async Task GetDataSummary_EmptyDatabase()
    {
        using var context = CreateContext();
        var service = new DataExplorerService(context);

        var summary = await service.GetDataSummaryAsync();
        Assert.Equal(0, summary.TotalGames);
        Assert.Equal(0, summary.TotalEvents);
        Assert.Equal(DateTime.MinValue, summary.OldestGame);
    }

    [Fact]
    public async Task GetSimulationSummary_EmptyDatabase()
    {
        using var context = CreateContext();
        var service = new DataExplorerService(context);

        var summary = await service.GetSimulationSummaryAsync();
        Assert.Equal(0, summary.TotalSimUsers);
        Assert.Equal(0, summary.TotalSimGames);
        Assert.Equal(0, summary.SimSolveRate);
    }
}
