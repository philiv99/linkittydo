using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

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

    [Fact]
    public async Task DataMigration_WithEmptyDirectories_ReturnsZeroCounts()
    {
        using var context = CreateInMemoryContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataDirectory"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
            })
            .Build();
        var logger = Mock.Of<ILogger<JsonToMySqlMigrationService>>();

        var service = new JsonToMySqlMigrationService(context, config, logger);
        var result = await service.MigrateAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.UsersImported);
        Assert.Equal(0, result.PhrasesImported);
        Assert.Equal(0, result.GameRecordsImported);
    }

    [Fact]
    public async Task DataMigration_WithUserFiles_ImportsUsers()
    {
        using var context = CreateInMemoryContext();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var usersDir = Path.Combine(tempDir, "Users");
        Directory.CreateDirectory(usersDir);

        try
        {
            // Create a test user JSON file
            var user = new
            {
                UniqueId = "USR-1234567890123-A1B2C3",
                Name = "TestUser",
                Email = "test@example.com",
                LifetimePoints = 500,
                PreferredDifficulty = 30,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var json = System.Text.Json.JsonSerializer.Serialize(user);
            await File.WriteAllTextAsync(Path.Combine(usersDir, "USR-1234567890123-A1B2C3.json"), json);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DataDirectory"] = tempDir })
                .Build();
            var logger = Mock.Of<ILogger<JsonToMySqlMigrationService>>();

            var service = new JsonToMySqlMigrationService(context, config, logger);
            var result = await service.MigrateAsync();

            Assert.Equal(1, result.UsersImported);
            Assert.Equal(0, result.UsersSkipped);

            // Verify in DB
            var dbUser = await context.Users.FindAsync("USR-1234567890123-A1B2C3");
            Assert.NotNull(dbUser);
            Assert.Equal("TestUser", dbUser.Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DataMigration_IsIdempotent_SkipsDuplicates()
    {
        using var context = CreateInMemoryContext();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var usersDir = Path.Combine(tempDir, "Users");
        Directory.CreateDirectory(usersDir);

        try
        {
            var user = new
            {
                UniqueId = "USR-1234567890123-IDMPOT",
                Name = "DupeUser",
                Email = "dupe@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var json = System.Text.Json.JsonSerializer.Serialize(user);
            await File.WriteAllTextAsync(Path.Combine(usersDir, "USR-1234567890123-IDMPOT.json"), json);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DataDirectory"] = tempDir })
                .Build();
            var logger = Mock.Of<ILogger<JsonToMySqlMigrationService>>();

            var service = new JsonToMySqlMigrationService(context, config, logger);

            // First run imports
            var result1 = await service.MigrateAsync();
            Assert.Equal(1, result1.UsersImported);

            // Second run skips
            var result2 = await service.MigrateAsync();
            Assert.Equal(0, result2.UsersImported);
            Assert.Equal(1, result2.UsersSkipped);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DataMigrationResult_DefaultsToSuccess()
    {
        var result = new DataMigrationResult();
        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal(0, result.UsersImported);
    }

    [Fact]
    public void DataMigrationResult_WithErrors_IsNotSuccess()
    {
        var result = new DataMigrationResult();
        result.Errors.Add("Something went wrong");
        Assert.False(result.Success);
    }
}
