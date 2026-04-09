using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class SimulationEngineTests
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

    private static void SeedPhrase(LinkittyDoDbContext context, string uniqueId = "PHR-0000000000001-TEST01", string text = "quick brown fox", int difficulty = 50)
    {
        context.GamePhrases.Add(new GamePhrase
        {
            UniqueId = uniqueId,
            Text = text,
            WordCount = text.Split(' ').Length,
            Difficulty = difficulty,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task SimulateGame_CreatesSimulatedUserAndGame()
    {
        using var context = CreateContext();
        SeedPhrase(context);
        var service = new SimulationService(context);

        var result = await service.SimulateGameAsync(2); // Average profile

        Assert.True(SimulationIdGenerator.IsSimulatedGame(result.GameId));
        Assert.True(SimulationIdGenerator.IsSimulatedUser(result.UserId));
        Assert.True(result.Result == GameResult.Solved || result.Result == GameResult.GaveUp);

        var user = await context.Users.FindAsync(result.UserId);
        Assert.NotNull(user);
        Assert.True(user.IsSimulated);

        var game = await context.GameRecords.FindAsync(result.GameId);
        Assert.NotNull(game);
        Assert.True(game.IsSimulated);
    }

    [Fact]
    public async Task SimulateGame_CreatesEvents()
    {
        using var context = CreateContext();
        SeedPhrase(context);
        var service = new SimulationService(context);

        var result = await service.SimulateGameAsync(2);

        var events = await context.GameEvents
            .Where(e => e.GameId == result.GameId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        Assert.NotEmpty(events);
        Assert.Contains(events, e => e is GameEndEvent);
    }

    [Fact]
    public async Task SimulateGame_WithSpecificPhrase()
    {
        using var context = CreateContext();
        SeedPhrase(context, "PHR-SPECIFIC", "hello world");
        var service = new SimulationService(context);

        var result = await service.SimulateGameAsync(1, "PHR-SPECIFIC");
        var game = await context.GameRecords.FindAsync(result.GameId);
        Assert.Equal("PHR-SPECIFIC", game!.PhraseText);
    }

    [Fact]
    public async Task SimulateGame_ThrowsIfNoProfile()
    {
        using var context = CreateContext();
        SeedPhrase(context);
        var service = new SimulationService(context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SimulateGameAsync(999));
    }

    [Fact]
    public async Task SimulateGame_ThrowsIfNoPhrases()
    {
        using var context = CreateContext();
        // No phrases seeded
        var service = new SimulationService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SimulateGameAsync(2));
    }

    [Fact]
    public async Task RunBatch_CreatesMultipleGames()
    {
        using var context = CreateContext();
        SeedPhrase(context);
        var service = new SimulationService(context);

        var batch = await service.RunBatchAsync(2, 5);

        Assert.Equal(5, batch.TotalGames);
        Assert.Equal(5, batch.Games.Count);
        Assert.Equal(batch.Solved + batch.GaveUp, batch.TotalGames);

        var simGames = await context.GameRecords.Where(g => g.IsSimulated).CountAsync();
        Assert.Equal(5, simGames);
    }

    [Fact]
    public async Task PurgeSimulationData_RemovesOnlySimulated()
    {
        using var context = CreateContext();
        SeedPhrase(context);

        // Add real user and game
        context.Users.Add(new User { UniqueId = "USR-REAL", Name = "RealUser", Email = "real@test.com", CreatedAt = DateTime.UtcNow });
        context.GameRecords.Add(new GameRecord { GameId = "GAME-REAL", UserId = "USR-REAL", PhraseText = "test", PlayedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new SimulationService(context);
        await service.RunBatchAsync(2, 3);

        var purgeResult = await service.PurgeSimulationDataAsync();

        Assert.Equal(3, purgeResult.UsersDeleted);
        Assert.Equal(3, purgeResult.GamesDeleted);
        Assert.True(purgeResult.EventsDeleted > 0);

        // Real data still exists
        Assert.NotNull(await context.Users.FindAsync("USR-REAL"));
        Assert.NotNull(await context.GameRecords.FindAsync("GAME-REAL"));

        // Simulated data is gone
        Assert.Equal(0, await context.Users.CountAsync(u => u.IsSimulated));
        Assert.Equal(0, await context.GameRecords.CountAsync(g => g.IsSimulated));
    }

    [Fact]
    public async Task GetProfiles_ReturnsActiveProfiles()
    {
        using var context = CreateContext();
        var service = new SimulationService(context);

        var profiles = await service.GetProfilesAsync();
        Assert.Equal(3, profiles.Count);
        Assert.All(profiles, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task SimulateGame_ScoredCorrectlyForSolvedGame()
    {
        using var context = CreateContext();
        SeedPhrase(context, text: "one"); // single word = higher chance of solve
        var service = new SimulationService(context);

        // Use Expert profile (high correct guess probability)
        var result = await service.SimulateGameAsync(3);

        if (result.Result == GameResult.Solved)
        {
            Assert.True(result.Score > 0);
        }
    }
}
