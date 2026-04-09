using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public class SimulationSummary
{
    public int TotalSimUsers { get; set; }
    public int TotalSimGames { get; set; }
    public int SimSolved { get; set; }
    public int SimGaveUp { get; set; }
    public double SimSolveRate { get; set; }
    public double SimAvgScore { get; set; }
    public Dictionary<string, int> GamesByProfile { get; set; } = new();
}

public class PlayerDetail
{
    public User? User { get; set; }
    public PlayerStats? Stats { get; set; }
    public IReadOnlyList<GameRecord> RecentGames { get; set; } = Array.Empty<GameRecord>();
}

public class DataSummary
{
    public int TotalUsers { get; set; }
    public int TotalPhrases { get; set; }
    public int TotalGames { get; set; }
    public int TotalEvents { get; set; }
    public int TotalSimUsers { get; set; }
    public int TotalSimGames { get; set; }
    public long EstimatedStorageSizeBytes { get; set; }
    public DateTime OldestGame { get; set; }
    public DateTime NewestGame { get; set; }
}

public interface IDataExplorerService
{
    Task<SimulationSummary> GetSimulationSummaryAsync();
    Task<PlayerDetail?> GetPlayerDetailAsync(string userId);
    Task<DataSummary> GetDataSummaryAsync();
}
