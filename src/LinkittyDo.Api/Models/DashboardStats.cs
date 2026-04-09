namespace LinkittyDo.Api.Models;

public class DashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalGamesPlayed { get; set; }
    public int TotalGamesSolved { get; set; }
    public int TotalGamesGaveUp { get; set; }
    public int TotalPhrases { get; set; }
    public int ActivePhrases { get; set; }
    public int SimulatedUsers { get; set; }
    public int SimulatedGames { get; set; }
    public double OverallSolveRate { get; set; }
    public double AvgScore { get; set; }
    public int GamesPlayedToday { get; set; }
    public int NewUsersToday { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
