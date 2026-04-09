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
    /// The difficulty level for this game session (0-100)
    /// </summary>
    public int Difficulty { get; set; } = 10;
    
    /// <summary>
    /// The user ID playing this game (null for guests)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// The game record that will be saved when the game completes (null for guests)
    /// </summary>
    public GameRecord? GameRecord { get; set; }
    
    /// <summary>
    /// Tracks used clue search terms per word index to avoid duplicate clues
    /// </summary>
    public Dictionary<int, HashSet<string>> UsedClueTerms { get; set; } = new();
    
    /// <summary>
    /// Tracks used clue URLs to ensure the same page isn't shown twice
    /// </summary>
    public HashSet<string> UsedClueUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Tracks the number of clues requested per word index
    /// </summary>
    public Dictionary<int, int> ClueCountPerWord { get; set; } = new();
    
    /// <summary>
    /// Tracks the number of guesses made per word index
    /// </summary>
    public Dictionary<int, int> GuessCountPerWord { get; set; } = new();
    
    /// <summary>
    /// Indicates if this is a guest session (no events/game record saved)
    /// </summary>
    public bool IsGuestSession => string.IsNullOrEmpty(UserId);
}
