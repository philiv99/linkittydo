using LinkittyDo.Api.Controllers;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LinkittyDo.Api.Tests;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        var roleServiceMock = new Mock<IRoleService>();
        _controller = new UserController(_userServiceMock.Object, roleServiceMock.Object);
    }

    private static User CreateTestUser(string id = "USR-1234567890123-ABC123", string name = "TestUser", string email = "test@example.com")
    {
        return new User
        {
            UniqueId = id,
            Name = name,
            Email = email,
            LifetimePoints = 0,
            PreferredDifficulty = 10,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WhenValid()
    {
        var request = new CreateUserRequest { Name = "NewUser", Email = "new@test.com" };
        var user = CreateTestUser(name: "NewUser", email: "new@test.com");

        _userServiceMock.Setup(s => s.IsNameAvailableAsync("NewUser", null)).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.IsEmailAvailableAsync("new@test.com", null)).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.CreateUserAsync(request)).ReturnsAsync(user);

        var result = await _controller.CreateUser(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserResponse>>(createdResult.Value);
        Assert.Equal("NewUser", response.Data!.Name);
        Assert.Equal("new@test.com", response.Data.Email);
    }

    [Fact]
    public async Task CreateUser_ReturnsConflict_WhenNameTaken()
    {
        var request = new CreateUserRequest { Name = "TakenName", Email = "new@test.com" };
        _userServiceMock.Setup(s => s.IsNameAvailableAsync("TakenName", null)).ReturnsAsync(false);

        var result = await _controller.CreateUser(request);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal("NAME_TAKEN", error.Error.Code);
    }

    [Fact]
    public async Task CreateUser_ReturnsConflict_WhenEmailTaken()
    {
        var request = new CreateUserRequest { Name = "NewUser", Email = "taken@test.com" };
        _userServiceMock.Setup(s => s.IsNameAvailableAsync("NewUser", null)).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.IsEmailAvailableAsync("taken@test.com", null)).ReturnsAsync(false);

        var result = await _controller.CreateUser(request);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal("EMAIL_TAKEN", error.Error.Code);
    }

    [Fact]
    public async Task GetUser_ReturnsOk_WhenFound()
    {
        var user = CreateTestUser();
        _userServiceMock.Setup(s => s.GetUserByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);

        var result = await _controller.GetUser("USR-1234567890123-ABC123");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserResponse>>(okResult.Value);
        Assert.Equal("TestUser", response.Data!.Name);
    }

    [Fact]
    public async Task GetUser_ReturnsNotFound_WhenMissing()
    {
        _userServiceMock.Setup(s => s.GetUserByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var result = await _controller.GetUser("nonexistent");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal("USER_NOT_FOUND", error.Error.Code);
    }

    [Fact]
    public async Task GetUserByEmail_ReturnsOk_WhenFound()
    {
        var user = CreateTestUser();
        _userServiceMock.Setup(s => s.GetUserByEmailAsync("test@example.com")).ReturnsAsync(user);

        var result = await _controller.GetUserByEmail("test@example.com");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserResponse>>(okResult.Value);
        Assert.Equal("test@example.com", response.Data!.Email);
    }

    [Fact]
    public async Task GetUserByEmail_ReturnsNotFound_WhenMissing()
    {
        _userServiceMock.Setup(s => s.GetUserByEmailAsync("missing@test.com")).ReturnsAsync((User?)null);

        var result = await _controller.GetUserByEmail("missing@test.com");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOk_WithUsers()
    {
        var users = new List<User> { CreateTestUser(), CreateTestUser(id: "USR-9999999999999-DEF456", name: "User2", email: "u2@test.com") };
        _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

        var result = await _controller.GetAllUsers();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<UserResponse>>>(okResult.Value);
        Assert.Equal(2, response.Data!.Count());
    }

    [Fact]
    public async Task UpdateUser_ReturnsOk_WhenValid()
    {
        var existingUser = CreateTestUser();
        var updatedUser = CreateTestUser(name: "UpdatedName", email: "updated@test.com");
        var request = new UpdateUserRequest { Name = "UpdatedName", Email = "updated@test.com" };

        _userServiceMock.Setup(s => s.GetUserByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(existingUser);
        _userServiceMock.Setup(s => s.IsNameAvailableAsync("UpdatedName", "USR-1234567890123-ABC123")).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.IsEmailAvailableAsync("updated@test.com", "USR-1234567890123-ABC123")).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.UpdateUserAsync("USR-1234567890123-ABC123", request)).ReturnsAsync(updatedUser);

        var result = await _controller.UpdateUser("USR-1234567890123-ABC123", request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserResponse>>(okResult.Value);
        Assert.Equal("UpdatedName", response.Data!.Name);
    }

    [Fact]
    public async Task UpdateUser_ReturnsNotFound_WhenUserMissing()
    {
        var request = new UpdateUserRequest { Name = "Name", Email = "e@test.com" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var result = await _controller.UpdateUser("nonexistent", request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent_WhenDeleted()
    {
        _userServiceMock.Setup(s => s.DeleteUserAsync("USR-1234567890123-ABC123")).ReturnsAsync(true);

        var result = await _controller.DeleteUser("USR-1234567890123-ABC123");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNotFound_WhenMissing()
    {
        _userServiceMock.Setup(s => s.DeleteUserAsync("nonexistent")).ReturnsAsync(false);

        var result = await _controller.DeleteUser("nonexistent");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CheckNameAvailability_ReturnsWrappedResponse()
    {
        _userServiceMock.Setup(s => s.IsNameAvailableAsync("AvailableName", null)).ReturnsAsync(true);

        var result = await _controller.CheckNameAvailability("AvailableName");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AvailabilityResponse>>(okResult.Value);
        Assert.True(response.Data!.Available);
    }

    [Fact]
    public async Task CheckEmailAvailability_ReturnsWrappedResponse()
    {
        _userServiceMock.Setup(s => s.IsEmailAvailableAsync("avail@test.com", null)).ReturnsAsync(false);

        var result = await _controller.CheckEmailAvailability("avail@test.com");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AvailabilityResponse>>(okResult.Value);
        Assert.False(response.Data!.Available);
    }

    [Fact]
    public async Task UpdateDifficulty_ReturnsOk_WhenValid()
    {
        var user = CreateTestUser();
        user.PreferredDifficulty = 50;
        _userServiceMock.Setup(s => s.UpdateDifficultyAsync("USR-1234567890123-ABC123", 50)).ReturnsAsync(user);

        var result = await _controller.UpdateDifficulty("USR-1234567890123-ABC123", new UpdateDifficultyRequest { Difficulty = 50 });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DifficultyResponse>>(okResult.Value);
        Assert.Equal(50, response.Data!.PreferredDifficulty);
    }

    [Fact]
    public async Task UpdateDifficulty_ReturnsNotFound_WhenUserMissing()
    {
        _userServiceMock.Setup(s => s.UpdateDifficultyAsync("nonexistent", 50)).ReturnsAsync((User?)null);

        var result = await _controller.UpdateDifficulty("nonexistent", new UpdateDifficultyRequest { Difficulty = 50 });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPoints_ReturnsOk_WhenValid()
    {
        var user = CreateTestUser();
        user.LifetimePoints = 100;
        _userServiceMock.Setup(s => s.AddPointsAsync("USR-1234567890123-ABC123", 100)).ReturnsAsync(user);

        var result = await _controller.AddPoints("USR-1234567890123-ABC123", new AddPointsRequest { Points = 100 });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PointsResponse>>(okResult.Value);
        Assert.Equal(100, response.Data!.LifetimePoints);
        Assert.Equal(100, response.Data.PointsAdded);
    }

    [Fact]
    public async Task AddPoints_ReturnsNotFound_WhenUserMissing()
    {
        _userServiceMock.Setup(s => s.AddPointsAsync("nonexistent", 100)).ReturnsAsync((User?)null);

        var result = await _controller.AddPoints("nonexistent", new AddPointsRequest { Points = 100 });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetUserGames_ReturnsOk_WithGames()
    {
        var user = CreateTestUser();
        var games = new List<GameRecord>
        {
            new() { GameId = "GAME-123-ABC", PhraseText = "Test phrase", Score = 200 }
        };
        _userServiceMock.Setup(s => s.GetUserByIdAsync("USR-1234567890123-ABC123")).ReturnsAsync(user);
        _userServiceMock.Setup(s => s.GetUserGamesAsync("USR-1234567890123-ABC123")).ReturnsAsync(games);

        var result = await _controller.GetUserGames("USR-1234567890123-ABC123");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<GameRecord>>>(okResult.Value);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task GetUserGames_ReturnsNotFound_WhenUserMissing()
    {
        _userServiceMock.Setup(s => s.GetUserByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var result = await _controller.GetUserGames("nonexistent");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
