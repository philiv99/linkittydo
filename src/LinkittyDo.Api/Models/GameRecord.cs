namespace LinkittyDo.Api.Models;

/// <summary>
/// Represents a record of a completed game for a user
/// </summary>
public class GameRecord
{
    /// <summary>
    /// Unique identifier for this game (format: GAME-{timestamp}-{random})
    /// </summary>
    public string GameId { get; set; } = string.Empty;
    
    /// <summary>
    /// When the game was started
    /// </summary>
    public DateTime PlayedAt { get; set; }
    
    /// <summary>
    /// When the game was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Final score for this game
    /// </summary>
    public int Score { get; set; }
    
    /// <summary>
    /// The phrase ID that was played
    /// </summary>
    public int PhraseId { get; set; }
    
    /// <summary>
    /// The full phrase text that was played
    /// </summary>
    public string PhraseText { get; set; } = string.Empty;
    
    /// <summary>
    /// Difficulty setting when the game was played
    /// </summary>
    public int Difficulty { get; set; }
    
    /// <summary>
    /// Result of the game (InProgress, Solved, GaveUp)
    /// </summary>
    public GameResult Result { get; set; } = GameResult.InProgress;
    
    /// <summary>
    /// Ordered list of events (clues, guesses, game end) that occurred during the game
    /// </summary>
    public List<GameEvent> Events { get; set; } = new();
    
    /// <summary>
    /// Helper property to check if game is completed
    /// </summary>
    public bool IsCompleted => Result != GameResult.InProgress;
}
