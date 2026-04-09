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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureGamePhrase(modelBuilder);
        ConfigureGameRecord(modelBuilder);
        ConfigureGameEvent(modelBuilder);
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

            // Ignore the embedded Games collection — GameRecords are a separate table
            entity.Ignore(e => e.Games);
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
}
