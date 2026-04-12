using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Data;

public class LinkittyDoDbContext : DbContext
{
    public LinkittyDoDbContext(DbContextOptions<LinkittyDoDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<GamePhrase> GamePhrases => Set<GamePhrase>();
    public DbSet<GameRecord> GameRecords => Set<GameRecord>();
    public DbSet<GameEvent> GameEvents => Set<GameEvent>();
    public DbSet<GameSessionRecord> GameSessions => Set<GameSessionRecord>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<SiteConfig> SiteConfigs => Set<SiteConfig>();
    public DbSet<PhraseCategory> PhraseCategories => Set<PhraseCategory>();
    public DbSet<PhraseCategoryAssignment> PhraseCategoryAssignments => Set<PhraseCategoryAssignment>();
    public DbSet<PhraseReview> PhraseReviews => Set<PhraseReview>();
    public DbSet<ClueEffectiveness> ClueEffectiveness => Set<ClueEffectiveness>();
    public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();
    public DbSet<PhrasePlayStats> PhrasePlayStats => Set<PhrasePlayStats>();
    public DbSet<SimulationProfile> SimulationProfiles => Set<SimulationProfile>();
    public DbSet<DailyChallenge> DailyChallenges => Set<DailyChallenge>();
    public DbSet<DailyChallengeResult> DailyChallengeResults => Set<DailyChallengeResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureGamePhrase(modelBuilder);
        ConfigureGameRecord(modelBuilder);
        ConfigureGameEvent(modelBuilder);
        ConfigureGameSession(modelBuilder);
        ConfigureRoles(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureContentManagement(modelBuilder);
        ConfigureAnalytics(modelBuilder);
        ConfigureSimulation(modelBuilder);
        ConfigureDailyChallenge(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UniqueId);
            entity.Property(e => e.UniqueId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.RefreshToken).HasMaxLength(255);
            entity.Property(e => e.LifetimePoints).HasDefaultValue(0);
            entity.Property(e => e.PreferredDifficulty).HasDefaultValue(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private static void ConfigureGamePhrase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GamePhrase>(entity =>
        {
            entity.HasKey(e => e.UniqueId);
            entity.Property(e => e.UniqueId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Text).HasMaxLength(500).IsRequired();
            entity.Property(e => e.WordCount).IsRequired();
            entity.Property(e => e.Difficulty).HasDefaultValue(0);
            entity.Property(e => e.GeneratedByLlm).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.Text).IsUnique();
        });
    }

    private static void ConfigureGameRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameRecord>(entity =>
        {
            entity.HasKey(e => e.GameId);
            entity.Property(e => e.GameId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.PhraseText).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PhraseUniqueId).HasMaxLength(30).HasDefaultValue(string.Empty);
            entity.Property(e => e.Score).HasDefaultValue(0);
            entity.Property(e => e.Difficulty).HasDefaultValue(0);
            entity.Property(e => e.Result).HasConversion<string>().HasMaxLength(20).HasDefaultValue(GameResult.InProgress);

            entity.HasIndex(e => new { e.UserId, e.PlayedAt });

            // Ignore computed property
            entity.Ignore(e => e.IsCompleted);
            // Ignore embedded Events — GameEvents are a separate table
            entity.Ignore(e => e.Events);
        });
    }

    private static void ConfigureGameEvent(ModelBuilder modelBuilder)
    {
        // Single-table inheritance for all event types
        modelBuilder.Entity<GameEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.GameId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.SequenceNumber).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();

            // Ignore the abstract EventType property — EF Core uses a shadow discriminator
            entity.Ignore(e => e.EventType);

            entity.HasIndex(e => new { e.GameId, e.SequenceNumber });

            entity.HasDiscriminator<string>("Discriminator")
                .HasValue<ClueEvent>("clue")
                .HasValue<GuessEvent>("guess")
                .HasValue<GameEndEvent>("gameend");
        });

        modelBuilder.Entity<ClueEvent>(entity =>
        {
            entity.Property(e => e.SearchTerm).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(2048);
        });

        modelBuilder.Entity<GuessEvent>(entity =>
        {
            entity.Property(e => e.GuessText).HasMaxLength(100);
        });

        modelBuilder.Entity<GameEndEvent>(entity =>
        {
            entity.Property(e => e.Reason).HasMaxLength(20);
        });
    }

    private static void ConfigureGameSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameSessionRecord>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(30);
            entity.Property(e => e.PhraseUniqueId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.GameRecordId).HasMaxLength(30);
            entity.Property(e => e.Score).HasDefaultValue(0);
            entity.Property(e => e.Difficulty).HasDefaultValue(10);
            entity.Property(e => e.StateJson).HasColumnType("TEXT").IsRequired();
            entity.Property(e => e.StartedAt).IsRequired();
            entity.Property(e => e.LastActivityAt).IsRequired();

            entity.HasIndex(e => e.LastActivityAt);
        });
    }

    private static void ConfigureRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasData(
                new Role { Id = 1, Name = "Player" },
                new Role { Id = 2, Name = "Moderator" },
                new Role { Id = 3, Name = "Admin" }
            );
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.Property(e => e.UserId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.AssignedAt).IsRequired();

            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(30);
            entity.Property(e => e.Details).HasColumnType("TEXT");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Timestamp).IsRequired();

            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
        });
    }

    private static void ConfigureContentManagement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SiteConfig>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasColumnType("TEXT").IsRequired();
            entity.Property(e => e.ValueType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(30);

            entity.HasData(
                new SiteConfig { Key = "MaxSessionTtlHours", Value = "24", ValueType = "int", Description = "Maximum session time-to-live in hours" },
                new SiteConfig { Key = "DefaultDifficulty", Value = "10", ValueType = "int", Description = "Default difficulty for new games" },
                new SiteConfig { Key = "ClueRetryLimit", Value = "5", ValueType = "int", Description = "Maximum clue retries per word" },
                new SiteConfig { Key = "MaintenanceMode", Value = "false", ValueType = "bool", Description = "Enable maintenance mode" }
            );
        });

        modelBuilder.Entity<PhraseCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasData(
                new PhraseCategory { Id = 1, Name = "Idioms", Description = "Common idiomatic expressions" },
                new PhraseCategory { Id = 2, Name = "Proverbs", Description = "Traditional sayings and proverbs" },
                new PhraseCategory { Id = 3, Name = "Quotes", Description = "Famous quotes and expressions" },
                new PhraseCategory { Id = 4, Name = "Pop Culture", Description = "References from movies, TV, and music" },
                new PhraseCategory { Id = 5, Name = "Science", Description = "Scientific terms and concepts" },
                new PhraseCategory { Id = 6, Name = "Literature", Description = "Literary references and phrases" }
            );
        });

        modelBuilder.Entity<PhraseCategoryAssignment>(entity =>
        {
            entity.HasKey(e => new { e.PhraseUniqueId, e.CategoryId });
            entity.Property(e => e.PhraseUniqueId).HasMaxLength(30).IsRequired();
            entity.HasOne(e => e.Phrase).WithMany().HasForeignKey(e => e.PhraseUniqueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhraseReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PhraseUniqueId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.SubmittedBy).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ReviewedBy).HasMaxLength(30);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
            entity.Property(e => e.ReviewNotes).HasColumnType("TEXT");
            entity.Property(e => e.SubmittedAt).IsRequired();

            entity.HasOne(e => e.Phrase).WithMany().HasForeignKey(e => e.PhraseUniqueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Status);
        });
    }

    private static void ConfigureAnalytics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClueEffectiveness>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TargetWord).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SearchTerm).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UrlDomain).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TimesShown).HasDefaultValue(0);
            entity.Property(e => e.TimesLedToCorrectGuess).HasDefaultValue(0);
            entity.Property(e => e.AvgGuessesAfterClue).HasColumnType("decimal(5,2)");
            entity.Property(e => e.LastComputedAt).IsRequired();

            entity.HasIndex(e => new { e.TargetWord, e.SearchTerm, e.UrlDomain }).IsUnique();
        });

        modelBuilder.Entity<PlayerStats>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.GamesPlayed).HasDefaultValue(0);
            entity.Property(e => e.GamesSolved).HasDefaultValue(0);
            entity.Property(e => e.GamesGaveUp).HasDefaultValue(0);
            entity.Property(e => e.AvgScore).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            entity.Property(e => e.AvgCluesPerGame).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
            entity.Property(e => e.AvgGuessesPerGame).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
            entity.Property(e => e.BestScore).HasDefaultValue(0);
            entity.Property(e => e.CurrentStreak).HasDefaultValue(0);
            entity.Property(e => e.BestStreak).HasDefaultValue(0);
            entity.Property(e => e.ComputedAt).IsRequired();
        });

        modelBuilder.Entity<PhrasePlayStats>(entity =>
        {
            entity.HasKey(e => e.PhraseUniqueId);
            entity.Property(e => e.PhraseUniqueId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.TimesPlayed).HasDefaultValue(0);
            entity.Property(e => e.TimesSolved).HasDefaultValue(0);
            entity.Property(e => e.TimesGaveUp).HasDefaultValue(0);
            entity.Property(e => e.SolveRate).HasColumnType("decimal(5,4)").HasDefaultValue(0m);
            entity.Property(e => e.AvgCluesToSolve).HasColumnType("decimal(5,2)");
            entity.Property(e => e.AvgTimeToSolveSeconds).HasColumnType("decimal(10,2)");
            entity.Property(e => e.GiveUpRate).HasColumnType("decimal(5,4)").HasDefaultValue(0m);
            entity.Property(e => e.LastComputedAt).IsRequired();
        });
    }

    private static void ConfigureSimulation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimulationProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ClueProbability).HasColumnType("decimal(3,2)");
            entity.Property(e => e.CorrectGuessProbability).HasColumnType("decimal(3,2)");
            entity.Property(e => e.GiveUpProbability).HasColumnType("decimal(3,2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasData(
                new SimulationProfile { Id = 1, Name = "Beginner", Description = "Simulates a beginner player", ClueProbability = 0.8m, CorrectGuessProbability = 0.3m, GiveUpProbability = 0.2m, PreferredDifficulty = 20 },
                new SimulationProfile { Id = 2, Name = "Average", Description = "Simulates an average player", ClueProbability = 0.5m, CorrectGuessProbability = 0.6m, GiveUpProbability = 0.1m, PreferredDifficulty = 50 },
                new SimulationProfile { Id = 3, Name = "Expert", Description = "Simulates an expert player", ClueProbability = 0.2m, CorrectGuessProbability = 0.9m, GiveUpProbability = 0.02m, PreferredDifficulty = 80 }
            );
        });

        modelBuilder.Entity<User>()
            .Property(e => e.IsSimulated).HasDefaultValue(false);

        modelBuilder.Entity<GameRecord>()
            .Property(e => e.IsSimulated).HasDefaultValue(false);
    }

    private static void ConfigureDailyChallenge(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyChallenge>(entity =>
        {
            entity.HasKey(e => e.Date);
            entity.Property(e => e.PhraseUniqueId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<DailyChallengeResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.GameId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Score).HasDefaultValue(0);
            entity.Property(e => e.Result).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CompletedAt).IsRequired();

            entity.HasIndex(e => new { e.ChallengeDate, e.UserId }).IsUnique();
        });
    }
}
