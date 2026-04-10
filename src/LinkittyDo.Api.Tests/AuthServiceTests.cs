using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LinkittyDo.Api.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockRoleService = new Mock<IRoleService>();
        _mockRoleService.Setup(r => r.GetUserRolesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Key", "TestAuthKey-That-Is-Long-Enough-For-HMAC256-Algo!" },
            { "Jwt:Issuer", "LinkittyDo.Api.Test" },
            { "Jwt:Audience", "LinkittyDo.Web.Test" },
            { "Jwt:ExpirationMinutes", "60" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _authService = new AuthService(_mockRepo.Object, _mockRoleService.Object, _configuration);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsAuthResponse()
    {
        _mockRepo.Setup(r => r.IsNameAvailableAsync(It.IsAny<string>(), null))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.IsEmailAvailableAsync(It.IsAny<string>(), null))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var request = new RegisterRequest
        {
            Name = "TestUser",
            Email = "test@example.com",
            Password = "password123"
        };

        var result = await _authService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal("TestUser", result!.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.StartsWith("USR-", result.UniqueId);
    }

    [Fact]
    public async Task Register_DuplicateName_ReturnsNull()
    {
        _mockRepo.Setup(r => r.IsNameAvailableAsync(It.IsAny<string>(), null))
            .ReturnsAsync(false);

        var request = new RegisterRequest
        {
            Name = "ExistingUser",
            Email = "new@example.com",
            Password = "password123"
        };

        var result = await _authService.RegisterAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsNull()
    {
        _mockRepo.Setup(r => r.IsNameAvailableAsync(It.IsAny<string>(), null))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.IsEmailAvailableAsync(It.IsAny<string>(), null))
            .ReturnsAsync(false);

        var request = new RegisterRequest
        {
            Name = "NewUser",
            Email = "existing@example.com",
            Password = "password123"
        };

        var result = await _authService.RegisterAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UniqueId = "USR-1234567890-ABC123",
            Name = "TestUser",
            Email = "test@example.com",
            PasswordHash = passwordHash
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var result = await _authService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("TestUser", result!.Name);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UniqueId = "USR-1234567890-ABC123",
            Name = "TestUser",
            Email = "test@example.com",
            PasswordHash = passwordHash
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        var result = await _authService.LoginAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_NonexistentUser_ReturnsNull()
    {
        _mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "password123"
        };

        var result = await _authService.LoginAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshToken_Valid_ReturnsNewTokens()
    {
        var user = new User
        {
            UniqueId = "USR-1234567890-ABC123",
            Name = "TestUser",
            Email = "test@example.com",
            PasswordHash = "hash",
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User> { user });
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var result = await _authService.RefreshTokenAsync("valid-refresh-token");

        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
        Assert.NotEqual("valid-refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_Expired_ReturnsNull()
    {
        var user = new User
        {
            UniqueId = "USR-1234567890-ABC123",
            Name = "TestUser",
            Email = "test@example.com",
            RefreshToken = "expired-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User> { user });

        var result = await _authService.RefreshTokenAsync("expired-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task Register_GeneratesValidJwtToken()
    {
        _mockRepo.Setup(r => r.IsNameAvailableAsync(It.IsAny<string>(), null))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.IsEmailAvailableAsync(It.IsAny<string>(), null))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var result = await _authService.RegisterAsync(new RegisterRequest
        {
            Name = "TokenTest",
            Email = "token@test.com",
            Password = "password123"
        });

        Assert.NotNull(result);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result!.AccessToken);

        Assert.Equal("LinkittyDo.Api.Test", token.Issuer);
        Assert.Contains(token.Audiences, a => a == "LinkittyDo.Web.Test");
        Assert.Equal("token@test.com", token.Claims.First(c => c.Type == "email").Value);
        Assert.Equal("TokenTest", token.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public async Task RevokeRefreshToken_ClearsToken()
    {
        var user = new User
        {
            UniqueId = "USR-1234567890-ABC123",
            RefreshToken = "some-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRepo.Setup(r => r.GetByIdAsync("USR-1234567890-ABC123"))
            .ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        await _authService.RevokeRefreshTokenAsync("USR-1234567890-ABC123");

        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task Login_AdminUser_ReturnsRolesInResponse()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UniqueId = "USR-1234567890-ADMIN1",
            Name = "AdminUser",
            Email = "admin@example.com",
            PasswordHash = passwordHash
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("admin@example.com"))
            .ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
        _mockRoleService.Setup(r => r.GetUserRolesAsync("USR-1234567890-ADMIN1"))
            .ReturnsAsync(new List<string> { "Player", "Admin" });

        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = "admin@example.com",
            Password = "password123"
        });

        Assert.NotNull(result);
        Assert.Contains("Admin", result!.Roles);
        Assert.Contains("Player", result.Roles);
    }

    [Fact]
    public async Task Login_RegularUser_ReturnsEmptyRoles()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UniqueId = "USR-1234567890-USER01",
            Name = "RegularUser",
            Email = "user@example.com",
            PasswordHash = passwordHash
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("user@example.com"))
            .ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
        _mockRoleService.Setup(r => r.GetUserRolesAsync("USR-1234567890-USER01"))
            .ReturnsAsync(new List<string>());

        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "password123"
        });

        Assert.NotNull(result);
        Assert.Empty(result!.Roles);
    }

    [Fact]
    public async Task Login_AdminUser_JwtContainsRoleClaims()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UniqueId = "USR-1234567890-ADMIN2",
            Name = "AdminUser2",
            Email = "admin2@example.com",
            PasswordHash = passwordHash
        };

        _mockRepo.Setup(r => r.GetByEmailAsync("admin2@example.com"))
            .ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
        _mockRoleService.Setup(r => r.GetUserRolesAsync("USR-1234567890-ADMIN2"))
            .ReturnsAsync(new List<string> { "Admin" });

        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = "admin2@example.com",
            Password = "password123"
        });

        Assert.NotNull(result);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result!.AccessToken);
        var roleClaims = token.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList();
        Assert.Single(roleClaims);
        Assert.Equal("Admin", roleClaims[0].Value);
    }
}
