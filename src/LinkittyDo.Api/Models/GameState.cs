namespace LinkittyDo.Api.Models;

/// <summary>
/// Represents the current state of a game sent to the client
/// </summary>
public class GameState
{
    public Guid SessionId { get; set; }
    public List<WordState> Words { get; set; } = new();
    public int Score { get; set; }
    public bool IsComplete { get; set; }
}

public class WordState
{
    public int Index { get; set; }
    public string? DisplayText { get; set; }  // null if hidden and not yet guessed
    public bool IsHidden { get; set; }
    public bool IsRevealed { get; set; }
}
