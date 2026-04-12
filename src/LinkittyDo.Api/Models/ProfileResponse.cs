namespace LinkittyDo.Api.Models;

public class ProfileResponse
{
    public string UniqueId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int LifetimePoints { get; set; }
    public int PreferredDifficulty { get; set; }
    public DateTime CreatedAt { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesSolved { get; set; }
    public int GamesGaveUp { get; set; }
    public decimal SolveRate { get; set; }
    public decimal AvgScore { get; set; }
    public int BestScore { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public List<GameRecord> RecentGames { get; set; } = new();
}
