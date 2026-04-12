using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace LinkittyDo.Api.Tests;

public class DatabaseSeedServiceTests
{
    [Fact]
    public async Task SeedService_CreatesAdminUser_WhenNotExists()
    {
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.GetByEmailAsync("admin@linkittydo.com")).ReturnsAsync((User?)null);
        userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var phraseRepoMock = new Mock<IGamePhraseRepository>();
        phraseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<GamePhrase>());
        phraseRepoMock.Setup(r => r.CreateAsync(It.IsAny<GamePhrase>())).ReturnsAsync((GamePhrase p) => p);

        var serviceProvider = BuildServiceProvider(userRepoMock.Object, phraseRepoMock.Object);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Production");

        var seedService = new DatabaseSeedService(
            serviceProvider,
            envMock.Object,
            Mock.Of<ILogger<DatabaseSeedService>>());

        await seedService.StartAsync(CancellationToken.None);

        userRepoMock.Verify(r => r.CreateAsync(It.Is<User>(u =>
            u.Email == "admin@linkittydo.com" && u.Name == "admin")), Times.Once);
    }

    [Fact]
    public async Task SeedService_SkipsAdminUser_WhenAlreadyExists()
    {
        var existingAdmin = new User
        {
            UniqueId = "USR-0000000000000-ADMIN1",
            Name = "Admin",
            Email = "admin@linkittydo.com"
        };

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.GetByEmailAsync("admin@linkittydo.com")).ReturnsAsync(existingAdmin);

        var phraseRepoMock = new Mock<IGamePhraseRepository>();

        var serviceProvider = BuildServiceProvider(userRepoMock.Object, phraseRepoMock.Object);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Production");

        var seedService = new DatabaseSeedService(
            serviceProvider,
            envMock.Object,
            Mock.Of<ILogger<DatabaseSeedService>>());

        await seedService.StartAsync(CancellationToken.None);

        userRepoMock.Verify(r => r.CreateAsync(It.Is<User>(u =>
            u.Email == "admin@linkittydo.com")), Times.Never);
    }

    [Fact]
    public async Task SeedService_SeedsPhrases_InDevelopment()
    {
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var phraseRepoMock = new Mock<IGamePhraseRepository>();
        phraseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<GamePhrase>());
        phraseRepoMock.Setup(r => r.CreateAsync(It.IsAny<GamePhrase>())).ReturnsAsync((GamePhrase p) => p);

        var serviceProvider = BuildServiceProvider(userRepoMock.Object, phraseRepoMock.Object);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var seedService = new DatabaseSeedService(
            serviceProvider,
            envMock.Object,
            Mock.Of<ILogger<DatabaseSeedService>>());

        await seedService.StartAsync(CancellationToken.None);

        phraseRepoMock.Verify(r => r.CreateAsync(It.IsAny<GamePhrase>()), Times.AtLeast(100));
    }

    [Fact]
    public async Task SeedService_SkipsPhrases_InProduction()
    {
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var phraseRepoMock = new Mock<IGamePhraseRepository>();

        var serviceProvider = BuildServiceProvider(userRepoMock.Object, phraseRepoMock.Object);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Production");

        var seedService = new DatabaseSeedService(
            serviceProvider,
            envMock.Object,
            Mock.Of<ILogger<DatabaseSeedService>>());

        await seedService.StartAsync(CancellationToken.None);

        phraseRepoMock.Verify(r => r.CreateAsync(It.IsAny<GamePhrase>()), Times.Never);
    }

    [Fact]
    public async Task SeedService_SkipsPhrases_WhenEnoughExist()
    {
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var existingPhrases = Enumerable.Range(1, 120).Select(i => new GamePhrase
        {
            UniqueId = $"P-{i}",
            Text = $"phrase {i}",
            WordCount = 2
        }).ToList();

        var phraseRepoMock = new Mock<IGamePhraseRepository>();
        phraseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existingPhrases);

        var serviceProvider = BuildServiceProvider(userRepoMock.Object, phraseRepoMock.Object);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var seedService = new DatabaseSeedService(
            serviceProvider,
            envMock.Object,
            Mock.Of<ILogger<DatabaseSeedService>>());

        await seedService.StartAsync(CancellationToken.None);

        phraseRepoMock.Verify(r => r.CreateAsync(It.IsAny<GamePhrase>()), Times.Never);
    }

    private static IServiceProvider BuildServiceProvider(
        IUserRepository userRepository,
        IGamePhraseRepository phraseRepository)
    {
        var roleServiceMock = new Mock<IRoleService>();
        roleServiceMock.Setup(r => r.GetUserRolesAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());
        roleServiceMock.Setup(r => r.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddScoped(_ => userRepository);
        services.AddScoped(_ => phraseRepository);
        services.AddScoped(_ => roleServiceMock.Object);
        return services.BuildServiceProvider();
    }
}
