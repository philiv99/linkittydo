using System.Text.Json.Serialization;

namespace LinkittyDo.Api.Models;

public class User
{
    public string UniqueId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public int LifetimePoints { get; set; } = 0;
    public int PreferredDifficulty { get; set; } = 10;
    public List<GameRecord> Games { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
