namespace LinkittyDo.Api.Models;

public class GameSession
{
    public Guid SessionId { get; set; }
    public int PhraseId { get; set; }
    public Phrase Phrase { get; set; } = null!;
    public Dictionary<int, bool> RevealedWords { get; set; } = new();
    public int Score { get; set; }
    public DateTime StartedAt { get; set; }
    
    /// <summary>
    /// Tracks used clue search terms per word index to avoid duplicate clues
    /// </summary>
    public Dictionary<int, HashSet<string>> UsedClueTerms { get; set; } = new();
    
    /// <summary>
    /// Tracks used clue URLs to ensure the same page isn't shown twice
    /// </summary>
    public HashSet<string> UsedClueUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
