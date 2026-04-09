namespace LinkittyDo.Api.Models;

public class PlayerStats
{
    public string UserId { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int GamesSolved { get; set; }
    public int GamesGaveUp { get; set; }
    public decimal AvgScore { get; set; }
    public decimal AvgCluesPerGame { get; set; }
    public decimal AvgGuessesPerGame { get; set; }
    public int BestScore { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
