using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class AnalyticsServiceTests
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

    private static GameRecord CreateGameRecord(string userId, string phraseText, GameResult result, int score, DateTime playedAt, List<GameEvent>? events = null)
    {
        var gameId = $"GAME-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        return new GameRecord
        {
            GameId = gameId,
            UserId = userId,
            PhraseText = phraseText,
            PhraseId = 1,
            Difficulty = 10,
            Result = result,
            Score = score,
            PlayedAt = playedAt,
            CompletedAt = playedAt.AddMinutes(5),
            Events = events ?? new()
        };
    }

    [Fact]
    public async Task RecomputePlayerStats_CreatesNewStats()
    {
        using var context = CreateContext();
        var userId = "USR-0000000000001-TEST01";
        context.GameRecords.AddRange(
            CreateGameRecord(userId, "phrase1", GameResult.Solved, 300, DateTime.UtcNow.AddDays(-2)),
            CreateGameRecord(userId, "phrase2", GameResult.Solved, 200, DateTime.UtcNow.AddDays(-1)),
            CreateGameRecord(userId, "phrase3", GameResult.GaveUp, 0, DateTime.UtcNow)
        );
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        await service.RecomputePlayerStatsAsync(userId);

        var stats = await context.PlayerStats.FindAsync(userId);
        Assert.NotNull(stats);
        Assert.Equal(3, stats.GamesPlayed);
        Assert.Equal(2, stats.GamesSolved);
        Assert.Equal(1, stats.GamesGaveUp);
        Assert.Equal(0, stats.CurrentStreak); // last game was GaveUp
    }

    [Fact]
    public async Task RecomputePlayerStats_UpdatesExisting()
    {
        using var context = CreateContext();
        var userId = "USR-0000000000002-TEST02";
        context.PlayerStats.Add(new PlayerStats { UserId = userId, GamesPlayed = 1, ComputedAt = DateTime.UtcNow.AddDays(-1) });
        context.GameRecords.Add(CreateGameRecord(userId, "phrase1", GameResult.Solved, 400, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        await service.RecomputePlayerStatsAsync(userId);

        var stats = await context.PlayerStats.FindAsync(userId);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.GamesPlayed);
        Assert.Equal(400, stats.BestScore);
    }

    [Fact]
    public async Task RecomputePlayerStats_ComputesStreak()
    {
        using var context = CreateContext();
        var userId = "USR-0000000000003-TEST03";
        context.GameRecords.AddRange(
            CreateGameRecord(userId, "p1", GameResult.GaveUp, 0, DateTime.UtcNow.AddDays(-4)),
            CreateGameRecord(userId, "p2", GameResult.Solved, 100, DateTime.UtcNow.AddDays(-3)),
            CreateGameRecord(userId, "p3", GameResult.Solved, 200, DateTime.UtcNow.AddDays(-2)),
            CreateGameRecord(userId, "p4", GameResult.Solved, 300, DateTime.UtcNow.AddDays(-1))
        );
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        await service.RecomputePlayerStatsAsync(userId);

        var stats = await context.PlayerStats.FindAsync(userId);
        Assert.Equal(3, stats!.CurrentStreak);
    }

    [Fact]
    public async Task RecomputePhrasePlayStats_ComputesRates()
    {
        using var context = CreateContext();
        var phrase = "test phrase";
        context.GameRecords.AddRange(
            CreateGameRecord("u1", phrase, GameResult.Solved, 100, DateTime.UtcNow.AddDays(-3)),
            CreateGameRecord("u2", phrase, GameResult.Solved, 200, DateTime.UtcNow.AddDays(-2)),
            CreateGameRecord("u3", phrase, GameResult.Solved, 150, DateTime.UtcNow.AddDays(-1)),
            CreateGameRecord("u4", phrase, GameResult.GaveUp, 0, DateTime.UtcNow.AddHours(-2)),
            CreateGameRecord("u5", phrase, GameResult.GaveUp, 0, DateTime.UtcNow.AddHours(-1))
        );
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        await service.RecomputePhrasePlayStatsAsync(phrase);

        var stats = await context.PhrasePlayStats.FindAsync(phrase);
        Assert.NotNull(stats);
        Assert.Equal(5, stats.TimesPlayed);
        Assert.Equal(3, stats.TimesSolved);
        Assert.Equal(2, stats.TimesGaveUp);
        Assert.Equal(0.6m, stats.SolveRate);
        Assert.Equal(0.4m, stats.GiveUpRate);
        Assert.NotNull(stats.CalibratedDifficulty); // 5+ games triggers calibration
        Assert.Equal(40, stats.CalibratedDifficulty); // (1 - 3/5) * 100 = 40
    }

    [Fact]
    public async Task RecomputePhrasePlayStats_NoCalibrateUnder5Games()
    {
        using var context = CreateContext();
        var phrase = "short phrase";
        context.GameRecords.AddRange(
            CreateGameRecord("u1", phrase, GameResult.Solved, 100, DateTime.UtcNow.AddDays(-1)),
            CreateGameRecord("u2", phrase, GameResult.GaveUp, 0, DateTime.UtcNow)
        );
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        await service.RecomputePhrasePlayStatsAsync(phrase);

        var stats = await context.PhrasePlayStats.FindAsync(phrase);
        Assert.NotNull(stats);
        Assert.Null(stats.CalibratedDifficulty);
    }

    [Fact]
    public async Task GetPlayerStats_ReturnsNullIfMissing()
    {
        using var context = CreateContext();
        var service = new AnalyticsService(context);
        var result = await service.GetPlayerStatsAsync("USR-NONEXISTENT");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPhrasePlayStats_ReturnsExistingStats()
    {
        using var context = CreateContext();
        context.PhrasePlayStats.Add(new PhrasePlayStats
        {
            PhraseUniqueId = "phrase-123",
            TimesPlayed = 10,
            SolveRate = 0.8m,
            LastComputedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        var stats = await service.GetPhrasePlayStatsAsync("phrase-123");
        Assert.NotNull(stats);
        Assert.Equal(10, stats.TimesPlayed);
    }

    [Fact]
    public async Task GetTopClues_ReturnsOrderedResults()
    {
        using var context = CreateContext();
        context.ClueEffectiveness.AddRange(
            new ClueEffectiveness { TargetWord = "freedom", SearchTerm = "liberty", UrlDomain = "a.com", TimesShown = 10, TimesLedToCorrectGuess = 3, LastComputedAt = DateTime.UtcNow },
            new ClueEffectiveness { TargetWord = "freedom", SearchTerm = "independence", UrlDomain = "b.com", TimesShown = 10, TimesLedToCorrectGuess = 8, LastComputedAt = DateTime.UtcNow },
            new ClueEffectiveness { TargetWord = "freedom", SearchTerm = "autonomy", UrlDomain = "c.com", TimesShown = 10, TimesLedToCorrectGuess = 5, LastComputedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        var top = await service.GetTopCluesAsync("freedom", 2);
        Assert.Equal(2, top.Count);
        Assert.Equal("independence", top[0].SearchTerm);
        Assert.Equal("autonomy", top[1].SearchTerm);
    }

    [Fact]
    public async Task NoOpAnalyticsService_ReturnsDefaults()
    {
        var service = new NoOpAnalyticsService();
        await service.RecomputePlayerStatsAsync("test");
        await service.RecomputePhrasePlayStatsAsync("test");
        await service.RecomputeClueEffectivenessAsync();
        Assert.Null(await service.GetPlayerStatsAsync("test"));
        Assert.Null(await service.GetPhrasePlayStatsAsync("test"));
        Assert.Empty(await service.GetTopCluesAsync("test"));
    }
}
