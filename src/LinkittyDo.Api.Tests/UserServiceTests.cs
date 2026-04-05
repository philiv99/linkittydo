using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Moq;

namespace LinkittyDo.Api.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _service = new UserService(_repoMock.Object);
    }

    private static User CreateTestUser(string id = "USR-1234567890123-ABC123")
    {
        return new User
        {
            UniqueId = id,
            Name = "TestUser",
            Email = "test@example.com",
            LifetimePoints = 0,
            PreferredDifficulty = 10,
            Games = new List<GameRecord>(),
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUser_WhenNameAndEmailAvailable()
    {
        var request = new CreateUserRequest { Name = "NewUser", Email = "new@test.com" };
        _repoMock.Setup(r => r.IsNameAvailableAsync("NewUser", null)).ReturnsAsync(true);
        _repoMock.Setup(r => r.IsEmailAvailableAsync("new@test.com", null)).ReturnsAsync(true);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.CreateUserAsync(request);

        Assert.StartsWith("USR-", result.UniqueId);
        Assert.Equal("NewUser", result.Name);
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal(0, result.LifetimePoints);
        Assert.Equal(10, result.PreferredDifficulty);
    }

    [Fact]
    public async Task CreateUserAsync_TrimsNameAndLowercasesEmail()
    {
        var request = new CreateUserRequest { Name = "  SpacedName  ", Email = "  Upper@Test.COM  " };
        _repoMock.Setup(r => r.IsNameAvailableAsync(It.IsAny<string>(), null)).ReturnsAsync(true);
        _repoMock.Setup(r => r.IsEmailAvailableAsync(It.IsAny<string>(), null)).ReturnsAsync(true);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.CreateUserAsync(request);

        Assert.Equal("SpacedName", result.Name);
        Assert.Equal("upper@test.com", result.Email);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsNameTaken()
    {
        var request = new CreateUserRequest { Name = "Taken", Email = "new@test.com" };
        _repoMock.Setup(r => r.IsNameAvailableAsync("Taken", null)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateUserAsync(request));
        Assert.Equal("NAME_TAKEN", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsEmailTaken()
    {
        var request = new CreateUserRequest { Name = "New", Email = "taken@test.com" };
        _repoMock.Setup(r => r.IsNameAvailableAsync("New", null)).ReturnsAsync(true);
        _repoMock.Setup(r => r.IsEmailAvailableAsync("taken@test.com", null)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateUserAsync(request));
        Assert.Equal("EMAIL_TAKEN", ex.Message);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser()
    {
        var user = CreateTestUser();
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);

        var result = await _service.GetUserByIdAsync("USR-1234567890123-ABC123");

        Assert.NotNull(result);
        Assert.Equal("TestUser", result!.Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var result = await _service.GetUserByIdAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesFields()
    {
        var user = CreateTestUser();
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);
        _repoMock.Setup(r => r.IsNameAvailableAsync("Updated", "USR-1234567890123-ABC123")).ReturnsAsync(true);
        _repoMock.Setup(r => r.IsEmailAvailableAsync("updated@test.com", "USR-1234567890123-ABC123")).ReturnsAsync(true);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.UpdateUserAsync("USR-1234567890123-ABC123", new UpdateUserRequest { Name = "Updated", Email = "updated@test.com" });

        Assert.NotNull(result);
        Assert.Equal("Updated", result!.Name);
        Assert.Equal("updated@test.com", result.Email);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var result = await _service.UpdateUserAsync("nonexistent", new UpdateUserRequest { Name = "n", Email = "e@t.com" });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsTrue_WhenDeleted()
    {
        _repoMock.Setup(r => r.DeleteAsync("USR-1234567890123-ABC123")).ReturnsAsync(true);

        var result = await _service.DeleteUserAsync("USR-1234567890123-ABC123");

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateDifficultyAsync_UpdatesDifficulty()
    {
        var user = CreateTestUser();
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.UpdateDifficultyAsync("USR-1234567890123-ABC123", 75);

        Assert.NotNull(result);
        Assert.Equal(75, result!.PreferredDifficulty);
    }

    [Fact]
    public async Task UpdateDifficultyAsync_ThrowsForInvalidDifficulty()
    {
        var user = CreateTestUser();
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.UpdateDifficultyAsync("USR-1234567890123-ABC123", 150));
    }

    [Fact]
    public async Task AddPointsAsync_AddsPoints()
    {
        var user = CreateTestUser();
        user.LifetimePoints = 500;
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.AddPointsAsync("USR-1234567890123-ABC123", 200);

        Assert.NotNull(result);
        Assert.Equal(700, result!.LifetimePoints);
    }

    [Fact]
    public async Task AddPointsAsync_ThrowsForNegativePoints()
    {
        var user = CreateTestUser();
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.AddPointsAsync("USR-1234567890123-ABC123", -10));
    }

    [Fact]
    public async Task AddGameRecordAsync_AddsRecord()
    {
        var user = CreateTestUser();
        var record = new GameRecord { GameId = "GAME-1-ABC", PhraseText = "test", Score = 100 };
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.AddGameRecordAsync("USR-1234567890123-ABC123", record);

        Assert.NotNull(result);
        Assert.Single(result!.Games);
        Assert.Equal("GAME-1-ABC", result.Games[0].GameId);
    }

    [Fact]
    public async Task GetUserGamesAsync_ReturnsGamesDescending()
    {
        var user = CreateTestUser();
        user.Games = new List<GameRecord>
        {
            new() { GameId = "G1", PlayedAt = DateTime.UtcNow.AddHours(-2) },
            new() { GameId = "G2", PlayedAt = DateTime.UtcNow.AddHours(-1) },
            new() { GameId = "G3", PlayedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);

        var result = (await _service.GetUserGamesAsync("USR-1234567890123-ABC123")).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("G3", result[0].GameId);
        Assert.Equal("G2", result[1].GameId);
        Assert.Equal("G1", result[2].GameId);
    }

    [Fact]
    public async Task GetUserGamesAsync_ReturnsEmpty_WhenUserNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var result = await _service.GetUserGamesAsync("nonexistent");

        Assert.Empty(result);
    }
}
