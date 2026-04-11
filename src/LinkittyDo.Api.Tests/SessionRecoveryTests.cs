using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace LinkittyDo.Api.Tests;

public class SessionRecoveryTests
{
    private static DbContextOptions<LinkittyDoDbContext> CreateDbOptions(string dbName)
    {
        return new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    private static IServiceProvider CreateServiceProvider(DbContextOptions<LinkittyDoDbContext> options)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new LinkittyDoDbContext(options));
        services.AddScoped<IGameRecordRepository>(_ =>
        {
            var mock = new Mock<IGameRecordRepository>();
            mock.Setup(r => r.GetByGameIdWithEventsAsync(It.IsAny<string>()))
                .ReturnsAsync((string gameId) => new GameRecord
                {
                    GameId = gameId,
                    UserId = "USR-1",
                    PlayedAt = DateTime.UtcNow,
                    PhraseText = "the quick brown fox",
                    Result = GameResult.InProgress,
                    Events = new List<GameEvent>()
                });
            return mock.Object;
        });
        return services.BuildServiceProvider();
    }

    private static DatabaseSessionStore.SessionState CreateTestState()
    {
        return new DatabaseSessionStore.SessionState
        {
            RevealedWords = new Dictionary<int, bool> { { 1, false }, { 2, false }, { 3, true } },
            UsedClueTerms = new Dictionary<int, List<string>> { { 1, new List<string> { "fast" } } },
            UsedClueUrls = new List<string> { "https://example.com" },
            ClueCountPerWord = new Dictionary<int, int> { { 1, 1 } },
            GuessCountPerWord = new Dictionary<int, int> { { 3, 1 } },
            PhraseWords = new List<DatabaseSessionStore.SessionPhraseWord>
            {
                new() { Index = 0, Text = "the", IsHidden = false },
                new() { Index = 1, Text = "quick", IsHidden = true },
                new() { Index = 2, Text = "brown", IsHidden = true },
                new() { Index = 3, Text = "fox", IsHidden = true }
            },
            PhraseFullText = "the quick brown fox",
            PhraseId = 42
        };
    }

    [Fact]
    public async Task LoadSessionsAsync_RecoversSavedSessions()
    {
        var dbName = $"SessionRecovery_{Guid.NewGuid()}";
        var dbOptions = CreateDbOptions(dbName);
        var serviceProvider = CreateServiceProvider(dbOptions);
        var logger = new Mock<ILogger<DatabaseSessionStore>>();
        var store = new DatabaseSessionStore(serviceProvider, logger.Object);

        var sessionId = Guid.NewGuid();
        var state = CreateTestState();
        // Seed data using a separate context
        using (var seedContext = new LinkittyDoDbContext(dbOptions))
        {
            seedContext.GameSessions.Add(new GameSessionRecord
            {
                SessionId = sessionId.ToString(),
                UserId = "USR-1",
                PhraseUniqueId = "PHRASE-001",
                GameRecordId = "GAME-001",
                Score = 100,
                Difficulty = 10,
                StateJson = JsonSerializer.Serialize(state),
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5)
            });
            await seedContext.SaveChangesAsync();
        }

        await store.LoadSessionsAsync(TimeSpan.FromHours(24));

        var recovered = store.Get(sessionId);
        Assert.NotNull(recovered);
        Assert.Equal("USR-1", recovered.UserId);
        Assert.Equal(100, recovered.Score);
        Assert.Equal(10, recovered.Difficulty);
        Assert.Equal("the quick brown fox", recovered.Phrase.FullText);
        Assert.Equal(4, recovered.Phrase.Words.Count);
        Assert.True(recovered.Phrase.Words[1].IsHidden);
        Assert.False(recovered.Phrase.Words[0].IsHidden);
        Assert.True(recovered.RevealedWords.ContainsKey(3));
        Assert.True(recovered.RevealedWords[3]);
        Assert.Single(recovered.UsedClueUrls);
        Assert.NotNull(recovered.GameRecord);
    }

    [Fact]
    public async Task LoadSessionsAsync_RemovesStaleSessions()
    {
        var dbName = $"SessionRecoveryStale_{Guid.NewGuid()}";
        var dbOptions = CreateDbOptions(dbName);
        var serviceProvider = CreateServiceProvider(dbOptions);
        var logger = new Mock<ILogger<DatabaseSessionStore>>();
        var store = new DatabaseSessionStore(serviceProvider, logger.Object);

        var staleSessionId = Guid.NewGuid();
        var state = CreateTestState();
        using (var seedContext = new LinkittyDoDbContext(dbOptions))
        {
            seedContext.GameSessions.Add(new GameSessionRecord
            {
                SessionId = staleSessionId.ToString(),
                UserId = "USR-1",
                PhraseUniqueId = "PHRASE-001",
                Score = 50,
                Difficulty = 10,
                StateJson = JsonSerializer.Serialize(state),
                StartedAt = DateTime.UtcNow.AddHours(-48),
                LastActivityAt = DateTime.UtcNow.AddHours(-48)
            });
            await seedContext.SaveChangesAsync();
        }

        await store.LoadSessionsAsync(TimeSpan.FromHours(24));

        var recovered = store.Get(staleSessionId);
        Assert.Null(recovered);
        Assert.Equal(0, store.Count);

        // Verify it was deleted from DB
        using (var verifyContext = new LinkittyDoDbContext(dbOptions))
        {
            var remaining = await verifyContext.GameSessions.CountAsync();
            Assert.Equal(0, remaining);
        }
    }

    [Fact]
    public async Task LoadSessionsAsync_HandlesCorruptStateJson_Gracefully()
    {
        var dbName = $"SessionRecoveryCorrupt_{Guid.NewGuid()}";
        var dbOptions = CreateDbOptions(dbName);
        var serviceProvider = CreateServiceProvider(dbOptions);
        var logger = new Mock<ILogger<DatabaseSessionStore>>();
        var store = new DatabaseSessionStore(serviceProvider, logger.Object);

        var sessionId = Guid.NewGuid();
        using (var seedContext = new LinkittyDoDbContext(dbOptions))
        {
            seedContext.GameSessions.Add(new GameSessionRecord
            {
                SessionId = sessionId.ToString(),
                UserId = "USR-1",
                PhraseUniqueId = "PHRASE-001",
                Score = 50,
                Difficulty = 10,
                StateJson = "{ invalid json }",
                StartedAt = DateTime.UtcNow.AddMinutes(-10),
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5)
            });
            await seedContext.SaveChangesAsync();
        }

        await store.LoadSessionsAsync(TimeSpan.FromHours(24));

        // Corrupt session should be discarded, not crash
        Assert.Equal(0, store.Count);
    }
}
