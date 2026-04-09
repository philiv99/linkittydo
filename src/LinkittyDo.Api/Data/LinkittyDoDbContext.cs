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
}
