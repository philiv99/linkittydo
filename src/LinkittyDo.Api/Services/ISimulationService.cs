using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public class SimulationResult
{
    public string GameId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public GameResult Result { get; set; }
    public int Score { get; set; }
    public int ClueCount { get; set; }
    public int GuessCount { get; set; }
}

public class BatchSimulationResult
{
    public int TotalGames { get; set; }
    public int Solved { get; set; }
    public int GaveUp { get; set; }
    public double AvgScore { get; set; }
    public List<SimulationResult> Games { get; set; } = new();
}

public class PurgeResult
{
    public int UsersDeleted { get; set; }
    public int GamesDeleted { get; set; }
    public int EventsDeleted { get; set; }
}

public interface ISimulationService
{
    Task<SimulationResult> SimulateGameAsync(int profileId, string? phraseUniqueId = null);
    Task<BatchSimulationResult> RunBatchAsync(int profileId, int count, string? phraseUniqueId = null);
    Task<PurgeResult> PurgeSimulationDataAsync();
    Task<IReadOnlyList<SimulationProfile>> GetProfilesAsync();
}
