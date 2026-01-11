using System.Text.Json.Serialization;

namespace LinkittyDo.Api.Models;

/// <summary>
/// Base class for game events (clues, guesses, game end)
/// </summary>
[JsonDerivedType(typeof(ClueEvent), typeDiscriminator: "clue")]
[JsonDerivedType(typeof(GuessEvent), typeDiscriminator: "guess")]
[JsonDerivedType(typeof(GameEndEvent), typeDiscriminator: "gameend")]
public abstract class GameEvent
{
    /// <summary>
    /// Type of event for serialization
    /// </summary>
    public abstract string EventType { get; }
    
    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event recorded when a clue is requested
/// </summary>
public class ClueEvent : GameEvent
{
    public override string EventType => "clue";
    
    /// <summary>
    /// The word index this clue was for
    /// </summary>
    public int WordIndex { get; set; }
    
    /// <summary>
    /// The search term used to find the clue (synonym)
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;
    
    /// <summary>
    /// The URL that was shown as the clue
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Event recorded when a guess is submitted
/// </summary>
public class GuessEvent : GameEvent
{
    public override string EventType => "guess";
    
    /// <summary>
    /// The word index the guess was for
    /// </summary>
    public int WordIndex { get; set; }
    
    /// <summary>
    /// The actual guess text submitted
    /// </summary>
    public string GuessText { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the guess was correct
    /// </summary>
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// Points awarded (100 if correct, 0 if incorrect)
    /// </summary>
    public int PointsAwarded { get; set; }
}

/// <summary>
/// Event recorded when the game ends (give up)
/// </summary>
public class GameEndEvent : GameEvent
{
    public override string EventType => "gameend";
    
    /// <summary>
    /// The reason for game ending: "solved" or "gaveup"
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result status of a completed game
/// </summary>
public enum GameResult
{
    InProgress,
    Solved,
    GaveUp
}
