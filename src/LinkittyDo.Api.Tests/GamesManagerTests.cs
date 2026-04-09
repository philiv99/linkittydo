using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class GamesManagerTests
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

    private static void SeedGames(LinkittyDoDbContext context)
    {
        context.GameRecords.AddRange(
            new GameRecord { GameId = "GAME-001", UserId = "USR-001", PhraseText = "PHR-001", Result = GameResult.Solved, Score = 300, PlayedAt = DateTime.UtcNow.AddDays(-2) },
            new GameRecord { GameId = "GAME-002", UserId = "USR-001", PhraseText = "PHR-002", Result = GameResult.GaveUp, Score = 0, PlayedAt = DateTime.UtcNow.AddDays(-1) },
            new GameRecord { GameId = "GAME-003", UserId = "USR-002", PhraseText = "PHR-001", Result = GameResult.Solved, Score = 200, PlayedAt = DateTime.UtcNow },
            new GameRecord { GameId = "SIM-GAME-001", UserId = "SIM-USR-001", PhraseText = "PHR-001", IsSimulated = true, Result = GameResult.Solved, Score = 150, PlayedAt = DateTime.UtcNow }
        );
        context.GameEvents.AddRange(
            new ClueEvent { GameId = "GAME-001", SequenceNumber = 0, WordIndex = 0, SearchTerm = "test", Url = "https://example.com" },
            new GuessEvent { GameId = "GAME-001", SequenceNumber = 1, WordIndex = 0, GuessText = "answer", IsCorrect = true, PointsAwarded = 100 },
            new GameEndEvent { GameId = "GAME-001", SequenceNumber = 2, Reason = "solved" }
        );
        context.SaveChanges();
    }

    [Fact]
    public async Task SearchGames_ReturnsAllGames()
    {
        using var context = CreateContext();
        SeedGames(context);
        var service = new GamesManagerService(context);

        var result = await service.SearchGamesAsync();
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.Games.Count);
    }

    [Fact]
    public async Task SearchGames_FilterByUserId()
    {
        using var context = CreateContext();
        SeedGames(context);
        var service = new GamesManagerService(context);

        var result = await service.SearchGamesAsync(userId: "USR-001");
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Games, g => Assert.Equal("USR-001", g.UserId));
    }

    [Fact]
    public async Task SearchGames_FilterByResult()
    {
        using var context = CreateContext();
        SeedGames(context);
        var service = new GamesManagerService(context);

        var result = await service.SearchGamesAsync(result: GameResult.Solved);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task SearchGames_FilterBySimulated()
    {
        using var context = CreateContext();
        SeedGames(context);
        var service = new GamesManagerService(context);

        var simResult = await service.SearchGamesAsync(isSimulated: true);
        Assert.Single(simResult.Games);

        var realResult = await service.SearchGamesAsync(isSimulated: false);
        Assert.Equal(3, realResult.Games.Count);
    }

    [Fact]
    public async Task SearchGames_Paginates()
    {
        using var context = CreateContext();
        SeedGames(context);
        var service = new GamesManagerService(context);

        var page1 = await service.SearchGamesAsync(page: 1, pageSize: 2);
        Assert.Equal(2, page1.Games.Count);
        Assert.Equal(4, page1.TotalCount);

        var page2 = await service.SearchGamesAsync(page: 2, pageSize: 2);
        Assert.Equal(2, page2.Games.Count);
    }

    [Fact]
    public async Task GetGameDetail_ReturnsGame()
    {
        using var context = CreateContext();
        SeedGames(context);
        var service = new GamesManagerService(context);

        var game = await service.GetGameDetailAsync("GAME-001");
        Assert.NotNull(game);
        Assert.Equal("USR-001", game.UserId);
        Assert.Equal(GameResult.Solved, game.Result);
    }

    [Fact]
    public async Task GetGameDetail_ReturnsNullForMissing()
    {
        using var context = CreateContext();
        var service = new GamesManagerService(context);

        var game = await service.GetGameDetailAsync("NONEXISTENT");
        Assert.Null(game);
    }

    [Fact]
    public async Task GetGameEvents_ReturnsOrderedEvents()
    {
        using var context = CreateContext();
        SeedGames(context);
        var service = new GamesManagerService(context);

        var events = await service.GetGameEventsAsync("GAME-001");
        Assert.Equal(3, events.Count);
        Assert.IsType<ClueEvent>(events[0]);
        Assert.IsType<GuessEvent>(events[1]);
        Assert.IsType<GameEndEvent>(events[2]);
    }

    [Fact]
    public async Task GetPhraseStats_ReturnsNullIfMissing()
    {
        using var context = CreateContext();
        var service = new GamesManagerService(context);

        var stats = await service.GetPhraseStatsAsync("NONEXISTENT");
        Assert.Null(stats);
    }

    [Fact]
    public async Task GetPhraseStats_ReturnsExisting()
    {
        using var context = CreateContext();
        context.PhrasePlayStats.Add(new PhrasePlayStats
        {
            PhraseUniqueId = "PHR-001",
            TimesPlayed = 20,
            SolveRate = 0.75m,
            LastComputedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var service = new GamesManagerService(context);

        var stats = await service.GetPhraseStatsAsync("PHR-001");
        Assert.NotNull(stats);
        Assert.Equal(20, stats.TimesPlayed);
    }
}
