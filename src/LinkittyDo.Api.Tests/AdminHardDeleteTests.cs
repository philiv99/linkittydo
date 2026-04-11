using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LinkittyDo.Api.Tests;

public class AdminHardDeleteTests
{
    private static LinkittyDoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new LinkittyDoDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedFullUserData(LinkittyDoDbContext context)
    {
        // User
        context.Users.Add(new User
        {
            UniqueId = "USR-001",
            Name = "DeleteMe",
            Email = "delete@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        // Another user (should NOT be affected)
        context.Users.Add(new User
        {
            UniqueId = "USR-002",
            Name = "KeepMe",
            Email = "keep@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        // GameRecords for target user
        context.GameRecords.AddRange(
            new GameRecord { GameId = "GAME-001", UserId = "USR-001", PhraseText = "test phrase", Result = GameResult.Solved, Score = 300, PlayedAt = DateTime.UtcNow },
            new GameRecord { GameId = "GAME-002", UserId = "USR-001", PhraseText = "another phrase", Result = GameResult.GaveUp, Score = 0, PlayedAt = DateTime.UtcNow }
        );

        // GameRecord for other user
        context.GameRecords.Add(new GameRecord { GameId = "GAME-003", UserId = "USR-002", PhraseText = "other phrase", Result = GameResult.Solved, Score = 200, PlayedAt = DateTime.UtcNow });

        // GameEvents for target user's games
        context.GameEvents.AddRange(
            new ClueEvent { GameId = "GAME-001", SequenceNumber = 0, WordIndex = 0, SearchTerm = "syn1", Url = "http://example.com" },
            new GuessEvent { GameId = "GAME-001", SequenceNumber = 1, WordIndex = 0, GuessText = "word", IsCorrect = true, PointsAwarded = 100 },
            new GuessEvent { GameId = "GAME-002", SequenceNumber = 0, WordIndex = 0, GuessText = "wrong", IsCorrect = false, PointsAwarded = 0 }
        );

        // GameEvent for other user's game
        context.GameEvents.Add(new ClueEvent { GameId = "GAME-003", SequenceNumber = 0, WordIndex = 0, SearchTerm = "other", Url = "http://other.com" });

        // GameSession for target user
        context.GameSessions.Add(new GameSessionRecord
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = "USR-001",
            PhraseUniqueId = "PHR-001",
            StateJson = "{}",
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        });

        // UserRoles for target user
        context.UserRoles.Add(new UserRole { UserId = "USR-001", RoleId = 1, AssignedAt = DateTime.UtcNow });

        // PlayerStats for target user
        context.PlayerStats.Add(new PlayerStats
        {
            UserId = "USR-001",
            GamesPlayed = 2,
            GamesSolved = 1,
            AvgScore = 150m,
            ComputedAt = DateTime.UtcNow
        });

        // AuditLog entries for target user
        context.AuditLog.Add(new AuditLogEntry
        {
            UserId = "USR-001",
            Action = "UserCreated",
            EntityType = "User",
            EntityId = "USR-001",
            Timestamp = DateTime.UtcNow
        });

        context.SaveChanges();
    }

    [Fact]
    public async Task HardDeleteUser_RemovesAllRelatedData()
    {
        using var context = CreateContext();
        SeedFullUserData(context);
        var service = new AdminService(context);

        var result = await service.HardDeleteUserAsync("USR-001");

        Assert.True(result);

        // User is gone
        Assert.Null(await context.Users.FindAsync("USR-001"));

        // GameRecords are gone
        Assert.Empty(await context.GameRecords.Where(g => g.UserId == "USR-001").ToListAsync());

        // GameEvents for target user's games are gone
        Assert.Empty(await context.GameEvents.Where(e => e.GameId == "GAME-001" || e.GameId == "GAME-002").ToListAsync());

        // GameSessions are gone
        Assert.Empty(await context.GameSessions.Where(s => s.UserId == "USR-001").ToListAsync());

        // UserRoles are gone
        Assert.Empty(await context.UserRoles.Where(ur => ur.UserId == "USR-001").ToListAsync());

        // PlayerStats are gone
        Assert.Null(await context.PlayerStats.FindAsync("USR-001"));

        // AuditLog entries are gone
        Assert.Empty(await context.AuditLog.Where(a => a.UserId == "USR-001").ToListAsync());
    }

    [Fact]
    public async Task HardDeleteUser_DoesNotAffectOtherUsers()
    {
        using var context = CreateContext();
        SeedFullUserData(context);
        var service = new AdminService(context);

        await service.HardDeleteUserAsync("USR-001");

        // Other user still exists
        Assert.NotNull(await context.Users.FindAsync("USR-002"));

        // Other user's game records still exist
        Assert.Single(await context.GameRecords.Where(g => g.UserId == "USR-002").ToListAsync());

        // Other user's game events still exist
        Assert.Single(await context.GameEvents.Where(e => e.GameId == "GAME-003").ToListAsync());
    }

    [Fact]
    public async Task HardDeleteUser_ReturnsFalseForNonexistentUser()
    {
        using var context = CreateContext();
        var service = new AdminService(context);

        var result = await service.HardDeleteUserAsync("USR-NONEXISTENT");

        Assert.False(result);
    }

    [Fact]
    public async Task HardDeleteUser_SucceedsWithNoRelatedData()
    {
        using var context = CreateContext();
        context.Users.Add(new User
        {
            UniqueId = "USR-LONELY",
            Name = "LonelyUser",
            Email = "lonely@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();
        var service = new AdminService(context);

        var result = await service.HardDeleteUserAsync("USR-LONELY");

        Assert.True(result);
        Assert.Null(await context.Users.FindAsync("USR-LONELY"));
    }
}
