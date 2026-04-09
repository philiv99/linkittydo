namespace LinkittyDo.Api.Models;

public class SimulationProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Probability (0.0-1.0) the simulated player requests a clue before guessing</summary>
    public decimal ClueProbability { get; set; } = 0.5m;

    /// <summary>Probability (0.0-1.0) the simulated player guesses correctly</summary>
    public decimal CorrectGuessProbability { get; set; } = 0.6m;

    /// <summary>Probability (0.0-1.0) the simulated player gives up (per word, after clue)</summary>
    public decimal GiveUpProbability { get; set; } = 0.1m;

    /// <summary>Average delay in seconds between actions (for realistic timing)</summary>
    public int AvgActionDelaySeconds { get; set; } = 5;

    /// <summary>Difficulty preference for the simulated player (0-100)</summary>
    public int PreferredDifficulty { get; set; } = 50;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
