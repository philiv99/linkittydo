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

        await SeedAdminUser(scope.ServiceProvider, userRepository);

        if (_environment.IsDevelopment())
        {
            await SeedTestUser(userRepository);
            await SeedSamplePhrases(phraseRepository);
        }

        _logger.LogInformation("Database seeding completed");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAdminUser(IServiceProvider scopedProvider, IUserRepository userRepository)
    {
        var existing = await userRepository.GetByEmailAsync("admin@linkittydo.com");
        if (existing != null)
        {
            _logger.LogInformation("Admin user already exists, skipping");
        }
        else
        {
            var admin = new User
            {
                UniqueId = "USR-0000000000000-ADMIN1",
                Name = "admin",
                Email = "admin@linkittydo.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("tatyung86"),
                LifetimePoints = 0,
                PreferredDifficulty = 10,
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.CreateAsync(admin);
            _logger.LogInformation("Seeded admin user: {Email}", admin.Email);
        }

        // Ensure admin role is assigned
        try
        {
            var roleService = scopedProvider.GetRequiredService<IRoleService>();
            var roles = await roleService.GetUserRolesAsync("USR-0000000000000-ADMIN1");
            if (!roles.Contains("Admin"))
            {
                await roleService.AssignRoleAsync("USR-0000000000000-ADMIN1", "Admin");
                _logger.LogInformation("Assigned Admin role to admin user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not assign Admin role (role service may not be available)");
        }
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
        var existingTexts = new HashSet<string>(
            existingPhrases.Select(p => p.Text.ToLowerInvariant().Trim()));

        var samplePhrases = GetCuratedPhrases();

        if (existingTexts.Count >= samplePhrases.Length)
        {
            _logger.LogInformation("Already have {Count} phrases (>= {Target}), skipping seed",
                existingTexts.Count, samplePhrases.Length);
            return;
        }

        var seeded = 0;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (var phraseText in samplePhrases)
        {
            var normalized = phraseText.ToLowerInvariant().Trim();
            if (existingTexts.Contains(normalized))
                continue;

            var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var phrase = new GamePhrase
            {
                UniqueId = $"PHR-{timestamp + seeded}-{random}",
                Text = phraseText,
                WordCount = phraseText.Split(' ').Length,
                GeneratedByLlm = false,
                Difficulty = GamePhraseService.ComputeDifficultyFromText(phraseText),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await phraseRepository.CreateAsync(phrase);
                existingTexts.Add(normalized);
                seeded++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping duplicate phrase: {Text}", phraseText);
            }
        }

        _logger.LogInformation("Seeded {Count} phrases (total: {Total})",
            seeded, existingTexts.Count);
    }

    private static string[] GetCuratedPhrases()
    {
        return new[]
        {
            // --- Proverbs & Idioms (common) ---
            "the early bird catches the worm",
            "a picture is worth a thousand words",
            "actions speak louder than words",
            "every cloud has a silver lining",
            "practice makes perfect",
            "time will tell",
            "less is more",
            "love is blind",
            "knowledge is power",
            "silence is golden",
            "break the ice",
            "bite the bullet",
            "burn the midnight oil",
            "spill the beans",
            "hit the nail on the head",
            "beat around the bush",
            "cost an arm and a leg",
            "let the cat out of the bag",
            "kill two birds with one stone",
            "curiosity killed the cat",
            "a stitch in time saves nine",
            "the pen is mightier than the sword",
            "a rolling stone gathers no moss",
            "birds of a feather flock together",
            "the grass is always greener on the other side",
            "when in rome do as the romans do",
            "all that glitters is not gold",
            "what goes up must come down",
            "out of sight out of mind",
            "where there is a will there is a way",

            // --- Proverbs & Sayings (less common) ---
            "fortune favors the bold",
            "necessity is the mother of invention",
            "two wrongs do not make a right",
            "the squeaky wheel gets the grease",
            "a penny saved is a penny earned",
            "better late than never",
            "blood is thicker than water",
            "an apple a day keeps the doctor away",
            "the best things in life are free",
            "laughter is the best medicine",
            "home is where the heart is",
            "honesty is the best policy",
            "haste makes waste",
            "patience is a virtue",
            "beauty is in the eye of the beholder",
            "the road to success is always under construction",
            "still waters run deep",
            "too many cooks spoil the broth",
            "every dog has its day",
            "measure twice cut once",

            // --- Longer expressions ---
            "you can lead a horse to water but you cannot make it drink",
            "do not count your chickens before they hatch",
            "do not put all your eggs in one basket",
            "the journey of a thousand miles begins with a single step",
            "people who live in glass houses should not throw stones",
            "a watched pot never boils",
            "do not judge a book by its cover",
            "if the shoe fits wear it",
            "the apple does not fall far from the tree",
            "an ounce of prevention is worth a pound of cure",

            // --- Nature & Animals ---
            "barking dogs seldom bite",
            "let sleeping dogs lie",
            "the leopard cannot change its spots",
            "when the cat is away the mice will play",
            "a bird in the hand is worth two in the bush",
            "you cannot teach an old dog new tricks",
            "the early worm gets eaten",
            "slow and steady wins the race",
            "every rose has its thorn",
            "mighty oaks from little acorns grow",

            // --- Life wisdom ---
            "there is no place like home",
            "give credit where credit is due",
            "a friend in need is a friend indeed",
            "good things come to those who wait",
            "nothing ventured nothing gained",
            "seeing is believing",
            "time heals all wounds",
            "easy come easy go",
            "absence makes the heart grow fonder",
            "the truth will set you free",
            "strike while the iron is hot",
            "a chain is only as strong as its weakest link",
            "jack of all trades master of none",
            "look before you leap",
            "no pain no gain",

            // --- Success & Effort ---
            "rome was not built in a day",
            "the harder you work the luckier you get",
            "where there is smoke there is fire",
            "one man's trash is another man's treasure",
            "you reap what you sow",
            "the best revenge is living well",
            "actions define character more than words",
            "great minds think alike",
            "two heads are better than one",
            "keep your friends close and your enemies closer",

            // --- Wit & Humor ---
            "if at first you do not succeed try again",
            "do not bite the hand that feeds you",
            "the proof is in the pudding",
            "a penny for your thoughts",
            "beggars cannot be choosers",
            "ignorance is bliss",
            "variety is the spice of life",
            "old habits die hard",
            "better safe than sorry",
            "you cannot have your cake and eat it too",

            // --- Challenging phrases (difficult vocabulary) ---
            "discretion is the better part of valor",
            "procrastination is the thief of time",
            "adversity introduces a man to himself",
            "familiarity breeds contempt",
            "perseverance conquers all obstacles",
            "eloquence persuades where logic fails",
            "ambition without direction leads nowhere",
            "experience is the harshest teacher",
            "moderation in all things brings balance",
            "opportunity seldom knocks twice"
        };
    }
}
