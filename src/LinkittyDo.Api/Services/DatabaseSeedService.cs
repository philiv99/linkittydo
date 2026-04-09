using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Seeds initial data on application startup.
/// In Development: creates admin user, test user, and sample phrases.
/// In Production: creates admin user only (if not exists).
/// </summary>
public class DatabaseSeedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DatabaseSeedService> _logger;

    public DatabaseSeedService(
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment,
        ILogger<DatabaseSeedService> logger)
    {
        _serviceProvider = serviceProvider;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var phraseRepository = scope.ServiceProvider.GetRequiredService<IGamePhraseRepository>();

        await SeedAdminUser(userRepository);

        if (_environment.IsDevelopment())
        {
            await SeedTestUser(userRepository);
            await SeedSamplePhrases(phraseRepository);
        }

        _logger.LogInformation("Database seeding completed");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAdminUser(IUserRepository userRepository)
    {
        var existing = await userRepository.GetByEmailAsync("admin@linkittydo.com");
        if (existing != null)
        {
            _logger.LogInformation("Admin user already exists, skipping");
            return;
        }

        var admin = new User
        {
            UniqueId = "USR-0000000000000-ADMIN1",
            Name = "Admin",
            Email = "admin@linkittydo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            LifetimePoints = 0,
            PreferredDifficulty = 10,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(admin);
        _logger.LogInformation("Seeded admin user: {Email}", admin.Email);
    }

    private async Task SeedTestUser(IUserRepository userRepository)
    {
        var existing = await userRepository.GetByEmailAsync("test@linkittydo.com");
        if (existing != null)
        {
            _logger.LogInformation("Test user already exists, skipping");
            return;
        }

        var testUser = new User
        {
            UniqueId = "USR-0000000000001-TEST01",
            Name = "TestPlayer",
            Email = "test@linkittydo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            LifetimePoints = 500,
            PreferredDifficulty = 25,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(testUser);
        _logger.LogInformation("Seeded test user: {Email}", testUser.Email);
    }

    private async Task SeedSamplePhrases(IGamePhraseRepository phraseRepository)
    {
        var existingPhrases = (await phraseRepository.GetAllAsync()).ToList();
        if (existingPhrases.Count >= 5)
        {
            _logger.LogInformation("Already have {Count} phrases, skipping seed", existingPhrases.Count);
            return;
        }

        var samplePhrases = new[]
        {
            "the early bird catches the worm",
            "a picture is worth a thousand words",
            "actions speak louder than words",
            "every cloud has a silver lining",
            "practice makes perfect"
        };

        var seeded = 0;
        foreach (var phraseText in samplePhrases)
        {
            var exists = existingPhrases.Any(p =>
                string.Equals(p.Text, phraseText, StringComparison.OrdinalIgnoreCase));
            if (exists) continue;

            var phrase = new GamePhrase
            {
                UniqueId = $"PHRASE-SEED-{seeded + 1:D4}",
                Text = phraseText,
                WordCount = phraseText.Split(' ').Length,
                GeneratedByLlm = false,
                Difficulty = 15 + (seeded * 5),
                CreatedAt = DateTime.UtcNow
            };

            await phraseRepository.CreateAsync(phrase);
            seeded++;
        }

        _logger.LogInformation("Seeded {Count} sample phrases", seeded);
    }
}
