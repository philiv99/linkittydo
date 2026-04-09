using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class EfCoreInfrastructureTests : IDisposable
{
    private readonly LinkittyDoDbContext _context;

    public EfCoreInfrastructureTests()
    {
        var options = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LinkittyDoDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // --- DbContext Schema Tests ---

    [Fact]
    public void DatabaseCreated_Successfully()
    {
        // InMemory database is always accessible
        Assert.NotNull(_context);
    }

    [Fact]
    public async Task Users_Table_CanInsertAndRetrieve()
    {
        var user = new User
        {
            UniqueId = "USR-1234567890123-ABCDEF",
            Name = "TestUser",
            Email = "test@example.com",
            PasswordHash = "hashed",
            LifetimePoints = 100,
            PreferredDifficulty = 25,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Users.FindAsync("USR-1234567890123-ABCDEF");
        Assert.NotNull(retrieved);
        Assert.Equal("TestUser", retrieved.Name);
        Assert.Equal("test@example.com", retrieved.Email);
        Assert.Equal(100, retrieved.LifetimePoints);
    }

    [Fact]
    public async Task Users_NameUniqueness_DetectedByRepository()
    {
        var repo = new EfUserRepository(_context);
        var user1 = new User { UniqueId = "USR-001", Name = "Same", Email = "a@b.com", IsActive = true, CreatedAt = DateTime.UtcNow };
        await repo.CreateAsync(user1);

        Assert.False(await repo.IsNameAvailableAsync("Same"));
        Assert.True(await repo.IsNameAvailableAsync("Different"));
    }

    [Fact]
    public async Task Users_EmailUniqueness_DetectedByRepository()
    {
        var repo = new EfUserRepository(_context);
        var user1 = new User { UniqueId = "USR-001", Name = "User1", Email = "same@email.com", IsActive = true, CreatedAt = DateTime.UtcNow };
        await repo.CreateAsync(user1);

        Assert.False(await repo.IsEmailAvailableAsync("same@email.com"));
        Assert.True(await repo.IsEmailAvailableAsync("other@email.com"));
    }

    [Fact]
    public async Task GamePhrases_Table_CanInsertAndRetrieve()
    {
        var phrase = new GamePhrase
        {
            UniqueId = "PHR-1234567890123-ABCDEF",
            Text = "the quick brown fox",
            WordCount = 4,
            Difficulty = 30,
            GeneratedByLlm = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.GamePhrases.Add(phrase);
        await _context.SaveChangesAsync();

        var retrieved = await _context.GamePhrases.FindAsync("PHR-1234567890123-ABCDEF");
        Assert.NotNull(retrieved);
        Assert.Equal("the quick brown fox", retrieved.Text);
        Assert.Equal(4, retrieved.WordCount);
    }

    [Fact]
    public async Task GameRecords_Table_CanInsertAndRetrieve()
    {
        var record = new GameRecord
        {
            GameId = "GAME-1234567890123-ABCDEF",
            UserId = "USR-001",
            PhraseText = "test phrase",
            PlayedAt = DateTime.UtcNow,
            Score = 200,
            Difficulty = 15,
            Result = GameResult.Solved
        };

        _context.GameRecords.Add(record);
        await _context.SaveChangesAsync();

        var retrieved = await _context.GameRecords.FindAsync("GAME-1234567890123-ABCDEF");
        Assert.NotNull(retrieved);
        Assert.Equal("USR-001", retrieved.UserId);
        Assert.Equal(200, retrieved.Score);
    }

    [Fact]
    public async Task GameEvents_SingleTableInheritance_StoresAllTypes()
    {
        var clueEvent = new ClueEvent
        {
            GameId = "GAME-001",
            SequenceNumber = 0,
            WordIndex = 1,
            SearchTerm = "synonym",
            Url = "https://example.com",
            Timestamp = DateTime.UtcNow
        };

        var guessEvent = new GuessEvent
        {
            GameId = "GAME-001",
            SequenceNumber = 1,
            WordIndex = 1,
            GuessText = "answer",
            IsCorrect = true,
            PointsAwarded = 100,
            Timestamp = DateTime.UtcNow
        };

        var endEvent = new GameEndEvent
        {
            GameId = "GAME-001",
            SequenceNumber = 2,
            Reason = "solved",
            Timestamp = DateTime.UtcNow
        };

        _context.GameEvents.AddRange(clueEvent, guessEvent, endEvent);
        await _context.SaveChangesAsync();

        var events = await _context.GameEvents
            .Where(e => e.GameId == "GAME-001")
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        Assert.Equal(3, events.Count);
        Assert.IsType<ClueEvent>(events[0]);
        Assert.IsType<GuessEvent>(events[1]);
        Assert.IsType<GameEndEvent>(events[2]);

        var clue = (ClueEvent)events[0];
        Assert.Equal("synonym", clue.SearchTerm);
        Assert.Equal("https://example.com", clue.Url);

        var guess = (GuessEvent)events[1];
        Assert.Equal("answer", guess.GuessText);
        Assert.True(guess.IsCorrect);
    }

    // --- EfUserRepository Tests ---

    [Fact]
    public async Task EfUserRepository_CreateAndRetrieve()
    {
        var repo = new EfUserRepository(_context);
        var user = new User
        {
            UniqueId = "USR-TEST-001",
            Name = "EfUser",
            Email = "ef@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await repo.CreateAsync(user);
        Assert.Equal("USR-TEST-001", created.UniqueId);

        var retrieved = await repo.GetByIdAsync("USR-TEST-001");
        Assert.NotNull(retrieved);
        Assert.Equal("EfUser", retrieved.Name);
    }

    [Fact]
    public async Task EfUserRepository_SoftDelete_ExcludesFromQueries()
    {
        var repo = new EfUserRepository(_context);
        var user = new User
        {
            UniqueId = "USR-DEL-001",
            Name = "ToDelete",
            Email = "del@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await repo.CreateAsync(user);
        var deleted = await repo.DeleteAsync("USR-DEL-001");
        Assert.True(deleted);

        var retrieved = await repo.GetByIdAsync("USR-DEL-001");
        Assert.Null(retrieved); // Soft deleted, not returned

        // But still in database
        var raw = await _context.Users.FindAsync("USR-DEL-001");
        Assert.NotNull(raw);
        Assert.False(raw.IsActive);
    }

    [Fact]
    public async Task EfUserRepository_IsNameAvailable()
    {
        var repo = new EfUserRepository(_context);
        var user = new User
        {
            UniqueId = "USR-NAME-001",
            Name = "TakenName",
            Email = "taken@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await repo.CreateAsync(user);

        Assert.False(await repo.IsNameAvailableAsync("TakenName"));
        Assert.True(await repo.IsNameAvailableAsync("TakenName", "USR-NAME-001")); // Excluded
        Assert.True(await repo.IsNameAvailableAsync("FreeName"));
    }

    // --- EfGamePhraseRepository Tests ---

    [Fact]
    public async Task EfGamePhraseRepository_CreateAndRetrieve()
    {
        var repo = new EfGamePhraseRepository(_context);
        var phrase = new GamePhrase
        {
            UniqueId = "PHR-TEST-001",
            Text = "test phrase here",
            WordCount = 3,
            Difficulty = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await repo.CreateAsync(phrase);
        var retrieved = await repo.GetByIdAsync("PHR-TEST-001");
        Assert.NotNull(retrieved);
        Assert.Equal("test phrase here", retrieved.Text);
    }

    [Fact]
    public async Task EfGamePhraseRepository_SoftDelete()
    {
        var repo = new EfGamePhraseRepository(_context);
        var phrase = new GamePhrase
        {
            UniqueId = "PHR-DEL-001",
            Text = "to be deleted",
            WordCount = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await repo.CreateAsync(phrase);
        await repo.DeleteAsync("PHR-DEL-001");

        var retrieved = await repo.GetByIdAsync("PHR-DEL-001");
        Assert.Null(retrieved);

        var count = await repo.GetCountAsync();
        Assert.Equal(0, count);
    }

    // --- EfGameRecordRepository Tests ---

    [Fact]
    public async Task EfGameRecordRepository_CreateAndRetrieveByUserId()
    {
        var repo = new EfGameRecordRepository(_context);
        var record1 = new GameRecord
        {
            GameId = "GAME-REC-001",
            UserId = "USR-001",
            PhraseText = "phrase one",
            PlayedAt = DateTime.UtcNow.AddHours(-1),
            Score = 100,
            Result = GameResult.Solved
        };
        var record2 = new GameRecord
        {
            GameId = "GAME-REC-002",
            UserId = "USR-001",
            PhraseText = "phrase two",
            PlayedAt = DateTime.UtcNow,
            Score = 200,
            Result = GameResult.Solved
        };

        await repo.CreateAsync(record1);
        await repo.CreateAsync(record2);

        var records = (await repo.GetByUserIdAsync("USR-001")).ToList();
        Assert.Equal(2, records.Count);
        Assert.Equal("GAME-REC-002", records[0].GameId); // Most recent first
    }

    [Fact]
    public async Task EfGameRecordRepository_GetByGameId()
    {
        var repo = new EfGameRecordRepository(_context);
        var record = new GameRecord
        {
            GameId = "GAME-FIND-001",
            UserId = "USR-001",
            PhraseText = "find me",
            PlayedAt = DateTime.UtcNow,
            Result = GameResult.InProgress
        };

        await repo.CreateAsync(record);
        var found = await repo.GetByGameIdAsync("GAME-FIND-001");
        Assert.NotNull(found);
        Assert.Equal("find me", found.PhraseText);
    }

    // --- UnitOfWork Tests ---

    [Fact]
    public void EfUnitOfWork_ExposesAllRepositories()
    {
        using var uow = new EfUnitOfWork(_context);
        Assert.NotNull(uow.Users);
        Assert.NotNull(uow.GamePhrases);
        Assert.NotNull(uow.GameRecords);
    }
}
