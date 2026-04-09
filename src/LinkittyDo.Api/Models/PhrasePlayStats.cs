namespace LinkittyDo.Api.Models;

public class PhrasePlayStats
{
    public string PhraseUniqueId { get; set; } = string.Empty;
    public int TimesPlayed { get; set; }
    public int TimesSolved { get; set; }
    public int TimesGaveUp { get; set; }
    public decimal SolveRate { get; set; }
    public decimal? AvgCluesToSolve { get; set; }
    public decimal? AvgTimeToSolveSeconds { get; set; }
    public decimal GiveUpRate { get; set; }
    public int? CalibratedDifficulty { get; set; }
    public DateTime LastComputedAt { get; set; } = DateTime.UtcNow;
}
