namespace LinkittyDo.Api.Models;

public class ClueEffectiveness
{
    public long Id { get; set; }
    public string TargetWord { get; set; } = string.Empty;
    public string SearchTerm { get; set; } = string.Empty;
    public string UrlDomain { get; set; } = string.Empty;
    public int TimesShown { get; set; }
    public int TimesLedToCorrectGuess { get; set; }
    public decimal? AvgGuessesAfterClue { get; set; }
    public DateTime LastComputedAt { get; set; } = DateTime.UtcNow;
}
