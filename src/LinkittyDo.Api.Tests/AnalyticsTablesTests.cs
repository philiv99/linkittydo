using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class AnalyticsTablesTests
{
    private static LinkittyDoDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LinkittyDoDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ClueEffectiveness_CanCreateAndQuery()
    {
        using var context = CreateInMemoryContext();
        context.ClueEffectiveness.Add(new ClueEffectiveness
        {
            TargetWord = "freedom",
            SearchTerm = "liberty",
            UrlDomain = "wikipedia.org",
            TimesShown = 10,
            TimesLedToCorrectGuess = 7,
            AvgGuessesAfterClue = 1.5m,
            LastComputedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var entry = await context.ClueEffectiveness
            .FirstOrDefaultAsync(c => c.TargetWord == "freedom");
        Assert.NotNull(entry);
        Assert.Equal("liberty", entry.SearchTerm);
        Assert.Equal(10, entry.TimesShown);
        Assert.Equal(7, entry.TimesLedToCorrectGuess);
    }

    [Fact]
    public async Task PlayerStats_CanCreateAndUpdate()
    {
        using var context = CreateInMemoryContext();
        context.PlayerStats.Add(new PlayerStats
        {
            UserId = "USR-1234567890123-STATS1",
            GamesPlayed = 20,
            GamesSolved = 15,
            GamesGaveUp = 5,
            AvgScore = 185.5m,
            BestScore = 400,
            CurrentStreak = 3,
            BestStreak = 7,
            LastPlayedAt = DateTime.UtcNow,
            ComputedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var stats = await context.PlayerStats.FindAsync("USR-1234567890123-STATS1");
        Assert.NotNull(stats);
        Assert.Equal(20, stats.GamesPlayed);
        Assert.Equal(185.5m, stats.AvgScore);

        stats.GamesPlayed = 21;
        stats.GamesSolved = 16;
        stats.CurrentStreak = 4;
        await context.SaveChangesAsync();

        var updated = await context.PlayerStats.FindAsync("USR-1234567890123-STATS1");
        Assert.Equal(21, updated!.GamesPlayed);
        Assert.Equal(4, updated.CurrentStreak);
    }

    [Fact]
    public async Task PhrasePlayStats_CanCreateAndQuery()
    {
        using var context = CreateInMemoryContext();
        context.PhrasePlayStats.Add(new PhrasePlayStats
        {
            PhraseUniqueId = "PHR-1234567890123-PSTAT1",
            TimesPlayed = 50,
            TimesSolved = 35,
            TimesGaveUp = 15,
            SolveRate = 0.7m,
            AvgCluesToSolve = 2.3m,
            AvgTimeToSolveSeconds = 120.5m,
            GiveUpRate = 0.3m,
            CalibratedDifficulty = 45,
            LastComputedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var stats = await context.PhrasePlayStats.FindAsync("PHR-1234567890123-PSTAT1");
        Assert.NotNull(stats);
        Assert.Equal(50, stats.TimesPlayed);
        Assert.Equal(0.7m, stats.SolveRate);
        Assert.Equal(45, stats.CalibratedDifficulty);
    }

    [Fact]
    public void ClueEffectiveness_Model_HasCorrectDefaults()
    {
        var entry = new ClueEffectiveness();
        Assert.Equal(0, entry.TimesShown);
        Assert.Equal(0, entry.TimesLedToCorrectGuess);
        Assert.Null(entry.AvgGuessesAfterClue);
    }

    [Fact]
    public void PlayerStats_Model_HasCorrectDefaults()
    {
        var stats = new PlayerStats();
        Assert.Equal(0, stats.GamesPlayed);
        Assert.Equal(0, stats.GamesSolved);
        Assert.Equal(0, stats.BestScore);
        Assert.Equal(0, stats.CurrentStreak);
    }

    [Fact]
    public void PhrasePlayStats_Model_HasCorrectDefaults()
    {
        var stats = new PhrasePlayStats();
        Assert.Equal(0, stats.TimesPlayed);
        Assert.Equal(0m, stats.SolveRate);
        Assert.Null(stats.CalibratedDifficulty);
    }

    [Fact]
    public async Task PlayerStats_MultiplePlayersCanCoexist()
    {
        using var context = CreateInMemoryContext();
        context.PlayerStats.AddRange(
            new PlayerStats { UserId = "USR-1111111111111-PLYR01", GamesPlayed = 10, ComputedAt = DateTime.UtcNow },
            new PlayerStats { UserId = "USR-2222222222222-PLYR02", GamesPlayed = 20, ComputedAt = DateTime.UtcNow },
            new PlayerStats { UserId = "USR-3333333333333-PLYR03", GamesPlayed = 30, ComputedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var allStats = await context.PlayerStats.OrderByDescending(s => s.GamesPlayed).ToListAsync();
        Assert.Equal(3, allStats.Count);
        Assert.Equal(30, allStats.First().GamesPlayed);
    }

    [Fact]
    public async Task ClueEffectiveness_CanComputeSuccessRate()
    {
        using var context = CreateInMemoryContext();
        context.ClueEffectiveness.Add(new ClueEffectiveness
        {
            TargetWord = "bright",
            SearchTerm = "luminous",
            UrlDomain = "example.com",
            TimesShown = 20,
            TimesLedToCorrectGuess = 16,
            LastComputedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var entry = await context.ClueEffectiveness.FirstAsync();
        var successRate = entry.TimesShown > 0 ? (double)entry.TimesLedToCorrectGuess / entry.TimesShown : 0;
        Assert.Equal(0.8, successRate, 2);
    }
}
