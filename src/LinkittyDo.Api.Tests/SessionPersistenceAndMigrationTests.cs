using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class SessionPersistenceAndMigrationTests
{
    private static LinkittyDoDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new LinkittyDoDbContext(options);
    }

    [Fact]
    public void GameSessionRecord_HasCorrectDefaults()
    {
        var record = new GameSessionRecord();
        Assert.Equal(string.Empty, record.SessionId);
        Assert.Null(record.UserId);
        Assert.Equal(string.Empty, record.PhraseUniqueId);
        Assert.Equal(0, record.Score);
        Assert.Equal(0, record.Difficulty);
        Assert.Equal("{}", record.StateJson);
    }

    [Fact]
    public async Task DbContext_GameSessions_CanCreateAndRetrieve()
    {
        using var context = CreateInMemoryContext();
        var sessionId = Guid.NewGuid().ToString();

        context.GameSessions.Add(new GameSessionRecord
        {
            SessionId = sessionId,
            UserId = "USR-1234567890123-A1B2C3",
            PhraseUniqueId = "PHR-1234567890123-D4E5F6",
            Score = 150,
            Difficulty = 50,
            StateJson = "{\"revealedWords\":{}}",
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var retrieved = await context.GameSessions.FindAsync(sessionId);
        Assert.NotNull(retrieved);
        Assert.Equal("USR-1234567890123-A1B2C3", retrieved.UserId);
        Assert.Equal(150, retrieved.Score);
        Assert.Equal(50, retrieved.Difficulty);
    }

    [Fact]
    public async Task DbContext_GameSessions_CanDelete()
    {
        using var context = CreateInMemoryContext();
        var sessionId = Guid.NewGuid().ToString();

        context.GameSessions.Add(new GameSessionRecord
        {
            SessionId = sessionId,
            PhraseUniqueId = "PHR-1234567890123-D4E5F6",
            StateJson = "{}",
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var record = await context.GameSessions.FindAsync(sessionId);
        Assert.NotNull(record);
        context.GameSessions.Remove(record);
        await context.SaveChangesAsync();

        var deleted = await context.GameSessions.FindAsync(sessionId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DbContext_GameSessions_CanUpdateScoreAndState()
    {
        using var context = CreateInMemoryContext();
        var sessionId = Guid.NewGuid().ToString();

        context.GameSessions.Add(new GameSessionRecord
        {
            SessionId = sessionId,
            PhraseUniqueId = "PHR-1234567890123-D4E5F6",
            Score = 0,
            StateJson = "{}",
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var record = await context.GameSessions.FindAsync(sessionId);
        Assert.NotNull(record);
        record.Score = 250;
        record.StateJson = "{\"revealedWords\":{\"0\":true}}";
        await context.SaveChangesAsync();

        var updated = await context.GameSessions.FindAsync(sessionId);
        Assert.Equal(250, updated!.Score);
        Assert.Contains("revealedWords", updated.StateJson);
    }

    [Fact]
    public void InMemorySessionStore_BasicOperations_Work()
    {
        var store = new InMemorySessionStore();
        var sessionId = Guid.NewGuid();
        var session = new GameSession { SessionId = sessionId };

        store.Set(sessionId, session);
        Assert.Equal(1, store.Count);

        var retrieved = store.Get(sessionId);
        Assert.NotNull(retrieved);
        Assert.Equal(sessionId, retrieved.SessionId);

        store.Remove(sessionId);
        Assert.Equal(0, store.Count);
        Assert.Null(store.Get(sessionId));
    }
}
