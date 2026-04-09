using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class SimulationInfrastructureTests
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

    [Fact]
    public void SimulationIdGenerator_GeneratesSimUserIds()
    {
        var id = SimulationIdGenerator.GenerateSimUserId();
        Assert.StartsWith("SIM-USR-", id);
        Assert.True(SimulationIdGenerator.IsSimulatedUser(id));
        Assert.False(SimulationIdGenerator.IsSimulatedGame(id));
    }

    [Fact]
    public void SimulationIdGenerator_GeneratesSimGameIds()
    {
        var id = SimulationIdGenerator.GenerateSimGameId();
        Assert.StartsWith("SIM-GAME-", id);
        Assert.True(SimulationIdGenerator.IsSimulatedGame(id));
        Assert.False(SimulationIdGenerator.IsSimulatedUser(id));
    }

    [Fact]
    public void SimulationIdGenerator_RegularIdsNotSimulated()
    {
        Assert.False(SimulationIdGenerator.IsSimulatedUser("USR-1234567890123-ABC123"));
        Assert.False(SimulationIdGenerator.IsSimulatedGame("GAME-1234567890123-ABC123"));
    }

    [Fact]
    public async Task SimulationProfile_SeedDataExists()
    {
        using var context = CreateContext();
        var profiles = await context.SimulationProfiles.ToListAsync();
        Assert.Equal(3, profiles.Count);
        Assert.Contains(profiles, p => p.Name == "Beginner");
        Assert.Contains(profiles, p => p.Name == "Average");
        Assert.Contains(profiles, p => p.Name == "Expert");
    }

    [Fact]
    public async Task SimulationProfile_BeginnerHasHighClueProbability()
    {
        using var context = CreateContext();
        var beginner = await context.SimulationProfiles.FirstAsync(p => p.Name == "Beginner");
        Assert.Equal(0.8m, beginner.ClueProbability);
        Assert.Equal(0.3m, beginner.CorrectGuessProbability);
        Assert.Equal(0.2m, beginner.GiveUpProbability);
    }

    [Fact]
    public async Task SimulationProfile_ExpertHasHighCorrectProbability()
    {
        using var context = CreateContext();
        var expert = await context.SimulationProfiles.FirstAsync(p => p.Name == "Expert");
        Assert.Equal(0.9m, expert.CorrectGuessProbability);
        Assert.Equal(0.02m, expert.GiveUpProbability);
        Assert.Equal(80, expert.PreferredDifficulty);
    }

    [Fact]
    public async Task User_IsSimulatedFlag_DefaultsFalse()
    {
        using var context = CreateContext();
        context.Users.Add(new User
        {
            UniqueId = "USR-0000000000001-REAL01",
            Name = "RealPlayer",
            Email = "real@test.com",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var user = await context.Users.FindAsync("USR-0000000000001-REAL01");
        Assert.False(user!.IsSimulated);
    }

    [Fact]
    public async Task User_CanBeMarkedSimulated()
    {
        using var context = CreateContext();
        var simId = SimulationIdGenerator.GenerateSimUserId();
        context.Users.Add(new User
        {
            UniqueId = simId,
            Name = "SimBot_1",
            Email = "sim1@simulated.local",
            IsSimulated = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var user = await context.Users.FindAsync(simId);
        Assert.True(user!.IsSimulated);
        Assert.True(SimulationIdGenerator.IsSimulatedUser(user.UniqueId));
    }

    [Fact]
    public async Task GameRecord_IsSimulatedFlag_DefaultsFalse()
    {
        using var context = CreateContext();
        context.GameRecords.Add(new GameRecord
        {
            GameId = "GAME-0000000000001-REAL01",
            UserId = "USR-TEST",
            PhraseText = "test",
            PlayedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var game = await context.GameRecords.FindAsync("GAME-0000000000001-REAL01");
        Assert.False(game!.IsSimulated);
    }

    [Fact]
    public async Task GameRecord_CanBeMarkedSimulated()
    {
        using var context = CreateContext();
        var simGameId = SimulationIdGenerator.GenerateSimGameId();
        context.GameRecords.Add(new GameRecord
        {
            GameId = simGameId,
            UserId = SimulationIdGenerator.GenerateSimUserId(),
            PhraseText = "simulated phrase",
            IsSimulated = true,
            PlayedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var game = await context.GameRecords.FindAsync(simGameId);
        Assert.True(game!.IsSimulated);
    }
}
